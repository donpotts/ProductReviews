using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using System.Net;
using System.Globalization;
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

        // Don't call EnsureSuccessStatusCode() since we handle status codes manually
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

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException("Authentication required");
        }

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

    public async Task<object?> BulkUpsertCategoryAsync(List<Category> categories)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Post, "/api/category/bulk-upsert");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(categories);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<object>();
    }

    public async Task<object?> BulkUpsertBrandAsync(List<Brand> brands)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Post, "/api/brand/bulk-upsert");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(brands);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<object>();
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

    public async Task<object?> BulkUpsertFeatureAsync(List<Feature> features)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Post, "/api/feature/bulk-upsert");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(features);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<object>();
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

    public async Task<object?> BulkUpsertTagAsync(List<Tag> tags)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Post, "/api/tag/bulk-upsert");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(tags);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<object>();
    }

    public async Task<object?> BulkUpsertProductAsync(List<Product> products)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Post, "/api/product/bulk-upsert");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(products);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<object>();
    }

    public async Task<object?> BulkUpsertProductReviewAsync(List<ProductReview> productReviews)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Post, "/api/productreview/bulk-upsert");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(productReviews);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<object>();
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

    // Notification methods
    public async Task<ODataResult<Notification>?> ListNotificationODataAsync(
        int? top = null,
        int? skip = null,
        string? orderby = null,
        string? filter = null,
        bool count = false,
        string? expand = null)
    {
        return await GetODataAsync<Notification>("notification", top, skip, orderby, filter, count, expand);
    }

    public async Task<Notification[]?> ListNotificationAsync()
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        HttpRequestMessage request = new(HttpMethod.Get, "/api/notification");
        request.Headers.Authorization = new("Bearer", token);
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
        return await response.Content.ReadFromJsonAsync<Notification[]>();
    }

    public async Task<Notification?> GetNotificationByIdAsync(long key)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        HttpRequestMessage request = new(HttpMethod.Get, $"/api/notification/{key}");
        request.Headers.Authorization = new("Bearer", token);
        var response = await httpClient.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        await HandleResponseErrorsAsync(response);
        return await response.Content.ReadFromJsonAsync<Notification>();
    }

    public async Task UpdateNotificationAsync(long key, Notification data)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        HttpRequestMessage request = new(HttpMethod.Put, $"/api/notification/{key}");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(data);
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
    }

    public async Task<Notification?> InsertNotificationAsync(Notification data)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        HttpRequestMessage request = new(HttpMethod.Post, "/api/notification");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(data);
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
        return await response.Content.ReadFromJsonAsync<Notification>();
    }

    public async Task<NotificationDispatchResponse?> SendNotificationAsync(NotificationDispatchRequest requestPayload)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        HttpRequestMessage request = new(HttpMethod.Post, "/api/notification/send");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(requestPayload);
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
        return await response.Content.ReadFromJsonAsync<NotificationDispatchResponse>();
    }

    public async Task DeleteNotificationAsync(long key)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        HttpRequestMessage request = new(HttpMethod.Delete, $"/api/notification/{key}");
        request.Headers.Authorization = new("Bearer", token);
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
    }

    public async Task MarkNotificationAsReadAsync(long id)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        HttpRequestMessage request = new(HttpMethod.Post, $"/api/notification/markAsRead/{id}");
        request.Headers.Authorization = new("Bearer", token);
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
    }

    public async Task MarkAllNotificationsAsReadAsync()
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        HttpRequestMessage request = new(HttpMethod.Post, "/api/notification/markAllAsRead");
        request.Headers.Authorization = new("Bearer", token);
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
    }

    public async Task<int> GetUnreadNotificationCountAsync()
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        HttpRequestMessage request = new(HttpMethod.Get, "/api/notification/unreadCount");
        request.Headers.Authorization = new("Bearer", token);
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
        return await response.Content.ReadFromJsonAsync<int>();
    }

    public async Task<object?> BulkUpsertNotificationAsync(IEnumerable<Notification> data)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        HttpRequestMessage request = new(HttpMethod.Post, "/api/notification/bulkUpsert");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(data);
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
        return await response.Content.ReadFromJsonAsync<object>();
    }

    // Shopping Cart Methods
    public async Task<ShoppingCart?> GetShoppingCartAsync()
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        HttpRequestMessage request = new(HttpMethod.Get, "/api/shoppingcart");
        request.Headers.Authorization = new("Bearer", token);
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
        return await response.Content.ReadFromJsonAsync<ShoppingCart>();
    }

    public async Task<ShoppingCart?> AddToCartAsync(long productId, int quantity = 1)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        var requestData = new { ProductId = productId, Quantity = quantity };
        HttpRequestMessage request = new(HttpMethod.Post, "/api/shoppingcart/add-item");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(requestData);
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
        return await response.Content.ReadFromJsonAsync<ShoppingCart>();
    }

    public async Task<ShoppingCart?> UpdateCartItemAsync(long itemId, int quantity)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        var requestData = new { Quantity = quantity };
        HttpRequestMessage request = new(HttpMethod.Put, $"/api/shoppingcart/update-item/{itemId}");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(requestData);
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
        return await response.Content.ReadFromJsonAsync<ShoppingCart>();
    }

    public async Task<ShoppingCart?> RemoveFromCartAsync(long itemId)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        HttpRequestMessage request = new(HttpMethod.Delete, $"/api/shoppingcart/remove-item/{itemId}");
        request.Headers.Authorization = new("Bearer", token);
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
        return await response.Content.ReadFromJsonAsync<ShoppingCart>();
    }

    public async Task<ShoppingCart?> ClearCartAsync()
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        HttpRequestMessage request = new(HttpMethod.Delete, "/api/shoppingcart/clear");
        request.Headers.Authorization = new("Bearer", token);
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
        return await response.Content.ReadFromJsonAsync<ShoppingCart>();
    }

    // Product Search Methods
    public async Task<Product[]?> SearchProductsAsync(string? query = null, long? categoryId = null, long? brandId = null, 
        decimal? minPrice = null, decimal? maxPrice = null, bool? inStock = null)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        
        var queryString = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrEmpty(query)) queryString.Add("query", query);
    if (categoryId.HasValue) queryString.Add("categoryId", categoryId.Value.ToString(CultureInfo.InvariantCulture));
    if (brandId.HasValue) queryString.Add("brandId", brandId.Value.ToString(CultureInfo.InvariantCulture));
    if (minPrice.HasValue) queryString.Add("minPrice", minPrice.Value.ToString(CultureInfo.InvariantCulture));
    if (maxPrice.HasValue) queryString.Add("maxPrice", maxPrice.Value.ToString(CultureInfo.InvariantCulture));
    if (inStock.HasValue) queryString.Add("inStock", inStock.Value.ToString());
        
        var uri = $"/api/product/search?{queryString}";
        HttpRequestMessage request = new(HttpMethod.Get, uri);
        request.Headers.Authorization = new("Bearer", token);
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
        return await response.Content.ReadFromJsonAsync<Product[]>();
    }

    // Order Methods
    public async Task<Order[]?> ListOrderAsync(
        string? orderby = null,
        string? filter = null,
        string? expand = null)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        var queryString = HttpUtility.ParseQueryString(string.Empty);

        if (!string.IsNullOrEmpty(orderby))
        {
            queryString.Add("$orderby", orderby);
        }

        if (!string.IsNullOrEmpty(filter))
        {
            queryString.Add("$filter", filter);
        }

        if (!string.IsNullOrEmpty(expand))
        {
            queryString.Add("$expand", expand);
        }

        var query = queryString.ToString();
        var uri = string.IsNullOrEmpty(query) ? "/api/order" : $"/api/order?{query}";

        HttpRequestMessage request = new(HttpMethod.Get, uri);
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<Order[]>();
    }

    public Task<ODataResult<Order>?> ListOrderODataAsync(
        int? top = null,
        int? skip = null,
        string? orderby = null,
        string? filter = null,
        bool count = false,
        string? expand = null)
    {
        return GetODataAsync<Order>("Order", top, skip, orderby, filter, count, expand);
    }

    public async Task<Order?> GetOrderByIdAsync(long key)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        HttpRequestMessage request = new(HttpMethod.Get, $"/api/order/{key}");
        request.Headers.Authorization = new("Bearer", token);
        var response = await httpClient.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        if (response.StatusCode == HttpStatusCode.Unauthorized) 
            throw new UnauthorizedAccessException("Authentication required");
        await HandleResponseErrorsAsync(response);
        return await response.Content.ReadFromJsonAsync<Order>();
    }

    public async Task<Order?> CreateOrderFromCartAsync(string shippingAddress, string billingAddress, 
        string billingStateCode, decimal? shippingAmount = null, PaymentMethod paymentMethod = PaymentMethod.CreditCard, 
        string? notes = null)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        var requestData = new 
        { 
            ShippingAddress = shippingAddress,
            BillingAddress = billingAddress,
            BillingStateCode = billingStateCode,
            ShippingAmount = shippingAmount,
            PaymentMethod = paymentMethod,
            Notes = notes
        };
        HttpRequestMessage request = new(HttpMethod.Post, "/api/order/create-from-cart");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(requestData);
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
        return await response.Content.ReadFromJsonAsync<Order>();
    }

    public async Task<Order?> CancelOrderAsync(long orderId)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        HttpRequestMessage request = new(HttpMethod.Post, $"/api/order/{orderId}/cancel");
        request.Headers.Authorization = new("Bearer", token);
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
        return await response.Content.ReadFromJsonAsync<Order>();
    }

    // Tax Methods
    public async Task<TaxCalculationResult?> CalculateTaxAsync(decimal subtotal, string stateCode)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        var requestData = new TaxCalculationRequest { Subtotal = subtotal, StateCode = stateCode };
        HttpRequestMessage request = new(HttpMethod.Post, "/api/taxrate/calculate");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(requestData);
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
        return await response.Content.ReadFromJsonAsync<TaxCalculationResult>();
    }

    public async Task<TaxRate[]?> GetTaxRatesAsync()
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        HttpRequestMessage request = new(HttpMethod.Get, "/api/taxrate");
        request.Headers.Authorization = new("Bearer", token);
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
        return await response.Content.ReadFromJsonAsync<TaxRate[]>();
    }

    public async Task<ShippingRate?> GetDefaultShippingRateAsync()
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Get, "/api/shippingrate/default");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);

        await HandleResponseErrorsAsync(response);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ShippingRate>();
    }

    // Payment Methods
    public async Task<CreateStripeSessionResponse?> CreateStripeSessionAsync(string baseUrl, string? shippingAddress = null, 
        string? billingAddress = null, string? billingStateCode = null, decimal shippingAmount = 0, string? notes = null)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        var requestData = new CreateStripeSessionRequest
        { 
            BaseUrl = baseUrl,
            ShippingAddress = shippingAddress,
            BillingAddress = billingAddress,
            BillingStateCode = billingStateCode,
            ShippingAmount = shippingAmount,
            Notes = notes
        };
        HttpRequestMessage request = new(HttpMethod.Post, "/api/payment/create-stripe-session");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(requestData);
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
        return await response.Content.ReadFromJsonAsync<CreateStripeSessionResponse>();
    }

    public async Task<Order?> ConfirmPaymentAsync(PaymentMethod paymentMethod, string? stripeSessionId = null, 
        string? shippingAddress = null, string? billingAddress = null, string billingStateCode = "", 
        decimal? shippingAmount = null, string? notes = null)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync() ?? throw new Exception("Not authorized");
        var requestData = new 
        { 
            PaymentMethod = paymentMethod,
            StripeSessionId = stripeSessionId,
            ShippingAddress = shippingAddress,
            BillingAddress = billingAddress,
            BillingStateCode = billingStateCode,
            ShippingAmount = shippingAmount,
            Notes = notes
        };
        HttpRequestMessage request = new(HttpMethod.Post, "/api/payment/confirm-payment");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(requestData);
        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
        return await response.Content.ReadFromJsonAsync<Order>();
    }

    public async Task<Order?> CancelPaymentAsync(string stripeSessionId)
    {
        if (string.IsNullOrWhiteSpace(stripeSessionId))
        {
            throw new ArgumentException("Stripe session ID is required.", nameof(stripeSessionId));
        }

        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        var requestData = new CancelPaymentRequest
        {
            StripeSessionId = stripeSessionId
        };

        HttpRequestMessage request = new(HttpMethod.Post, "/api/payment/cancel-payment");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(requestData);

        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadFromJsonAsync<Order>();
    }

    // Stripe Checkout Methods
    public async Task<(string? SessionId, string? Url, string? SuccessUrl, string? CancelUrl, string? ErrorMessage)> CreateCheckoutSessionAsync(List<CartProduct> cart)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Post, "/api/payment/checkout");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(cart);

        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync();
            return (null, null, null, null, errorMessage);
        }

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        return (
            result?["sessionId"],
            result?["url"],
            result?["successUrl"],
            result?["cancelUrl"],
            null
        );
    }

    // Email Methods
    public async Task SendOrderConfirmationEmailAsync(long orderId, string customerEmail)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Post, $"/api/email/order-confirmation/{orderId}");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(new { CustomerEmail = customerEmail });

        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
    }

    public async Task SendCheckoutCancellationEmailAsync(List<CartProduct> cart, string? sessionId = null)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Post, "/api/email/checkout-cancellation");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(new { Cart = cart, SessionId = sessionId });

        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
    }

    public async Task SendOrderCancellationEmailAsync(long orderId)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Post, $"/api/email/order-cancellation/{orderId}");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);
    }

    public async Task<byte[]> DownloadOrderPdfAsync(long orderId)
    {
        var token = await authenticationStateProvider.GetBearerTokenAsync()
            ?? throw new Exception("Not authorized");

        HttpRequestMessage request = new(HttpMethod.Get, $"/api/order/{orderId}/pdf");
        request.Headers.Authorization = new("Bearer", token);

        var response = await httpClient.SendAsync(request);
        await HandleResponseErrorsAsync(response);

        return await response.Content.ReadAsByteArrayAsync();
    }

}
