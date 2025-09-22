using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Web;
using AppProduct.Shared.Blazor.Authorization;
using AppProduct.Shared.Blazor.Models;
using AppProduct.Shared.Models;

namespace AppProduct.Shared.Blazor.Services;

public class AppService(
    HttpClient httpClient,
    AuthenticationStateProvider authenticationStateProvider)
{
    private readonly IdentityAuthenticationStateProvider authenticationStateProvider
            = authenticationStateProvider as IdentityAuthenticationStateProvider
                ?? throw new InvalidOperationException();

    private static async Task HandleResponseErrorsAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode
            && response.StatusCode != HttpStatusCode.Unauthorized
            && response.StatusCode != HttpStatusCode.NotFound)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new Exception(message);
        }

        response.EnsureSuccessStatusCode();
    }

    public class ODataResult<T>
    {
        [JsonPropertyName("@odata.count")]
        public int? Count { get; set; }

        public IEnumerable<T>? Value { get; set; }
    }

    public async Task<ODataResult<T>?> GetODataAsync<T>(
            string entity,
            int? top = null,
            int? skip = null,
            string? orderby = null,
            string? filter = null,
            bool count = false,
            string? expand = null)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        var queryString = HttpUtility.ParseQueryString(string.Empty);

        if (top.HasValue)
        {
            queryString.Add("$top", top.ToString());
        }

        if (skip.HasValue)
        {
            queryString.Add("$skip", skip.ToString());
        }

        if (!string.IsNullOrEmpty(orderby))
        {
            queryString.Add("$orderby", orderby);
        }

        if (!string.IsNullOrEmpty(filter))
        {
            queryString.Add("$filter", filter);
        }

        if (count)
        {
            queryString.Add("$count", "true");
        }

        if (!string.IsNullOrEmpty(expand))
        {
            queryString.Add("$expand", expand);
        }

        var uri = $"/odata/{entity}?{queryString}";

        HttpRequestMessage request = new(HttpMethod.Get, uri);
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<ODataResult<T>>();
    }


    public async Task<Dictionary<string, List<string>>> RegisterUserAsync(RegisterModel registerModel)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/identity/register",
            new { registerModel.Email, registerModel.Password });

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var json = await response.Content.ReadAsStringAsync();

            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

            return problemDetails?.Errors != null
                ? problemDetails.Errors
                : throw new Exception("Bad Request");
        }

        response.EnsureSuccessStatusCode();

        response = await httpClient.PostAsJsonAsync(
            "/identity/login",
            new { registerModel.Email, registerModel.Password });

        response.EnsureSuccessStatusCode();

        var accessTokenResponse = await response.Content.ReadFromJsonAsync<AccessTokenResponse>()
            ?? throw new Exception("Failed to authenticate");

        HttpRequestMessage request = new(HttpMethod.Put, "/api/user/@me");
        request.Headers.Authorization = new("Bearer", accessTokenResponse.AccessToken);
        request.Content = JsonContent.Create(new UpdateApplicationUserDto
        {
            FirstName = registerModel.FirstName,
            LastName = registerModel.LastName,
            Title = registerModel.Title,
            CompanyName = registerModel.CompanyName,
            Photo = registerModel.Photo,
        });

        response = await httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        return [];
    }

    public async Task<ApplicationUserDto[]?> ListUserAsync()
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Get, "/api/user");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<ApplicationUserDto[]>();
    }

    public Task<ODataResult<ApplicationUserDto>?> ListUserODataAsync(
        int? top = null,
        int? skip = null,
        string? orderby = null,
        string? filter = null,
        bool count = false,
        string? expand = null)
    {
        return GetODataAsync<ApplicationUserDto>("User", top, skip, orderby, filter, count, expand);
    }

    public async Task<ApplicationUserWithRolesDto?> GetUserByIdAsync(string id)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Get, $"/api/user/{id}");
        request.Headers.Add("Authorization", $"Bearer {token}");

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<ApplicationUserWithRolesDto>();
    }

    public async Task UpdateUserAsync(string id, UpdateApplicationUserDto data)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Put, $"/api/user/{id}");
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Content = JsonContent.Create(data);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);
    }

    public async Task DeleteUserAsync(string id)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Delete, $"/api/user/{id}");
        request.Headers.Add("Authorization", $"Bearer {token}");

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);
    }

    public async Task<Product[]?> ListProductAsync()
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Get, "/api/product");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<Product[]>();
    }

    public Task<ODataResult<Product>?> ListProductODataAsync(
        int? top = null,
        int? skip = null,
        string? orderby = null,
        string? filter = null,
        bool count = false,
        string? expand = null)
    {
        return GetODataAsync<Product>("Product", top, skip, orderby, filter, count, expand);
    }

    public async Task<Product?> GetProductByIdAsync(long key)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Get, $"/api/product/{key}");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<Product>();
    }

    public async Task UpdateProductAsync(long key, Product data)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Put, $"/api/product/{key}");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(data);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);
    }

    public async Task<Product?> InsertProductAsync(Product data)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Post, "/api/product");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(data);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<Product>();
    }

    public async Task DeleteProductAsync(long key)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Delete, $"/api/product/{key}");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);
    }

    public async Task<Brand[]?> ListBrandAsync()
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Get, "/api/brand");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<Brand[]>();
    }

    public Task<ODataResult<Brand>?> ListBrandODataAsync(
        int? top = null,
        int? skip = null,
        string? orderby = null,
        string? filter = null,
        bool count = false,
        string? expand = null)
    {
        return GetODataAsync<Brand>("Brand", top, skip, orderby, filter, count, expand);
    }

    public async Task<Brand?> GetBrandByIdAsync(long key)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Get, $"/api/brand/{key}");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<Brand>();
    }

    public async Task UpdateBrandAsync(long key, Brand data)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Put, $"/api/brand/{key}");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(data);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);
    }

    public async Task<Brand?> InsertBrandAsync(Brand data)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Post, "/api/brand");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(data);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<Brand>();
    }

    public async Task DeleteBrandAsync(long key)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Delete, $"/api/brand/{key}");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);
    }

    public async Task<Category[]?> ListCategoryAsync()
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Get, "/api/category");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<Category[]>();
    }

    public Task<ODataResult<Category>?> ListCategoryODataAsync(
        int? top = null,
        int? skip = null,
        string? orderby = null,
        string? filter = null,
        bool count = false,
        string? expand = null)
    {
        return GetODataAsync<Category>("Category", top, skip, orderby, filter, count, expand);
    }

    public async Task<Category?> GetCategoryByIdAsync(long key)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Get, $"/api/category/{key}");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<Category>();
    }

    public async Task UpdateCategoryAsync(long key, Category data)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Put, $"/api/category/{key}");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(data);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);
    }

    public async Task<Category?> InsertCategoryAsync(Category data)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Post, "/api/category");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(data);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<Category>();
    }

    public async Task DeleteCategoryAsync(long key)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Delete, $"/api/category/{key}");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);
    }

    public async Task<Feature[]?> ListFeatureAsync()
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Get, "/api/feature");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<Feature[]>();
    }

    public Task<ODataResult<Feature>?> ListFeatureODataAsync(
        int? top = null,
        int? skip = null,
        string? orderby = null,
        string? filter = null,
        bool count = false,
        string? expand = null)
    {
        return GetODataAsync<Feature>("Feature", top, skip, orderby, filter, count, expand);
    }

    public async Task<Feature?> GetFeatureByIdAsync(long key)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Get, $"/api/feature/{key}");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<Feature>();
    }

    public async Task UpdateFeatureAsync(long key, Feature data)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Put, $"/api/feature/{key}");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(data);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);
    }

    public async Task<Feature?> InsertFeatureAsync(Feature data)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Post, "/api/feature");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(data);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<Feature>();
    }

    public async Task DeleteFeatureAsync(long key)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Delete, $"/api/feature/{key}");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);
    }

    public async Task<ProductReview[]?> ListProductReviewAsync()
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Get, "/api/productreview");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<ProductReview[]>();
    }

    public Task<ODataResult<ProductReview>?> ListProductReviewODataAsync(
        int? top = null,
        int? skip = null,
        string? orderby = null,
        string? filter = null,
        bool count = false,
        string? expand = null)
    {
        return GetODataAsync<ProductReview>("ProductReview", top, skip, orderby, filter, count, expand);
    }

    public async Task<ProductReview?> GetProductReviewByIdAsync(long key)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Get, $"/api/productreview/{key}");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<ProductReview>();
    }

    public async Task UpdateProductReviewAsync(long key, ProductReview data)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Put, $"/api/productreview/{key}");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(data);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);
    }

    public async Task<ProductReview?> InsertProductReviewAsync(ProductReview data)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Post, "/api/productreview");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(data);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<ProductReview>();
    }

    public async Task DeleteProductReviewAsync(long key)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Delete, $"/api/productreview/{key}");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);
    }

    public async Task<Tag[]?> ListTagAsync()
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Get, "/api/tag");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<Tag[]>();
    }

    public Task<ODataResult<Tag>?> ListTagODataAsync(
        int? top = null,
        int? skip = null,
        string? orderby = null,
        string? filter = null,
        bool count = false,
        string? expand = null)
    {
        return GetODataAsync<Tag>("Tag", top, skip, orderby, filter, count, expand);
    }

    public async Task<Tag?> GetTagByIdAsync(long key)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Get, $"/api/tag/{key}");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<Tag>();
    }

    public async Task UpdateTagAsync(long key, Tag data)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Put, $"/api/tag/{key}");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(data);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);
    }

    public async Task<Tag?> InsertTagAsync(Tag data)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Post, "/api/tag");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(data);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<Tag>();
    }

    public async Task DeleteTagAsync(long key)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Delete, $"/api/tag/{key}");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);
    }

    public async Task<string?> UploadImageAsync(Stream stream, int bufferSize, string contentType)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        MultipartFormDataContent content = [];
        StreamContent fileContent = new(stream, bufferSize);
        fileContent.Headers.ContentType = new(contentType);
        content.Add(fileContent, "image", "image");

        HttpRequestMessage request = new(HttpMethod.Post, $"/api/image");
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Content = content;

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<string>();
    }

    public async Task<string?> UploadImageAsync(IBrowserFile image)
    {
        using var stream = image.OpenReadStream(image.Size);

        return await UploadImageAsync(stream, Convert.ToInt32(image.Size), image.ContentType);
    }

    public async Task ChangePasswordAsync(string oldPassword, string newPassword)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Post, $"/identity/manage/info");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(new { oldPassword, newPassword });

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);
    }

    public async Task ModifyRolesAsync(string key, IEnumerable<string> roles)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Put, $"/api/user/{key}/roles");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(roles);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);
    }

    public async Task<(string answer, IEnumerable<ProductSource> sources)> AskProductChatAsync(string question)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        HttpRequestMessage request = new(HttpMethod.Post, "/api/chat/products");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(new { question });
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
        var payload = await response.Content.ReadFromJsonAsync<ProductChatResponse>() ?? throw new Exception("Invalid response");
        return (payload.Answer, payload.Sources ?? Enumerable.Empty<ProductSource>());
    }

    public record ProductChatResponse(string Answer, IEnumerable<ProductSource>? Sources);
    public record ProductSource(long? Id, string? Name, decimal? Price);
}
