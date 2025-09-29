using Microsoft.Graph;
using Microsoft.Graph.Models;
using Azure.Identity;
using AppProduct.Shared.Models;
using System.Net;
using System.Linq; // added
using System.Text; // new

namespace AppProduct.Services;

public interface IEmailNotificationService
{
    Task SendNewReviewNotificationAsync(ProductReview review, Product product);
    Task SendReviewResponseNotificationAsync(ProductReview review, Product product);
    Task SendOrderConfirmationAsync(Order order, string customerEmail);
    Task SendOrderCancellationAsync(Order order, string customerEmail);
    Task SendCheckoutCancellationEmailAsync(List<CartProduct> cart, string customerEmail);
    Task SendInvoiceEmailAsync(Order order, string customerEmail, byte[] invoicePdf);
    Task SendCashOrderEmailAsync(Order order, string customerEmail, byte[] deliveryReceiptPdf);
    
    // Service notifications
    Task SendNewServiceReviewNotificationAsync(ServiceReview review, Service service);
    Task SendServiceReviewResponseNotificationAsync(ServiceReview review, Service service);
    Task SendServiceOrderConfirmationAsync(ServiceOrder order, string customerEmail);
    Task SendServiceOrderStatusUpdateAsync(ServiceOrder order, string customerEmail, ServiceOrderStatus oldStatus);
    Task SendServiceCreatedNotificationAsync(Service service); // NEW
}

public class EmailNotificationService : IEmailNotificationService
{
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly IConfiguration _configuration;
    private GraphServiceClient? _graphServiceClient;

    public EmailNotificationService(ILogger<EmailNotificationService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        InitializeGraphClient();
    }

    private void InitializeGraphClient()
    {
        try
        {
            var clientId = _configuration["EmailSettings:ClientId"];
            var clientSecret = _configuration["EmailSettings:ClientSecret"];
            var tenantId = _configuration["EmailSettings:TenantId"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("Microsoft Graph configuration is missing. Email notifications will be disabled.");
                return;
            }

            var credentials = new ClientSecretCredential(tenantId, clientId, clientSecret);
            _graphServiceClient = new GraphServiceClient(credentials);

            _logger.LogInformation("Microsoft Graph client initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Microsoft Graph client.");
        }
    }

    // ---------- Product Review Emails ----------
    public async Task SendNewReviewNotificationAsync(ProductReview review, Product product)
    {
        if (_graphServiceClient == null)
        {
            _logger.LogWarning("Microsoft Graph client is not initialized. Cannot send email notification.");
            return;
        }

        try
        {
            var adminEmail = _configuration["EmailSettings:AdminEmail"];
            if (string.IsNullOrEmpty(adminEmail))
            {
                _logger.LogWarning("Admin email is not configured. Cannot send notification.");
                return;
            }

            var productDisplayName = product.GetDisplayName();
            if (string.IsNullOrWhiteSpace(productDisplayName))
            {
                productDisplayName = product.Name ?? "your product";
            }

            var subject = $"New Review Received for {productDisplayName}";
            var body = BuildNewReviewEmailBody(review, product, productDisplayName);

            var message = new Message
            {
                Subject = subject,
                Body = new ItemBody { ContentType = BodyType.Html, Content = body },
                ToRecipients = new List<Recipient>{ new() { EmailAddress = new EmailAddress { Address = adminEmail } } },
                ReplyTo = new List<Recipient>{ new() { EmailAddress = new EmailAddress { Address = review.CustomerEmail, Name = review.CustomerName } } }
            };

            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            if (string.IsNullOrWhiteSpace(senderEmail))
            {
                _logger.LogWarning("SenderEmail not configured");
                return;
            }
            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody { Message = message });
            _logger.LogInformation("New review notification sent successfully for review ID {ReviewId}", review.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send new review notification for review ID {ReviewId}", review.Id);
        }
    }

    public async Task SendReviewResponseNotificationAsync(ProductReview review, Product product)
    {
        if (_graphServiceClient == null || string.IsNullOrEmpty(review.CustomerEmail))
        {
            return;
        }

        try
        {
            var productDisplayName = product.GetDisplayName();
            if (string.IsNullOrWhiteSpace(productDisplayName))
                productDisplayName = product.Name ?? "your product";

            var subject = $"Response to Your Review of {productDisplayName}";
            var body = BuildResponseEmailBody(review, product, productDisplayName);

            var message = new Message
            {
                Subject = subject,
                Body = new ItemBody { ContentType = BodyType.Html, Content = body },
                ToRecipients = new List<Recipient>{ new() { EmailAddress = new EmailAddress { Address = review.CustomerEmail } } }
            };

            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            if (string.IsNullOrWhiteSpace(senderEmail))
            {
                _logger.LogWarning("SenderEmail not configured");
                return;
            }
            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody { Message = message });
            _logger.LogInformation("Review response notification sent to {CustomerEmail} for review {ReviewId}", review.CustomerEmail, review.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send review response notification for review {ReviewId}", review.Id);
        }
    }

    private string BuildNewReviewEmailBody(ProductReview review, Product product, string productDisplayName)
    {
        string E(string? v) => WebUtility.HtmlEncode(v ?? string.Empty);
        var rating = review.Rating?.ToString("0.0") ?? "N/A";
        var inner = $@"<h2 class='h'>New Product Review</h2>
<p>A new review was submitted for <strong>{E(productDisplayName)}</strong>.</p>
<table class='data'>
<tr><th>Field</th><th>Value</th></tr>
<tr><td>Reviewer</td><td>{E(review.CustomerName)}</td></tr>
<tr><td>Email</td><td>{E(review.CustomerEmail)}</td></tr>
<tr><td>Rating</td><td>{rating} / 5</td></tr>
<tr><td>Title</td><td>{E(review.Title)}</td></tr>
<tr><td>Date</td><td>{review.ReviewDate:yyyy-MM-dd HH:mm} UTC</td></tr>
<tr><td>Verified Purchase</td><td>{(review.IsVerifiedPurchase ? "Yes" : "No")}</td></tr>
</table>
<h3>Review Text</h3>
<p class='pre'>{E(review.ReviewText)}</p>";
        return WrapEmail(inner, "#ff9800", "#ff5722");
    }

    private string BuildResponseEmailBody(ProductReview review, Product product, string productDisplayName)
    {
        string E(string? v) => WebUtility.HtmlEncode(v ?? string.Empty);
        var inner = $@"<h2 class='h'>Response to Your Review</h2>
<p>Thank you for reviewing <strong>{E(productDisplayName)}</strong>. We have responded to your feedback.</p>
<h3>Your Original Review</h3>
<p><strong>Title:</strong> {E(review.Title)}</p>
<p class='pre'>{E(review.ReviewText)}</p>
<h3>Our Response</h3>
<p class='pre'>{E(review.Response)}</p>
<p class='meta'>Review Date: {review.ReviewDate:yyyy-MM-dd} | Response Date: {review.ResponseDate:yyyy-MM-dd}</p>";
        return WrapEmail(inner, "#00bcd4", "#2196f3");
    }

    // ---------- Order Emails ----------
    public async Task SendOrderConfirmationAsync(Order order, string customerEmail)
    {
        if (_graphServiceClient == null) { _logger.LogInformation("Graph client not available"); return; }
        var adminEmail = _configuration["EmailSettings:AdminEmail"]; if (string.IsNullOrEmpty(adminEmail)) { _logger.LogWarning("Admin email not configured"); return; }
        try
        {
            var subject = $"Order Confirmation - {order.OrderNumber}";
            var customerBody = GenerateOrderConfirmationEmail(order, customerEmail);
            var adminBody = GenerateOrderNotificationEmail(order, customerEmail);
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            if (string.IsNullOrWhiteSpace(senderEmail)) { _logger.LogWarning("SenderEmail not configured"); return; }
            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody { Message = new Message { Subject = subject, Body = new ItemBody { ContentType = BodyType.Html, Content = customerBody }, ToRecipients = new List<Recipient>{ new(){ EmailAddress = new EmailAddress{ Address = customerEmail } } } } });
            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody { Message = new Message { Subject = $"New Order Received - {order.OrderNumber}", Body = new ItemBody { ContentType = BodyType.Html, Content = adminBody }, ToRecipients = new List<Recipient>{ new(){ EmailAddress = new EmailAddress{ Address = adminEmail } } } } });
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to send order confirmation for order {OrderId}", order.Id); throw; }
    }

    public async Task SendOrderCancellationAsync(Order order, string customerEmail)
    {
        if (_graphServiceClient == null) { _logger.LogInformation("Graph client not available"); return; }
        var adminEmail = _configuration["EmailSettings:AdminEmail"]; if (string.IsNullOrEmpty(adminEmail)) { _logger.LogWarning("Admin email not configured"); return; }
        try
        {
            var subject = $"Order Cancelled - {order.OrderNumber}";
            var customerBody = GenerateOrderCancellationEmail(order, customerEmail);
            var adminBody = GenerateOrderCancellationNotificationEmail(order, customerEmail);
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            if (string.IsNullOrWhiteSpace(senderEmail)) { _logger.LogWarning("SenderEmail not configured"); return; }
            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(
                new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
                {
                    Message = new Message
                    {
                        Subject = subject,
                        Body = new ItemBody { ContentType = BodyType.Html, Content = customerBody },
                        ToRecipients = new List<Recipient>{ new(){ EmailAddress = new EmailAddress{ Address = customerEmail } } }
                    }
                });
            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(
                new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
                {
                    Message = new Message
                    {
                        Subject = subject,
                        Body = new ItemBody { ContentType = BodyType.Html, Content = adminBody },
                        ToRecipients = new List<Recipient>{ new(){ EmailAddress = new EmailAddress{ Address = adminEmail } } }
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order cancellation emails for order {OrderId}", order.Id);
            throw;
        }
    }

    public async Task SendCheckoutCancellationEmailAsync(List<CartProduct> cart, string customerEmail)
    {
        if (_graphServiceClient == null) { _logger.LogInformation("Graph client not available"); return; }
        var adminEmail = _configuration["EmailSettings:AdminEmail"]; if (string.IsNullOrEmpty(adminEmail)) { _logger.LogWarning("Admin email not configured"); return; }
        try
        {
            var subject = "Checkout Cancelled";
            var customerBody = GenerateCheckoutCancellationEmail(cart, customerEmail);
            var adminBody = GenerateCheckoutCancellationNotificationEmail(cart, customerEmail);
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            if (string.IsNullOrWhiteSpace(senderEmail)) { _logger.LogWarning("SenderEmail not configured"); return; }
            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(
                new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
                {
                    Message = new Message
                    {
                        Subject = subject,
                        Body = new ItemBody { ContentType = BodyType.Html, Content = customerBody },
                        ToRecipients = new List<Recipient>{ new(){ EmailAddress = new EmailAddress{ Address = customerEmail } } }
                    }
                });
            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(
                new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
                {
                    Message = new Message
                    {
                        Subject = "Customer Cancelled Checkout",
                        Body = new ItemBody { ContentType = BodyType.Html, Content = adminBody },
                        ToRecipients = new List<Recipient>{ new(){ EmailAddress = new EmailAddress{ Address = adminEmail } } }
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send checkout cancellation emails for {CustomerEmail}", customerEmail);
            throw;
        }
    }

    // ---------- Invoice Emails ----------
    public async Task SendInvoiceEmailAsync(Order order, string customerEmail, byte[] invoicePdf)
    {
        if (_graphServiceClient == null) { _logger.LogWarning("Graph client not initialized"); return; }
        var adminEmail = _configuration["EmailSettings:AdminEmail"]; if (string.IsNullOrEmpty(adminEmail)) { _logger.LogWarning("Admin email not configured"); return; }
        try
        {
            var subject = $"Invoice for Purchase Order - {order.OrderNumber}";
            var customerBody = GenerateInvoiceEmail(order, customerEmail);
            var adminBody = GenerateInvoiceNotificationEmail(order, customerEmail);
            var pdfAttachment = new FileAttachment { Name = $"Invoice-{order.OrderNumber}.pdf", ContentType = "application/pdf", ContentBytes = invoicePdf };
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            if (string.IsNullOrWhiteSpace(senderEmail)) { _logger.LogWarning("SenderEmail not configured"); return; }
            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody { Message = new Message { Subject = subject, Body = new ItemBody{ ContentType = BodyType.Html, Content = customerBody }, ToRecipients = new List<Recipient>{ new(){ EmailAddress = new EmailAddress{ Address = customerEmail } } }, Attachments = new List<Attachment>{ pdfAttachment } } });
            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody { Message = new Message { Subject = $"New Purchase Order Invoice Generated - {order.OrderNumber}", Body = new ItemBody{ ContentType = BodyType.Html, Content = adminBody }, ToRecipients = new List<Recipient>{ new(){ EmailAddress = new EmailAddress{ Address = adminEmail } } }, Attachments = new List<Attachment>{ pdfAttachment } } });
            _logger.LogInformation("Invoice emails sent for order {OrderId}", order.Id);
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to send invoice emails for order {OrderId}", order.Id); throw; }
    }

    public async Task SendCashOrderEmailAsync(Order order, string customerEmail, byte[] deliveryReceiptPdf)
    {
        if (_graphServiceClient == null) { _logger.LogWarning("Graph client not initialized"); return; }
        var adminEmail = _configuration["EmailSettings:AdminEmail"]; if (string.IsNullOrEmpty(adminEmail)) { _logger.LogWarning("Admin email not configured"); return; }
        try
        {
            var subject = $"Cash Order Delivery Receipt - {order.OrderNumber}";
            var customerBody = GenerateCashOrderEmail(order, customerEmail);
            var adminBody = GenerateCashOrderNotificationEmail(order, customerEmail);
            var pdfAttachment = new FileAttachment { Name = $"Delivery-Receipt-{order.OrderNumber}.pdf", ContentType = "application/pdf", ContentBytes = deliveryReceiptPdf };
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            if (string.IsNullOrWhiteSpace(senderEmail)) { _logger.LogWarning("SenderEmail not configured"); return; }
            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody { Message = new Message { Subject = subject, Body = new ItemBody{ ContentType = BodyType.Html, Content = customerBody }, ToRecipients = new List<Recipient>{ new(){ EmailAddress = new EmailAddress{ Address = customerEmail } } }, Attachments = new List<Attachment>{ pdfAttachment } } });
            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody { Message = new Message { Subject = $"New Cash Order for Delivery - {order.OrderNumber}", Body = new ItemBody{ ContentType = BodyType.Html, Content = adminBody }, ToRecipients = new List<Recipient>{ new(){ EmailAddress = new EmailAddress{ Address = adminEmail } } }, Attachments = new List<Attachment>{ pdfAttachment } } });
            _logger.LogInformation("Cash order emails sent for order {OrderId}", order.Id);
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to send cash order emails for order {OrderId}", order.Id); throw; }
    }

    // (Helper HTML builders for orders / invoices / cash orders are below)
    private string GenerateOrderConfirmationEmail(Order order, string customerEmail) => BuildOrderEmail("Order Confirmation", order, customerEmail, highlight: null, isCancellation:false, accent1:"#0d6efd", accent2:"#6610f2");
    private string GenerateOrderNotificationEmail(Order order, string customerEmail) => BuildOrderEmail("New Order Received (Admin)", order, customerEmail, highlight: null, isCancellation:false, admin:true, accent1:"#fd7e14", accent2:"#ffc107");
    private string GenerateOrderCancellationEmail(Order order, string customerEmail) => BuildOrderEmail("Order Cancelled", order, customerEmail, highlight: "#dc3545", isCancellation:true, accent1:"#dc3545", accent2:"#b02a37");
    private string GenerateOrderCancellationNotificationEmail(Order order, string customerEmail) => BuildOrderEmail("Order Cancelled (Admin)", order, customerEmail, highlight: "#dc3545", isCancellation:true, admin:true, accent1:"#dc3545", accent2:"#b02a37");
    private string GenerateCheckoutCancellationEmail(List<CartProduct> cart, string customerEmail) => BuildCheckoutCancelledEmail(cart, customerEmail, false, "#ff1744", "#d50000");
    private string GenerateCheckoutCancellationNotificationEmail(List<CartProduct> cart, string customerEmail) => BuildCheckoutCancelledEmail(cart, customerEmail, true, "#ff1744", "#d50000");
    private string GenerateInvoiceEmail(Order order, string customerEmail) => BuildOrderEmail("Invoice", order, customerEmail, highlight: "#6f42c1", isCancellation:false, isInvoice:true, accent1:"#6f42c1", accent2:"#6610f2");
    private string GenerateInvoiceNotificationEmail(Order order, string customerEmail) => BuildOrderEmail("Invoice Generated (Admin)", order, customerEmail, highlight: "#6f42c1", isCancellation:false, isInvoice:true, admin:true, accent1:"#6f42c1", accent2:"#6610f2");
    private string GenerateCashOrderEmail(Order order, string customerEmail) => BuildOrderEmail("Cash Order Delivery Receipt", order, customerEmail, highlight: "#198754", isCancellation:false, isCash:true, accent1:"#198754", accent2:"#20c997");
    private string GenerateCashOrderNotificationEmail(Order order, string customerEmail) => BuildOrderEmail("New Cash Order (Admin)", order, customerEmail, highlight: "#198754", isCancellation:false, isCash:true, admin:true, accent1:"#198754", accent2:"#20c997");

    // ---------- Service Emails ----------
    public async Task SendNewServiceReviewNotificationAsync(ServiceReview review, Service service)
    { if (_graphServiceClient == null) return; var adminEmail = _configuration["EmailSettings:AdminEmail"]; if (string.IsNullOrEmpty(adminEmail)) return; try { var senderEmail = _configuration["EmailSettings:SenderEmail"]; if (string.IsNullOrWhiteSpace(senderEmail)) { _logger.LogWarning("SenderEmail not configured"); return; } await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody { Message = new Message { Subject = $"New Service Review: {service.Name}", Body = new ItemBody{ ContentType = BodyType.Html, Content = GenerateServiceReviewNotificationEmail(review, service) }, ToRecipients = new List<Recipient>{ new(){ EmailAddress = new EmailAddress{ Address = adminEmail } } } } }); } catch (Exception ex) { _logger.LogError(ex, "Failed service review notification {ReviewId}", review.Id); } }
    public async Task SendServiceReviewResponseNotificationAsync(ServiceReview review, Service service)
    { if (_graphServiceClient == null || string.IsNullOrEmpty(review.CustomerEmail)) return; try { var senderEmail = _configuration["EmailSettings:SenderEmail"]; if (string.IsNullOrWhiteSpace(senderEmail)) { _logger.LogWarning("SenderEmail not configured"); return; } await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody { Message = new Message { Subject = $"Response to Your Service Review: {service.Name}", Body = new ItemBody{ ContentType = BodyType.Html, Content = GenerateServiceReviewResponseEmail(review, service) }, ToRecipients = new List<Recipient>{ new(){ EmailAddress = new EmailAddress{ Address = review.CustomerEmail } } } } }); } catch (Exception ex) { _logger.LogError(ex, "Failed service review response {ReviewId}", review.Id); } }
    public async Task SendServiceOrderConfirmationAsync(ServiceOrder order, string customerEmail)
    { if (_graphServiceClient == null) return; try { var senderEmail = _configuration["EmailSettings:SenderEmail"]; if (string.IsNullOrWhiteSpace(senderEmail)) { _logger.LogWarning("SenderEmail not configured"); return; } var adminEmail = _configuration["EmailSettings:AdminEmail"]; var customerBody = GenerateServiceOrderConfirmationEmail(order, customerEmail); await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody { Message = new Message { Subject = $"Service Order Confirmation - {order.OrderNumber}", Body = new ItemBody{ ContentType = BodyType.Html, Content = customerBody }, ToRecipients = new List<Recipient>{ new(){ EmailAddress = new EmailAddress{ Address = customerEmail } } } } }); if (!string.IsNullOrWhiteSpace(adminEmail)) { var adminBody = GenerateServiceOrderAdminConfirmationEmail(order, customerEmail); await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody { Message = new Message { Subject = $"New Service Order - {order.OrderNumber}", Body = new ItemBody{ ContentType = BodyType.Html, Content = adminBody }, ToRecipients = new List<Recipient>{ new(){ EmailAddress = new EmailAddress{ Address = adminEmail } } } } }); } } catch (Exception ex) { _logger.LogError(ex, "Failed service order confirmation {OrderId}", order.Id); } }
    public async Task SendServiceOrderStatusUpdateAsync(ServiceOrder order, string customerEmail, ServiceOrderStatus oldStatus)
    { if (_graphServiceClient == null) return; try { var senderEmail = _configuration["EmailSettings:SenderEmail"]; if (string.IsNullOrWhiteSpace(senderEmail)) { _logger.LogWarning("SenderEmail not configured"); return; } await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody { Message = new Message { Subject = $"Service Order Update - {order.OrderNumber}", Body = new ItemBody{ ContentType = BodyType.Html, Content = GenerateServiceOrderStatusUpdateEmail(order, customerEmail, oldStatus) }, ToRecipients = new List<Recipient>{ new(){ EmailAddress = new EmailAddress{ Address = customerEmail } } } } }); } catch (Exception ex) { _logger.LogError(ex, "Failed service order status update {OrderId}", order.Id); } }
    public async Task SendServiceCreatedNotificationAsync(Service service)
    { if (_graphServiceClient == null) { _logger.LogWarning("Graph client not initialized"); return; } var adminEmail = _configuration["EmailSettings:AdminEmail"]; if (string.IsNullOrWhiteSpace(adminEmail)) { _logger.LogWarning("Admin email not configured"); return; } try { var senderEmail = _configuration["EmailSettings:SenderEmail"]; if (string.IsNullOrWhiteSpace(senderEmail)) { _logger.LogWarning("SenderEmail not configured"); return; } await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody { Message = new Message { Subject = $"New Service Submitted: {service.Name}", Body = new ItemBody{ ContentType = BodyType.Html, Content = BuildServiceCreatedEmailBody(service) }, ToRecipients = new List<Recipient>{ new(){ EmailAddress = new EmailAddress{ Address = adminEmail } } } } }); } catch (Exception ex) { _logger.LogError(ex, "Failed to send service created notification {ServiceId}", service.Id); } }

    // Service email generators
    private string GenerateServiceReviewNotificationEmail(ServiceReview review, Service service)
    {
        string E(string? v) => WebUtility.HtmlEncode(v ?? string.Empty);
        var rating = review.Rating?.ToString("0.0") ?? "N/A";
        var inner = $@"<h2 class='h'>New Service Review</h2>
<p>Service: <strong>{E(service.Name)}</strong></p>
<table class='data'>
<tr><th>Field</th><th>Value</th></tr>
<tr><td>Reviewer</td><td>{E(review.CustomerName)}</td></tr>
<tr><td>Email</td><td>{E(review.CustomerEmail)}</td></tr>
<tr><td>Rating</td><td>{rating} / 5</td></tr>
<tr><td>Category</td><td>{review.ReviewCategory}</td></tr>
<tr><td>Date</td><td>{review.ReviewDate:yyyy-MM-dd HH:mm} UTC</td></tr>
</table>
<h3>Review Text</h3>
<p class='pre'>{E(review.ReviewText)}</p>";
        return WrapEmail(inner, "#ff9800", "#ff5722");
    }
    private string GenerateServiceReviewResponseEmail(ServiceReview review, Service service)
    {
        string E(string? v) => WebUtility.HtmlEncode(v ?? string.Empty);
        var inner = $@"<h2 class='h'>Response to Your Service Review</h2>
<p>Service: <strong>{E(service.Name)}</strong></p>
<h3>Your Review</h3>
<p class='pre'>{E(review.ReviewText)}</p>
<h3>Our Response</h3>
<p class='pre'>{E(review.Response)}</p>
<p class='meta'>Review Date: {review.ReviewDate:yyyy-MM-dd} | Response: {review.ResponseDate:yyyy-MM-dd}</p>";
        return WrapEmail(inner, "#00bcd4", "#2196f3");
    }
    private string GenerateServiceOrderConfirmationEmail(ServiceOrder order, string customerEmail) => BuildServiceOrderEmail("Service Order Confirmation", order, customerEmail, false, "#17a2b8", "#0d6efd");
    private string GenerateServiceOrderAdminConfirmationEmail(ServiceOrder order, string customerEmail) => BuildServiceOrderEmail("New Service Order (Admin)", order, customerEmail, true, "#17a2b8", "#0d6efd");
    private string GenerateServiceOrderStatusUpdateEmail(ServiceOrder order, string customerEmail, ServiceOrderStatus oldStatus) => BuildServiceOrderStatusUpdate(order, customerEmail, oldStatus, "#6c757d", "#343a40");

    // ---------- Shared Helpers ----------
    private string BuildServiceCreatedEmailBody(Service service)
    {
        string H(string? v) => WebUtility.HtmlEncode(v ?? string.Empty);
        string ToLines(string? v) => string.IsNullOrWhiteSpace(v) ? "<em>Not provided</em>" : WebUtility.HtmlEncode(v).Replace("\r\n", "\n").Replace('\r', '\n').Trim().Replace("\n", "<br>");
        var pricingType = service.PricingType.ToString();
        var rates = new List<string>();
        if (service.HourlyRate.HasValue) rates.Add($"Hourly: ${service.HourlyRate.Value:F2}");
        if (service.DailyRate.HasValue) rates.Add($"Daily: ${service.DailyRate.Value:F2}");
        if (service.ProjectRate.HasValue) rates.Add($"Project: ${service.ProjectRate.Value:F2}");
        if (!rates.Any()) rates.Add("<em>No rates specified</em>");
        var category = service.ServiceCategory?.FirstOrDefault()?.Name ?? "Uncategorized";
        var company = service.ServiceCompany?.FirstOrDefault()?.Name ?? "Unassigned";
        var inner = @$"<h2 class='h'>New Service Submitted: {H(service.Name)}</h2>
<p>A new service has been submitted. Review the details below:</p>
<div class='grid'>
  <div>
    <h3>Basic Information</h3>
    <p><strong>SKU:</strong> {H(service.SKU) ?? "<em>None</em>"}</p>
    <p><strong>Category:</strong> {H(category)}</p>
    <p><strong>Company:</strong> {H(company)}</p>
    <p><strong>Pricing Type:</strong> {pricingType}</p>
    <p><strong>Rates:</strong> {string.Join(" | ", rates)}</p>
    <p><strong>Availability:</strong> {(service.IsAvailable ? "Available" : "Unavailable")}</p>
    <p><strong>Requires On-site:</strong> {(service.RequiresOnsite ? "Yes" : "No")}</p>
    <p><strong>Includes Travel:</strong> {(service.IncludesTravel ? "Yes" : "No")}</p>
    {(service.EstimatedDurationHours.HasValue ? $"<p><strong>Estimated Duration (hrs):</strong> {service.EstimatedDurationHours}</p>" : "")}
  </div>
</div>
<h3>Description</h3><p>{ToLines(service.Description)}</p>
<h3>Requirements</h3><p>{ToLines(service.Requirements)}</p>
<h3>Deliverables</h3><p>{ToLines(service.Deliverables)}</p>
<h3>Notes</h3><p>{ToLines(service.Notes)}</p>
{(string.IsNullOrWhiteSpace(service.ImageUrl) ? string.Empty : $"<p><strong>Image URL:</strong> {H(service.ImageUrl)}</p>")}";
        return WrapEmail(inner, "#673ab7", "#3f51b5");
    }

    private string BuildOrderEmail(string title, Order order, string customerEmail, string? highlight, bool isCancellation, bool admin = false, bool isInvoice = false, bool isCash = false, string accent1 = "#0d6efd", string accent2 = "#6610f2")
    {
        string E(string? v) => WebUtility.HtmlEncode(v ?? string.Empty);
        string Money(decimal? v) => v.HasValue ? v.Value.ToString("C2") : "$0.00";
        var sb = new StringBuilder();
        sb.Append($"<h2 class='h' style='color:{(highlight ?? accent1)}'>{E(title)}</h2>");
        if (isCancellation) sb.Append("<p class='badge danger'>Cancelled</p>");
        else if (isInvoice) sb.Append("<p class='lead'>Attached is your invoice. A summary is below.</p>");
        else if (isCash) sb.Append("<p class='lead'>Delivery receipt for your cash order.</p>");
        else sb.Append("<p class='lead'>Thank you for your purchase.</p>");
        sb.Append("<table class='data compact'><tr><th>Order #</th><th>Status</th><th>Date (UTC)</th><th>Payment</th><th>Customer</th></tr>");
        sb.Append($"<tr><td>{E(order.OrderNumber)}</td><td>{order.Status}</td><td>{order.OrderDate:yyyy-MM-dd HH:mm}</td><td>{order.PaymentMethod}</td><td>{E(customerEmail)}</td></tr></table>");
        sb.Append("<h3>Items</h3><table class='data wide'><tr><th>#</th><th style='text-align:left'>Item</th><th>Qty</th><th style='text-align:right'>Unit</th><th style='text-align:right'>Line Total</th></tr>");
        var idx = 1; foreach (var it in order.Items) { var name = it.Product?.GetDisplayName() ?? it.Product?.Name ?? (it.ProductId.HasValue ? $"Product {it.ProductId}" : "Item"); sb.Append($"<tr><td>{idx}</td><td>{E(name)}</td><td>{it.Quantity}</td><td class='num'>{Money(it.UnitPrice)}</td><td class='num'>{Money(it.TotalPrice)}</td></tr>"); idx++; }
        if (!order.Items.Any()) sb.Append("<tr><td colspan='5' class='empty'>No line items recorded.</td></tr>");
        sb.Append("</table>");
        sb.Append("<div class='totals'><table><tr><td>Subtotal</td><td class='num'>" + Money(order.Subtotal) + "</td></tr><tr><td>Tax</td><td class='num'>" + Money(order.TaxAmount) + "</td></tr><tr><td>Shipping</td><td class='num'>" + Money(order.ShippingAmount) + "</td></tr><tr class='grand'><td>Total</td><td class='num'>" + Money(order.TotalAmount) + "</td></tr>");
        if (order.EstimatedDeliveryDate.HasValue && !isCancellation) sb.Append($"<tr><td>Est. Delivery</td><td class='num'>{order.EstimatedDeliveryDate:yyyy-MM-dd}</td></tr>");
        if (!string.IsNullOrWhiteSpace(order.TrackingNumber)) sb.Append($"<tr><td>Tracking #</td><td class='num'>{E(order.TrackingNumber)}</td></tr>");
        sb.Append("</table></div>");
        if (!string.IsNullOrWhiteSpace(order.ShippingAddress) || !string.IsNullOrWhiteSpace(order.BillingAddress)) { sb.Append("<div class='cols'>"); if (!string.IsNullOrWhiteSpace(order.ShippingAddress)) sb.Append($"<div><h4>Shipping</h4><p class='pre'>{E(order.ShippingAddress)}</p></div>"); if (!string.IsNullOrWhiteSpace(order.BillingAddress)) sb.Append($"<div><h4>Billing</h4><p class='pre'>{E(order.BillingAddress)}</p></div>"); sb.Append("</div>"); }
        if (!string.IsNullOrWhiteSpace(order.Notes)) { sb.Append("<h3>Notes</h3><p class='pre'>" + E(order.Notes) + "</p>"); }
        if (admin) sb.Append("<p class='meta'>Internal notification – do not forward externally without review.</p>");
        return WrapEmail(sb.ToString(), accent1, accent2);
    }

    private string BuildCheckoutCancelledEmail(List<CartProduct> cart, string customerEmail, bool admin, string accent1, string accent2)
    {
        string E(string? v) => WebUtility.HtmlEncode(v ?? string.Empty);
        string Money(decimal? v) => v.HasValue ? v.Value.ToString("C2") : "$0.00";
        var subtotal = cart.Sum(c => (c.Price ?? 0m) * c.Quantity);
        var sb = new StringBuilder();
        sb.Append($"<h2 class='h'>{(admin ? "Customer Cancelled Checkout" : "Checkout Cancelled")}</h2>");
        if (admin) sb.Append($"<p class='meta'><strong>Customer:</strong> {E(customerEmail)}</p>"); else sb.Append("<p class='lead'>Your payment was cancelled. Items remain in your cart.</p>");
        sb.Append("<h3>Items In Cart</h3><table class='data wide'><tr><th>#</th><th style='text-align:left'>Item</th><th>Qty</th><th style='text-align:right'>Unit</th><th style='text-align:right'>Line Total</th></tr>");
        int i = 1; foreach (var item in cart) { var line = (item.Price ?? 0m) * item.Quantity; sb.Append($"<tr><td>{i}</td><td>{E(item.DisplayName)}</td><td>{item.Quantity}</td><td class='num'>{Money(item.Price)}</td><td class='num'>{Money(line)}</td></tr>"); i++; }
        if (!cart.Any()) sb.Append("<tr><td colspan='5' class='empty'>No items were found for this cancelled session.</td></tr>");
        sb.Append("</table><p><strong>Subtotal:</strong> " + Money(subtotal) + "</p>");
        if (!admin) sb.Append("<p>You can return and complete your purchase at any time.</p>");
        return WrapEmail(sb.ToString(), accent1, accent2);
    }

    private string BuildServiceOrderEmail(string title, ServiceOrder order, string customerEmail, bool admin, string accent1, string accent2)
    {
        string E(string? v) => WebUtility.HtmlEncode(v ?? string.Empty);
        string Money(decimal? v) => v.HasValue ? v.Value.ToString("C2") : "$0.00";
        var sb = new StringBuilder();
        sb.Append($"<h2 class='h'>{E(title)}</h2>");
        sb.Append("<table class='data compact'><tr><th>Order #</th><th>Status</th><th>Date (UTC)</th><th>Payment</th><th>Customer</th></tr>");
        sb.Append($"<tr><td>{E(order.OrderNumber)}</td><td>{order.Status}</td><td>{order.OrderDate:yyyy-MM-dd HH:mm}</td><td>{order.PaymentMethod}</td><td>{E(customerEmail)}</td></tr></table>");
        sb.Append("<h3>Service Items</h3><table class='data wide'><tr><th>#</th><th style='text-align:left'>Service</th><th>Hours</th><th style='text-align:right'>Unit</th><th style='text-align:right'>Line Total</th></tr>");
        int idx = 1; foreach (var it in order.Items) { var name = it.Service?.Name ?? (it.ServiceId.HasValue ? $"Service {it.ServiceId}" : "Service"); var hours = it.HoursActual ?? it.HoursEstimated ?? 1m; var total = it.TotalPrice ?? (it.UnitPrice ?? 0) * hours; sb.Append($"<tr><td>{idx}</td><td>{E(name)}</td><td>{hours:0.##}</td><td class='num'>{Money(it.UnitPrice)}</td><td class='num'>{Money(total)}</td></tr>"); idx++; }
        if (!order.Items.Any()) sb.Append("<tr><td colspan='5' class='empty'>No service line items.</td></tr>");
        sb.Append("</table>");
        if (order.Expenses.Any()) { sb.Append("<h3>Expenses</h3><table class='data wide'><tr><th>#</th><th style='text-align:left'>Description</th><th style='text-align:right'>Amount</th></tr>"); int j = 1; foreach (var ex in order.Expenses) { sb.Append($"<tr><td>{j}</td><td>{E(ex.Description)}</td><td class='num'>{Money(ex.Amount)}</td></tr>"); j++; } sb.Append("</table>"); }
        sb.Append("<div class='totals'><table><tr><td>Subtotal</td><td class='num'>" + Money(order.Subtotal) + "</td></tr><tr><td>Tax</td><td class='num'>" + Money(order.TaxAmount) + "</td></tr>");
        if (order.ExpenseAmount.HasValue) sb.Append("<tr><td>Expenses</td><td class='num'>" + Money(order.ExpenseAmount) + "</td></tr>");
        sb.Append("<tr class='grand'><td>Total</td><td class='num'>" + Money(order.TotalAmount) + "</td></tr>");
        if (order.ScheduledStartDate.HasValue) sb.Append($"<tr><td>Scheduled Start</td><td class='num'>{order.ScheduledStartDate:yyyy-MM-dd}</td></tr>");
        if (order.ScheduledEndDate.HasValue) sb.Append($"<tr><td>Scheduled End</td><td class='num'>{order.ScheduledEndDate:yyyy-MM-dd}</td></tr>");
        if (order.RequiresOnsite && !string.IsNullOrWhiteSpace(order.OnsiteAddress)) sb.Append($"<tr><td>On-site Address</td><td class='num'>{E(order.OnsiteAddress)}</td></tr>");
        sb.Append("</table></div>");
        if (!string.IsNullOrWhiteSpace(order.Notes)) sb.Append("<h3>Notes</h3><p class='pre'>" + E(order.Notes) + "</p>");
        if (admin) sb.Append("<p class='meta'>Internal notification</p>");
        return WrapEmail(sb.ToString(), accent1, accent2);
    }

    private string BuildServiceOrderStatusUpdate(ServiceOrder order, string customerEmail, ServiceOrderStatus oldStatus, string accent1, string accent2)
    {
        string E(string? v) => WebUtility.HtmlEncode(v ?? string.Empty);
        var sb = new StringBuilder();
        sb.Append("<h2 class='h'>Service Order Status Update</h2>");
        sb.Append($"<p>Your service order <strong>{E(order.OrderNumber)}</strong> status changed from <strong>{oldStatus}</strong> to <strong>{order.Status}</strong>.</p>");
        if (order.ScheduledStartDate.HasValue) sb.Append($"<p><strong>Scheduled Start:</strong> {order.ScheduledStartDate:yyyy-MM-dd}</p>");
        if (order.ScheduledEndDate.HasValue) sb.Append($"<p><strong>Scheduled End:</strong> {order.ScheduledEndDate:yyyy-MM-dd}</p>");
        if (!string.IsNullOrWhiteSpace(order.Notes)) sb.Append("<h3>Notes</h3><p class='pre'>" + E(order.Notes) + "</p>");
        return WrapEmail(sb.ToString(), accent1, accent2);
    }

    // Central wrapper with dynamic accent colors
    private string WrapEmail(string inner, string accent1 = "#0d6efd", string accent2 = "#6610f2")
    {
        string style = $"<!DOCTYPE html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'/>" +
               "<title>Notification</title><style>:root{--accent1:" + accent1 + ";--accent2:" + accent2 + ";}body{margin:0;background:#f5f7fa;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Arial,sans-serif;color:#212529;-webkit-font-smoothing:antialiased;}a{color:var(--accent1);text-decoration:none}h1,h2,h3,h4{font-weight:600;margin:0 0 12px}p{line-height:1.5;margin:0 0 12px}.wrapper{max-width:680px;margin:0 auto;padding:32px 24px}.card{background:#ffffff;border:1px solid #e4e6eb;border-radius:12px;padding:40px 44px;box-shadow:0 2px 4px rgba(0,0,0,.04),0 4px 16px -2px rgba(0,0,0,.06);}table{border-collapse:collapse;width:100%;}table.data{margin:12px 0 20px;font-size:13px;}table.data th,table.data td{padding:8px 10px;border:1px solid #e5e7eb;vertical-align:top;background:#fff;}table.data th{background:#f1f3f5;font-weight:600;font-size:12px;text-transform:uppercase;letter-spacing:.5px;color:#495057;}table.data.compact th,table.data.compact td{padding:6px 8px}table.data.wide th:first-child{width:40px}.totals{margin:20px 0 8px;display:flex;justify-content:flex-end} .totals table{width:auto;font-size:13px;} .totals td{padding:4px 10px;} .totals tr.grand td{font-size:14px;font-weight:600;border-top:2px solid var(--accent1);} .badge{display:inline-block;padding:4px 10px;border-radius:20px;font-size:12px;background:linear-gradient(90deg,var(--accent1),var(--accent2));color:#fff;margin:4px 0 12px} .badge.danger{background:linear-gradient(90deg,#dc3545,#b02a37)}.lead{color:#495057;font-size:14px;margin-top:0}.pre{white-space:pre-wrap;word-break:break-word}.meta{font-size:12px;color:#6c757d}.empty{text-align:center;color:#6c757d;font-style:italic}.cols{display:flex;gap:24px;margin-top:24px} .cols>div{flex:1;min-width:0;background:#fff;border:1px solid #e5e7eb;padding:12px 16px;border-radius:8px} .cols h4{margin:0 0 8px;font-size:13px;text-transform:uppercase;letter-spacing:.5px;color:#495057} @media (max-width:640px){.card{padding:28px 24px}.cols{flex-direction:column;gap:12px}table.data th,table.data td{font-size:12px;padding:6px 6px}.totals td{padding:4px 6px}} .h{background:linear-gradient(90deg,var(--accent1),var(--accent2));-webkit-background-clip:text;color:transparent}</style></head><body>";
        return style + "<div class='wrapper'><div class='card'>" + inner + "<hr style='margin:32px 0 16px;border:none;border-top:1px solid #e9ecef'><p class='meta'>Automated message • ProductReviews</p></div><p style='text-align:center;margin:16px 0 32px;font-size:11px;color:#94a3b8'>Please do not reply directly to this automated email.</p></div></body></html>";
    }
}