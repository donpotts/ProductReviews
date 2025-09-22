#pragma warning disable SKEXP0001
using AppProduct.Data;
using AppProduct.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Http;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Globalization;

namespace AppProduct.Services;

public interface IProductChatService
{
    Task<(string answer, IEnumerable<Product> sources)> AskAsync(string question, CancellationToken ct = default);
}

public class ProductChatService(
    ApplicationDbContext db,
    ITextEmbeddingGenerationService embeddingService,
    IChatCompletionService chatService) : IProductChatService
{
    private static readonly ConcurrentDictionary<long, float[]> _productEmbeddings = new();
    private static bool _initialized;
    private static readonly SemaphoreSlim _initLock = new(1,1);
    private static bool _aiAvailable = true;              // Chat model availability
    private static bool _embeddingAvailable = true;       // Embedding model availability

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initialized) return;
        await _initLock.WaitAsync(ct);
        try
        {
            if (_initialized) return;
            List<Product> products = [];
            try
            {
                products = await db.Product
                    .Include(p=>p.Brand)
                    .Include(p=>p.Category)
                    .Include(p=>p.Feature)
                    .Include(p=>p.Tag)
                    .ToListAsync(ct);

                foreach(var p in products)
                {
                    if(!_embeddingAvailable) break;
                    var text = BuildProductText(p);
                    try
                    {
                        var embedding = await embeddingService.GenerateEmbeddingAsync(text, cancellationToken: ct);
                        _productEmbeddings[p.Id!.Value] = embedding.ToArray();
                    }
                    catch (HttpOperationException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        _embeddingAvailable = false;
                        break;
                    }
                    catch
                    {
                        _embeddingAvailable = false;
                        break;
                    }
                }
            }
            catch
            {
                _embeddingAvailable = false; // DB or other issue, still allow chat
            }
            finally
            {
                _initialized = true; // mark so we don't retry every call (avoid repeated failures)
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static string BuildProductText(Product p)
    {
        var brands = string.Join(',', p.Brand?.Select(b => b.Name) ?? Enumerable.Empty<string>());
        var categories = string.Join(',', p.Category?.Select(c => c.Name) ?? Enumerable.Empty<string>());
        var features = string.Join(',', p.Feature?.Select(f => f.Name) ?? Enumerable.Empty<string>());
        var tags = string.Join(',', p.Tag?.Select(t => t.Name) ?? Enumerable.Empty<string>());
        var sb = new StringBuilder();
        sb.AppendLine($"Product Id: {p.Id}");
        sb.AppendLine($"Name: {p.Name}");
        sb.AppendLine($"Description: {p.Description}");
        sb.AppendLine($"Specs: {p.DetailedSpecs}");
        sb.AppendLine($"Price: {p.Price}");
        sb.AppendLine($"InStock: {p.InStock}");
        sb.AppendLine($"ReleaseDate: {p.ReleaseDate}");
        sb.AppendLine($"Brand: {brands}");
        sb.AppendLine($"Categories: {categories}");
        sb.AppendLine($"Features: {features}");
        sb.AppendLine($"Tags: {tags}");
        return sb.ToString().TrimEnd();
    }

    private static float[] HashEmbed(string text, int dim = 32)
    {
        var vec = new float[dim];
        foreach (var ch in text)
        {
            vec[ch % dim] += 1f;
        }
        var norm = (float)Math.Sqrt(vec.Sum(v => v * v)) + 1e-6f;
        for (int i = 0; i < dim; i++) vec[i] /= norm;
        return vec;
    }

    // Detect if the user intent is to list/show all products (broad catalog request)
    private static bool IsListAllRequest(string question)
    {
        if (string.IsNullOrWhiteSpace(question)) return false;
        var q = question.ToLowerInvariant();
        string[] patterns =
        [
            "list all products",
            "show all products",
            "what products do you have",
            "show me every product",
            "list every product",
            "all your products",
            "entire catalog",
            "full catalog",
            "everything you have"
        ];
        return patterns.Any(p => q.Contains(p));
    }

    // Detect lowest / cheapest product request
    private static bool IsLowestPriceRequest(string question)
    {
        if (string.IsNullOrWhiteSpace(question)) return false;
        var q = question.ToLowerInvariant();
        string[] patterns =
        [
            "lowest priced",
            "lowest price",
            "cheapest",
            "least expensive",
            "lowest cost",
            "lowest-priced",
            "cheapest product",
            "least costly"
        ];
        return patterns.Any(p => q.Contains(p));
    }

    public async Task<(string answer, IEnumerable<Product> sources)> AskAsync(string question, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);

        if(!_aiAvailable)
        {
            return ("AI not available (invalid or missing key).", Enumerable.Empty<Product>());
        }

        bool lowestRequest = IsLowestPriceRequest(question);

        float[] qEmbedding = [];
        bool retrievalPossible = _embeddingAvailable && _productEmbeddings.Any();
        if (retrievalPossible)
        {
            try
            {
                var qEmbeddingMem = await embeddingService.GenerateEmbeddingAsync(question, cancellationToken: ct);
                qEmbedding = qEmbeddingMem.ToArray();
            }
            catch (HttpOperationException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                _embeddingAvailable = false; // disable embeddings but keep chat
                retrievalPossible = false;
            }
            catch
            {
                _embeddingAvailable = false;
                retrievalPossible = false;
            }
        }
        if (!retrievalPossible)
        {
            // Fallback deterministic embedding for cosine selection (only if we already have deterministic ones). If no product embeddings because disabled early, skip retrieval.
            if (_productEmbeddings.Any())
            {
                qEmbedding = HashEmbed(question);
            }
        }

        static double CosSim(IReadOnlyList<float> a, IReadOnlyList<float> b)
        {
            double dot=0, na=0, nb=0; int len = Math.Min(a.Count,b.Count);
            for(int i=0;i<len;i++){ dot += a[i]*b[i]; na += a[i]*a[i]; nb += b[i]*b[i]; }
            return dot / (Math.Sqrt(na)*Math.Sqrt(nb) + 1e-8);
        }

        List<Product> products = [];
        if (qEmbedding.Length > 0 && _productEmbeddings.Any())
        {
            var top = _productEmbeddings
                .Select(kvp => new { id = kvp.Key, score = CosSim(qEmbedding, kvp.Value) })
                .OrderByDescending(x=>x.score)
                .Take(5)
                .ToList();

            var productIds = top.Select(x=>x.id).ToList();
            products = await db.Product
                .Include(p=>p.Brand)
                .Include(p=>p.Category)
                .Include(p=>p.Feature)
                .Include(p=>p.Tag)
                .Where(p=> productIds.Contains(p.Id!.Value))
                .ToListAsync(ct);
        }

        Product? lowestProduct = null;
        if (lowestRequest)
        {
            try
            {
                lowestProduct = await db.Product
                    .Include(p=>p.Brand)
                    .Include(p=>p.Category)
                    .Include(p=>p.Feature)
                    .Include(p=>p.Tag)
                    .Where(p => p.Price != null)
                    .OrderBy(p => p.Price)
                    .FirstOrDefaultAsync(ct);

                if (lowestProduct != null && !products.Any(p => p.Id == lowestProduct.Id))
                {
                    products.Add(lowestProduct);
                }
                else if (!products.Any() && lowestProduct != null)
                {
                    products = [ lowestProduct ];
                }
            }
            catch
            {
                // ignore retrieval issues
            }
        }

        var contextBlock = products.Any() ? string.Join("\n\n---\n\n", products.Select(p => BuildProductText(p))) : "(no product context available)";
        var systemPrompt = "You are a strict product knowledge assistant. Answer ONLY using the provided product context. If the question is outside product data, reply: 'I can only answer questions about the products in the catalog.' Provide concise factual answers.";
        if (lowestRequest && lowestProduct != null)
        {
            systemPrompt += " If the user asks for the lowest priced product, respond ONLY with that single product's name, Id and price (and optionally a brief spec) drawn from context.";
        }
        var prompt = $"<system>\n{systemPrompt}\n</system>\n<context>\n{contextBlock}\n</context>\n<user_question>\n{question}\n</user_question>\n<instructions>Limit answer to product facts. Do not speculate. Cite product Ids mentioned.</instructions>";

        string answer;
        try
        {
            var history = new ChatHistory();
            history.AddSystemMessage(systemPrompt + " Context will follow.");
            history.AddUserMessage(prompt);
            var result = await chatService.GetChatMessageContentAsync(history, cancellationToken: ct);
            answer = result.Content ?? "(no answer)";
        }
        catch (HttpOperationException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            _aiAvailable = false;
            return ("AI not available (invalid key).", products);
        }
        catch
        {
            _aiAvailable = false;
            return ("AI not available (error calling model).", products);
        }

        if(!products.Any() || answer.Contains("I don't know", StringComparison.OrdinalIgnoreCase))
        {
            answer = "I can only answer questions about the products in the catalog.";
        }
        var forbidden = new []{"politics","weather","news","sports"};
        if(forbidden.Any(f => answer.Contains(f, StringComparison.OrdinalIgnoreCase)))
        {
            answer = "I can only answer questions about the products in the catalog.";
        }

        if (!_embeddingAvailable)
        {
            answer += "\n\n(Note: Embedding model unavailable, retrieval reduced.)";
        }

        // If user asked to list/show all products, explain why only a subset is returned
        if (IsListAllRequest(question))
        {
            try
            {
                var total = await db.Product.CountAsync(ct);
                if (products.Any() && products.Count < total)
                {
                    answer += $"\n\nNote: Showing only {products.Count} of {total} products (top matches) to keep the response concise and within token limits. Ask about a category, feature, brand, or specific criteria for more targeted details.";
                }
            }
            catch
            {
                // ignore counting errors
            }
        }

        // Guarantee lowest priced product shown if requested
        if (lowestRequest && lowestProduct != null)
        {
            var idStr = lowestProduct.Id?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            var name = lowestProduct.Name ?? string.Empty;
            if (!answer.Contains(idStr, StringComparison.OrdinalIgnoreCase) && !answer.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                var priceStr = lowestProduct.Price?.ToString("0.00", CultureInfo.InvariantCulture) ?? "unknown";
                answer = $"Lowest priced product: {name} (Id {idStr}) at price {priceStr}.";
            }
        }

        return (answer, products);
    }
}
#pragma warning restore SKEXP0001
