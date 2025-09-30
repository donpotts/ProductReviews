#pragma warning disable SKEXP0001, CS0618
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
    Task<(string answer, IEnumerable<Product> productSources, IEnumerable<Service> serviceSources, IEnumerable<Order> orderSources, IEnumerable<ServiceOrder> serviceOrderSources)> AskAsync(string question, CancellationToken ct = default);
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

    private static string BuildServiceText(Service s)
    {
        var pricing = new List<string>();
        if (s.HourlyRate.HasValue) pricing.Add($"Hourly: {s.HourlyRate:C}");
        if (s.DailyRate.HasValue) pricing.Add($"Daily: {s.DailyRate:C}");
        if (s.ProjectRate.HasValue) pricing.Add($"Project: {s.ProjectRate:C}");
        
        var sb = new StringBuilder();
        sb.AppendLine($"Service Id: {s.Id}");
        sb.AppendLine($"Name: {s.Name}");
        sb.AppendLine($"Description: {s.Description}");
        sb.AppendLine($"Category: {string.Join(", ", s.ServiceCategory?.Select(c => c.Name) ?? new[] { "Not specified" })}");
        sb.AppendLine($"Company: {string.Join(", ", s.ServiceCompany?.Select(c => c.Name) ?? new[] { "Not specified" })}");
        sb.AppendLine($"Pricing: {string.Join(", ", pricing)}");
        sb.AppendLine($"Duration: {s.EstimatedDurationHours} hours");
        sb.AppendLine($"Complexity: {s.Complexity}");
        sb.AppendLine($"Pricing Type: {s.PricingType}");
        sb.AppendLine($"On-site Required: {(s.RequiresOnsite ? "Yes" : "No")}");
        sb.AppendLine($"Availability: {(s.IsAvailable ? "Available" : "Not Available")}");
        return sb.ToString().TrimEnd();
    }

    private static string BuildOrderText(Order o)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Order Id: {o.Id}");
        sb.AppendLine($"Order Number: {o.OrderNumber}");
        sb.AppendLine($"Order Date: {o.OrderDate:yyyy-MM-dd}");
        sb.AppendLine($"Status: {o.Status}");
        sb.AppendLine($"Total Amount: {o.TotalAmount:C}");
        sb.AppendLine($"Payment Method: {o.PaymentMethod}");
        sb.AppendLine($"Items: {o.Items?.Count ?? 0}");
        if (o.EstimatedDeliveryDate.HasValue)
            sb.AppendLine($"Estimated Delivery: {o.EstimatedDeliveryDate:yyyy-MM-dd}");
        if (o.ActualDeliveryDate.HasValue)
            sb.AppendLine($"Delivered: {o.ActualDeliveryDate:yyyy-MM-dd}");
        if (!string.IsNullOrEmpty(o.TrackingNumber))
            sb.AppendLine($"Tracking: {o.TrackingNumber}");
        return sb.ToString().TrimEnd();
    }

    private static string BuildServiceOrderText(ServiceOrder so)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Service Order Id: {so.Id}");
        sb.AppendLine($"Order Number: {so.OrderNumber}");
        sb.AppendLine($"Order Date: {so.OrderDate:yyyy-MM-dd}");
        sb.AppendLine($"Status: {so.Status}");
        sb.AppendLine($"Total Amount: {so.TotalAmount:C}");
        sb.AppendLine($"Payment Method: {so.PaymentMethod}");
        sb.AppendLine($"Services: {so.Items?.Count ?? 0}");
        if (so.ScheduledStartDate.HasValue)
            sb.AppendLine($"Scheduled Start: {so.ScheduledStartDate:yyyy-MM-dd}");
        if (so.ScheduledEndDate.HasValue)
            sb.AppendLine($"Scheduled End: {so.ScheduledEndDate:yyyy-MM-dd}");
        if (so.RequiresOnsite)
            sb.AppendLine($"On-site Required: Yes");
        if (!string.IsNullOrEmpty(so.ContactPerson))
            sb.AppendLine($"Contact: {so.ContactPerson}");
        return sb.ToString().TrimEnd();
    }

    private static bool IsServiceRelatedQuestion(string question)
    {
        var serviceKeywords = new[] 
        { 
            "service", "services", "consulting", "support", "installation", "maintenance", 
            "repair", "setup", "configuration", "training", "implementation", "deployment",
            "what do you offer", "what services", "professional services", "technical support",
            "help with", "assistance", "expert", "specialist"
        };
        
        return serviceKeywords.Any(keyword => question.ToLowerInvariant().Contains(keyword));
    }

    private static bool IsOrderRelatedQuestion(string question)
    {
        var orderKeywords = new[] 
        { 
            "order", "orders", "my order", "my orders", "purchase", "purchases", "bought", "buy",
            "order status", "order history", "tracking", "delivery", "shipped", "invoice",
            "receipt", "payment", "refund", "return", "when will", "where is", "order number"
        };
        
        return orderKeywords.Any(keyword => question.ToLowerInvariant().Contains(keyword));
    }

    private static bool IsReviewRelatedQuestion(string question)
    {
        var reviewKeywords = new[] 
        { 
            "review", "reviews", "rating", "ratings", "feedback", "testimonial", "testimonials",
            "customer feedback", "opinions", "what people say", "customer reviews", "star rating",
            "quality", "satisfaction", "recommend", "recommendation"
        };
        
        return reviewKeywords.Any(keyword => question.ToLowerInvariant().Contains(keyword));
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

    // Detect best-selling / most popular product request
    private static bool IsBestSellingRequest(string question)
    {
        if (string.IsNullOrWhiteSpace(question)) return false;
        var q = question.ToLowerInvariant();
        string[] patterns =
        [
            "best selling",
            "best-selling", 
            "most popular",
            "top selling",
            "most sold",
            "bestseller",
            "best seller",
            "most ordered",
            "top rated",
            "most bought",
            "popular products",
            "trending products",
            "top products"
        ];
        return patterns.Any(p => q.Contains(p));
    }

    public async Task<(string answer, IEnumerable<Product> productSources, IEnumerable<Service> serviceSources, IEnumerable<Order> orderSources, IEnumerable<ServiceOrder> serviceOrderSources)> AskAsync(string question, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);

        if(!_aiAvailable)
        {
            return ("AI not available (invalid or missing key).", Enumerable.Empty<Product>(), Enumerable.Empty<Service>(), Enumerable.Empty<Order>(), Enumerable.Empty<ServiceOrder>());
        }

        bool lowestRequest = IsLowestPriceRequest(question);
        bool bestSellingRequest = IsBestSellingRequest(question);

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

        List<Product> bestSellingProducts = [];
        if (bestSellingRequest)
        {
            try
            {
                // Get best-selling products based on order quantities
                bestSellingProducts = await db.OrderItem
                    .Include(oi => oi.Product)
                        .ThenInclude(p => p!.Brand)
                    .Include(oi => oi.Product)
                        .ThenInclude(p => p!.Category)
                    .Include(oi => oi.Product)
                        .ThenInclude(p => p!.Feature)
                    .Include(oi => oi.Product)
                        .ThenInclude(p => p!.Tag)
                    .Where(oi => oi.Product != null)
                    .GroupBy(oi => oi.Product!.Id)
                    .Select(g => new { 
                        ProductId = g.Key, 
                        TotalQuantity = g.Sum(oi => oi.Quantity),
                        Product = g.First().Product
                    })
                    .OrderByDescending(x => x.TotalQuantity)
                    .Take(5)
                    .Select(x => x.Product!)
                    .ToListAsync(ct);

                // If no order data, fall back to highest rated products
                if (!bestSellingProducts.Any())
                {
                    var topRatedProductIds = await db.ProductReview
                        .Where(pr => pr.ProductId.HasValue && pr.Rating.HasValue)
                        .GroupBy(pr => pr.ProductId!.Value)
                        .Select(g => new {
                            ProductId = g.Key,
                            AvgRating = g.Average(pr => pr.Rating!.Value),
                            ReviewCount = g.Count()
                        })
                        .Where(x => x.ReviewCount >= 2) // At least 2 reviews
                        .OrderByDescending(x => x.AvgRating)
                        .ThenByDescending(x => x.ReviewCount)
                        .Take(5)
                        .Select(x => x.ProductId)
                        .ToListAsync(ct);

                    if (topRatedProductIds.Any())
                    {
                        bestSellingProducts = await db.Product
                            .Include(p => p.Brand)
                            .Include(p => p.Category)
                            .Include(p => p.Feature)
                            .Include(p => p.Tag)
                            .Where(p => p.Id.HasValue && topRatedProductIds.Contains(p.Id.Value))
                            .ToListAsync(ct);
                    }
                }

                // Merge with existing products
                foreach (var product in bestSellingProducts)
                {
                    if (!products.Any(p => p.Id == product.Id))
                    {
                        products.Add(product);
                    }
                }

                // If no products yet, use best-selling as primary
                if (!products.Any() && bestSellingProducts.Any())
                {
                    products = bestSellingProducts;
                }
            }
            catch
            {
                // ignore retrieval issues
            }
        }

        // Get services data for service-related questions
        var services = new List<Service>();
        if (IsServiceRelatedQuestion(question))
        {
            try
            {
                services = await db.Service
                    .Include(s => s.ServiceCategory)
                    .Include(s => s.ServiceCompany)
                    .ToListAsync(ct);
            }
            catch
            {
                // ignore if services not available
            }
        }

        // Get orders data for order-related questions
        var orders = new List<Order>();
        var serviceOrders = new List<ServiceOrder>();
        if (IsOrderRelatedQuestion(question))
        {
            try
            {
                orders = await db.Order
                    .Include(o => o.Items)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .ToListAsync(ct);
            }
            catch
            {
                // ignore if orders not available
            }

            try
            {
                serviceOrders = await db.ServiceOrder
                    .Include(so => so.Items)
                    .Include(so => so.Expenses)
                    .OrderByDescending(so => so.OrderDate)
                    .Take(10)
                    .ToListAsync(ct);
            }
            catch
            {
                // ignore if service orders not available
            }
        }

        var productContext = products.Any() ? string.Join("\n\n---\n\n", products.Select(p => BuildProductText(p))) : "";
        var serviceContext = services.Any() ? string.Join("\n\n---\n\n", services.Select(s => BuildServiceText(s))) : "";
        var orderContext = orders.Any() ? string.Join("\n\n---\n\n", orders.Select(o => BuildOrderText(o))) : "";
        var serviceOrderContext = serviceOrders.Any() ? string.Join("\n\n---\n\n", serviceOrders.Select(so => BuildServiceOrderText(so))) : "";
        
        var contextParts = new List<string>();
        if (!string.IsNullOrEmpty(productContext)) contextParts.Add($"PRODUCTS:\n{productContext}");
        if (!string.IsNullOrEmpty(serviceContext)) contextParts.Add($"SERVICES:\n{serviceContext}");
        if (!string.IsNullOrEmpty(orderContext)) contextParts.Add($"ORDERS:\n{orderContext}");
        if (!string.IsNullOrEmpty(serviceOrderContext)) contextParts.Add($"SERVICE ORDERS:\n{serviceOrderContext}");
        
        var contextBlock = contextParts.Any() ? string.Join("\n\n===\n\n", contextParts) : "(no context available)";

        var systemPrompt = "You are a helpful assistant for products, services, and orders. You MUST use the provided context data to answer questions. When someone asks about services, use the SERVICES section. When someone asks about products, use the PRODUCTS section. When someone asks about orders, use the ORDERS and SERVICE ORDERS sections. ALWAYS provide helpful answers using the context data provided. Only say you cannot answer if no relevant context is provided.";
        if (lowestRequest && lowestProduct != null)
        {
            systemPrompt += " If the user asks for the lowest priced product, respond ONLY with that single product's name, Id and price (and optionally a brief spec) drawn from context.";
        }
        if (bestSellingRequest && bestSellingProducts.Any())
        {
            systemPrompt += " If the user asks for best-selling or most popular products, list the products with their names, Ids, and prices. If based on order data, mention sales performance; if based on ratings, mention customer ratings. Be concise and factual.";
        }
        var prompt = $"<system>\n{systemPrompt}\n</system>\n<context>\n{contextBlock}\n</context>\n<user_question>\n{question}\n</user_question>\n<instructions>Use the provided context to answer about products and services. Do not speculate. Cite IDs when mentioned.</instructions>";

        string answer;
        try
        {
            var history = new ChatHistory();
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage($"Context:\n{contextBlock}\n\nQuestion: {question}");
            var result = await chatService.GetChatMessageContentAsync(history, cancellationToken: ct);
            answer = result.Content ?? "(no answer)";
        }
        catch (HttpOperationException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            _aiAvailable = false;
            return ("AI not available (invalid key).", products, services, orders, serviceOrders);
        }
        catch
        {
            _aiAvailable = false;
            return ("AI not available (error calling model).", products, services, orders, serviceOrders);
        }

        if((!products.Any() && !services.Any() && !orders.Any() && !serviceOrders.Any()) || answer.Contains("I don't know", StringComparison.OrdinalIgnoreCase))
        {
            answer = "I can only answer questions about the products, services, and orders in our system.";
        }
        var forbidden = new []{"politics","weather","news","sports"};
        if(forbidden.Any(f => answer.Contains(f, StringComparison.OrdinalIgnoreCase)))
        {
            answer = "I can only answer questions about the products, services, and orders in our system.";
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
            var displayName = lowestProduct.GetDisplayName();
            var name = string.IsNullOrWhiteSpace(displayName) ? lowestProduct.Name ?? string.Empty : displayName;
            if (!answer.Contains(idStr, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(name) && !answer.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                var priceStr = lowestProduct.Price?.ToString("0.00", CultureInfo.InvariantCulture) ?? "unknown";
                answer = $"Lowest priced product: {name} (Id {idStr}) at price {priceStr}.";
            }
        }

        // Guarantee best-selling products shown if requested
        if (bestSellingRequest && bestSellingProducts.Any())
        {
            bool hasOrderData = false;
            try
            {
                hasOrderData = await db.OrderItem.AnyAsync(ct);
            }
            catch
            {
                // ignore check errors
            }

            var productNames = bestSellingProducts
                .Select(p =>
                {
                    var displayName = p.GetDisplayName();
                    return string.IsNullOrWhiteSpace(displayName) ? p.Name ?? string.Empty : displayName;
                })
                .ToList();
            bool anyMentioned = productNames.Any(name => !string.IsNullOrEmpty(name) && answer.Contains(name, StringComparison.OrdinalIgnoreCase));
            
            if (!anyMentioned)
            {
                var productList = bestSellingProducts.Take(3).Select(p =>
                {
                    var idStr = p.Id?.ToString(CultureInfo.InvariantCulture) ?? "";
                    var displayName = p.GetDisplayName();
                    var name = string.IsNullOrWhiteSpace(displayName) ? p.Name ?? "Unknown" : displayName;
                    var priceStr = p.Price?.ToString("0.00", CultureInfo.InvariantCulture) ?? "Price not available";
                    return $"{name} (Id {idStr}) - ${priceStr}";
                });

                var dataSource = hasOrderData ? "based on sales data" : "based on customer ratings";
                answer = $"Our best-selling products ({dataSource}):\n\n" + string.Join("\n", productList);
                
                if (bestSellingProducts.Count > 3)
                {
                    answer += $"\n\n...and {bestSellingProducts.Count - 3} more top-performing products.";
                }
            }
        }

        return (answer, products, services, orders, serviceOrders);
    }
}
#pragma warning restore SKEXP0001, CS0618
