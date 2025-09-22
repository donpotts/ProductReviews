#pragma warning disable SKEXP0001, SKEXP0010
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AppProduct.Configuration;

public static class SemanticKernelConfig
{
    public static void AddSemanticKernel(this IServiceCollection services, IConfiguration config)
    {
        var endpoint = config["GitHubAI:Endpoint"] ?? "https://models.inference.ai.azure.com";
        var model = config["GitHubAI:ChatModel"] ?? "gpt-4o-mini";
        var embeddingModel = config["GitHubAI:EmbeddingModel"] ?? "text-embedding-3-small";
        var apiKey = config["GitHubAI:ApiKey"] ?? config["GITHUB_TOKEN"] ?? string.Empty;

        var builder = services.AddKernel();

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            builder.AddOpenAIChatCompletion(modelId: model, apiKey: apiKey, endpoint: new Uri(endpoint));

            services.AddHttpClient("GitHubModels", client =>
            {
                client.BaseAddress = new Uri(endpoint);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });
            services.AddSingleton<ITextEmbeddingGenerationService>(sp =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("GitHubModels");
                return new GitHubOpenAIEmbeddingService(httpClient, embeddingModel);
            });
        }
        else
        {
            services.AddSingleton<ITextEmbeddingGenerationService, SimpleDeterministicEmbeddingService>();
            services.AddSingleton<IChatCompletionService, NoOpChatCompletionService>();
        }
    }

    // Simple deterministic fallback embedding (used only when no API key)
    private sealed class SimpleDeterministicEmbeddingService : ITextEmbeddingGenerationService
    {
        public string ModelId => "fallback-hash-embedding";
        public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

        public Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
            IList<string> data,
            Kernel? kernel,
            CancellationToken cancellationToken)
        {
            IList<ReadOnlyMemory<float>> result = new List<ReadOnlyMemory<float>>(data.Count);
            foreach (var text in data)
            {
                var vec = new float[32];
                foreach (var ch in text)
                {
                    vec[ch % vec.Length] += 1f;
                }
                var norm = (float)Math.Sqrt(vec.Sum(v => v * v)) + 1e-6f;
                for (int i = 0; i < vec.Length; i++) vec[i] /= norm;
                result.Add(new ReadOnlyMemory<float>(vec));
            }
            return Task.FromResult(result);
        }
    }

    // Custom embedding service using GitHub models endpoint (OpenAI-compatible)
    private sealed class GitHubOpenAIEmbeddingService : ITextEmbeddingGenerationService
    {
        private readonly HttpClient _http;
        private readonly string _model;
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public GitHubOpenAIEmbeddingService(HttpClient httpClient, string modelId)
        {
            _http = httpClient;
            _model = modelId;
        }

        public string ModelId => _model;
        public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

        private sealed record EmbeddingRequest(string model, object input);
        private sealed record EmbeddingData(float[] embedding);
        private sealed record EmbeddingResponse(List<EmbeddingData> data);

        public async Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
            IList<string> data,
            Kernel? kernel,
            CancellationToken cancellationToken)
        {
            // GitHub chat path lacks /v1 prefix; embeddings follows same pattern -> /embeddings
            var reqPayload = new EmbeddingRequest(_model, data);
            var json = JsonSerializer.Serialize(reqPayload, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _http.PostAsync("/embeddings", content, cancellationToken);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Embedding request failed: {(int)resp.StatusCode} {resp.ReasonPhrase} - {body}", null, resp.StatusCode);
            }
            await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
            var parsed = await JsonSerializer.DeserializeAsync<EmbeddingResponse>(stream, _jsonOptions, cancellationToken) 
                ?? throw new InvalidOperationException("Invalid embedding response");
            IList<ReadOnlyMemory<float>> result = new List<ReadOnlyMemory<float>>(parsed.data.Count);
            foreach (var d in parsed.data)
            {
                result.Add(new ReadOnlyMemory<float>(d.embedding));
            }
            return result;
        }
    }

    private sealed class NoOpChatCompletionService : IChatCompletionService
    {
        public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();
        public string ModelId => "fallback-noop-chat";

        public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings,
            Kernel? kernel,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ChatMessageContent> list = new List<ChatMessageContent>
            {
                new ChatMessageContent(AuthorRole.Assistant, "AI not configured.", modelId: ModelId)
            };
            return Task.FromResult(list);
        }

        public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings,
            Kernel? kernel,
            CancellationToken cancellationToken)
        {
            return GetStream();
            async IAsyncEnumerable<StreamingChatMessageContent> GetStream()
            {
                yield return new StreamingChatMessageContent(AuthorRole.Assistant, "AI not configured.", modelId: ModelId);
                await Task.CompletedTask;
            }
        }
    }
}
#pragma warning restore SKEXP0001, SKEXP0010
