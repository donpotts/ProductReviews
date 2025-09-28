using Microsoft.Graph;
using Microsoft.Graph.Models;
using Azure.Identity;
using AppProduct.Shared.Models;
using System.Net;

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
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = body
                },
                ToRecipients = new List<Recipient>()
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = adminEmail
                        }
                    }
                },
                ReplyTo = new List<Recipient>()
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = review.CustomerEmail,
                            Name = review.CustomerName
                        }
                    }
                }
            };

            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = message
            });

            _logger.LogInformation("New review notification sent successfully for review ID {ReviewId}", review.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send new review notification for review ID {ReviewId}", review.Id);
        }
    }

    public async Task SendReviewResponseNotificationAsync(ProductReview review, Product product)
    {
        if (_graphServiceClient == null)
        {
            _logger.LogWarning("Microsoft Graph client is not initialized. Cannot send email notification.");
            return;
        }

        try
        {
            if (string.IsNullOrEmpty(review.CustomerEmail))
            {
                _logger.LogWarning("Customer email is not available for review ID {ReviewId}. Cannot send response notification.", review.Id);
                return;
            }

            var productDisplayName = product.GetDisplayName();
            if (string.IsNullOrWhiteSpace(productDisplayName))
            {
                productDisplayName = product.Name ?? "your product";
            }

            var subject = $"Response to Your Review of {productDisplayName}";
            var body = BuildResponseEmailBody(review, product, productDisplayName);

            var message = new Message
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = body
                },
                ToRecipients = new List<Recipient>()
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = review.CustomerEmail
                        }
                    }
                }
            };

            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = message
            });

            _logger.LogInformation("Review response notification sent successfully to {CustomerEmail} for review ID {ReviewId}", 
                review.CustomerEmail, review.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send review response notification for review ID {ReviewId}", review.Id);
        }
    }

    private string BuildNewReviewEmailBody(ProductReview review, Product product, string productDisplayName)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h2 style='color: #2c5aa0;'>New Review Received</h2>
                
                <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #495057;'>Product: {productDisplayName}</h3>
                    <p><strong>Customer:</strong> {review.CustomerName}</p>
                    <p><strong>Customer Email:</strong> <a href='mailto:{review.CustomerEmail}' style='color: #2c5aa0; text-decoration: none;'>{review.CustomerEmail}</a></p>
                    <p><strong>Rating:</strong> {review.Rating}/5 ⭐</p>
                    {(!string.IsNullOrEmpty(review.Title) ? $"<p><strong>Title:</strong> {review.Title}</p>" : "")}
                    <p><strong>Review:</strong></p>
                    <blockquote style='margin: 10px 0; padding: 10px; background-color: white; border-left: 4px solid #2c5aa0;'>
                        {review.ReviewText}
                    </blockquote>
                    <p><strong>Review Date:</strong> {review.ReviewDate:MMM dd, yyyy}</p>
                    {(review.IsVerifiedPurchase ? "<p><span style='color: #28a745;'>✓ Verified Purchase</span></p>" : "")}
                </div>

                <div style='background-color: #e7f3ff; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h4 style='margin-top: 0; color: #2c5aa0;'>Reply to Customer</h4>
                    <p style='margin-bottom: 10px;'>To respond to this review, simply reply to this email or click the button below:</p>
                    <p style='margin-bottom: 15px;'>
                        <strong>Customer Email:</strong> <a href='mailto:{review.CustomerEmail}?subject=Re: Your review of {productDisplayName}' style='color: #2c5aa0; text-decoration: none; font-weight: bold;'>{review.CustomerEmail}</a>
                    </p>
                    <p style='margin-bottom: 0;'>
                        <a href='mailto:{review.CustomerEmail}?subject=Re: Your review of {productDisplayName}&body=Dear {review.CustomerName},%0A%0AThank you for your review of {productDisplayName}. ' 
                           style='background-color: #2c5aa0; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                            Reply to Customer
                        </a>
                    </p>
                </div>

                <hr style='margin: 30px 0; border: none; border-top: 1px solid #dee2e6;'>
                <p style='font-size: 12px; color: #6c757d;'>
                    This is an automated notification from your ProductReviews system.
                </p>
            </div>
        </body>
        </html>";
    }

    private string BuildResponseEmailBody(ProductReview review, Product product, string productDisplayName)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h2 style='color: #2c5aa0;'>Thank you for your review!</h2>
                
                <p>Dear {review.CustomerName},</p>
                
                <p>Thank you for taking the time to review <strong>{productDisplayName}</strong>. We have responded to your feedback:</p>

                <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h4 style='margin-top: 0; color: #495057;'>Your Review ({review.Rating}/5 ⭐):</h4>
                    <blockquote style='margin: 10px 0; padding: 10px; background-color: white; border-left: 4px solid #6c757d;'>
                        {review.ReviewText}
                    </blockquote>
                </div>

                <div style='background-color: #e7f3ff; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h4 style='margin-top: 0; color: #2c5aa0;'>Our Response:</h4>
                    <p style='margin-bottom: 0;'>{review.Response}</p>
                    <p style='font-size: 12px; color: #6c757d; margin-top: 10px;'>
                        Response Date: {review.ResponseDate:MMM dd, yyyy}
                    </p>
                </div>

                <p>We appreciate your feedback and hope you continue to enjoy our products. If you have any additional questions or concerns, please don't hesitate to contact us.</p>

                <p>Best regards,<br>The ProductReviews Team</p>

                <hr style='margin: 30px 0; border: none; border-top: 1px solid #dee2e6;'>
                <p style='font-size: 12px; color: #6c757d;'>
                    This is an automated notification. Please do not reply to this email.
                </p>
            </div>
        </body>
        </html>";
    }

    public async Task SendOrderConfirmationAsync(Order order, string customerEmail)
    {
        if (_graphServiceClient == null)
        {
            _logger.LogInformation("Graph client not available");
            return;
        }

        var adminEmail = _configuration["EmailSettings:AdminEmail"];
        if (string.IsNullOrEmpty(adminEmail))
        {
            _logger.LogWarning("Admin email not configured");
            return;
        }

        try
        {
            var subject = $"Order Confirmation - {order.OrderNumber}";
            var customerBody = GenerateOrderConfirmationEmail(order, customerEmail);
            var adminBody = GenerateOrderNotificationEmail(order, customerEmail);

            // Send to customer
            var customerMessage = new Message
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = customerBody
                },
                ToRecipients = new List<Recipient>
                {
                    new() { EmailAddress = new EmailAddress { Address = customerEmail } }
                }
            };

            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = customerMessage
            });

            // Send to admin
            var adminMessage = new Message
            {
                Subject = $"New Order Received - {order.OrderNumber}",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = adminBody
                },
                ToRecipients = new List<Recipient>
                {
                    new() { EmailAddress = new EmailAddress { Address = adminEmail } }
                }
            };

            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = adminMessage
            });

            _logger.LogInformation("Order confirmation emails sent for order {OrderId}", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order confirmation emails for order {OrderId}", order.Id);
            throw;
        }
    }

    private string GenerateOrderConfirmationEmail(Order order, string customerEmail)
    {
        var itemsHtml = BuildOrderItemsHtml(order);
        var subtotal = order.Subtotal ?? 0m;
        var taxAmount = order.TaxAmount ?? 0m;
        var shippingAmount = order.ShippingAmount ?? 0m;
        var totalAmount = order.TotalAmount ?? subtotal + taxAmount + shippingAmount;
        var orderDate = FormatDate(order.OrderDate);
        var estimatedDelivery = order.EstimatedDeliveryDate.HasValue
            ? $"<p><strong>Estimated Delivery:</strong> {order.EstimatedDeliveryDate.Value:MMM dd, yyyy}</p>"
            : string.Empty;
        var shippingInfo = ConvertToHtmlLines(order.ShippingAddress);
        var billingInfo = ConvertToHtmlLines(order.BillingAddress);
        var notesInfo = ConvertToHtmlLines(order.Notes, "No additional notes provided.");

        return $@"<!DOCTYPE html>
        <html>
        <head>
            <title>Order Confirmation</title>
        </head>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px;'>
                <h2 style='color: #2c5aa0; margin-top: 0;'>Order Confirmation</h2>
                
                <p>Dear Customer,</p>
                
                <p>Thank you for your order! We have received your order and will process it shortly.</p>
                
                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Order Details</h3>
                    <p><strong>Order Number:</strong> {HtmlEncode(order.OrderNumber)}</p>
                    <p><strong>Order Date:</strong> {orderDate}</p>
                    <p><strong>Payment Method:</strong> {order.PaymentMethod}</p>
                    <p><strong>Status:</strong> {order.Status}</p>
                    {estimatedDelivery}
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Items Ordered</h3>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <thead>
                            <tr style='background-color: #f8f9fa;'>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: left;'>Product</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: center;'>Qty</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: right;'>Price</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: right;'>Total</th>
                            </tr>
                        </thead>
                        <tbody>
                            {itemsHtml}
                        </tbody>
                        <tfoot>
                            <tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Subtotal:</td>
                                <td style='padding: 8px; text-align: right;'>${subtotal:F2}</td>
                            </tr>
                            {(taxAmount > 0 ? $@"<tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Tax:</td>
                                <td style='padding: 8px; text-align: right;'>${taxAmount:F2}</td>
                            </tr>" : string.Empty)}
                            {(shippingAmount > 0 ? $@"<tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Shipping:</td>
                                <td style='padding: 8px; text-align: right;'>${shippingAmount:F2}</td>
                            </tr>" : string.Empty)}
                            <tr style='font-weight: bold; background-color: #f8f9fa; border-top: 2px solid #dee2e6;'>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Total:</td>
                                <td style='padding: 8px; text-align: right;'>${totalAmount:F2}</td>
                            </tr>
                        </tfoot>
                    </table>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Shipping Information</h3>
                    <p>{shippingInfo}</p>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Billing Information</h3>
                    <p>{billingInfo}</p>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Order Notes</h3>
                    <p>{notesInfo}</p>
                </div>

                <p>We will send you another email with tracking information once your order ships.</p>

                <p>Thank you for your business!</p>

                <p>Best regards,<br>The ProductReviews Team</p>

                <hr style='margin: 30px 0; border: none; border-top: 1px solid #dee2e6;'>
                <p style='font-size: 12px; color: #6c757d;'>
                    This is an automated confirmation. Please do not reply to this email.
                </p>
            </div>
        </body>
        </html>";
    }

    private string GenerateOrderNotificationEmail(Order order, string customerEmail)
    {
        var itemsHtml = BuildOrderItemsHtml(order);
        var subtotal = order.Subtotal ?? 0m;
        var taxAmount = order.TaxAmount ?? 0m;
        var shippingAmount = order.ShippingAmount ?? 0m;
        var totalAmount = order.TotalAmount ?? subtotal + taxAmount + shippingAmount;
        var orderDate = FormatDate(order.OrderDate);
        var shippingInfo = ConvertToHtmlLines(order.ShippingAddress);
        var billingInfo = ConvertToHtmlLines(order.BillingAddress);
        var notesInfo = ConvertToHtmlLines(order.Notes, "No additional notes provided.");
        var paymentIntent = string.IsNullOrWhiteSpace(order.PaymentIntentId)
            ? "<em>Not provided</em>"
            : HtmlEncode(order.PaymentIntentId);
        var userId = string.IsNullOrWhiteSpace(order.UserId)
            ? "<em>Unknown</em>"
            : HtmlEncode(order.UserId);
        var trackingInfo = string.IsNullOrWhiteSpace(order.TrackingNumber)
            ? "<em>Not assigned</em>"
            : HtmlEncode(order.TrackingNumber);
        var estimatedDelivery = order.EstimatedDeliveryDate.HasValue
            ? FormatDate(order.EstimatedDeliveryDate, "Not set")
            : "<em>Not set</em>";

        return $@"<!DOCTYPE html>
        <html>
        <head>
            <title>New Order Notification</title>
        </head>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px;'>
                <h2 style='color: #2c5aa0; margin-top: 0;'>New Order Received</h2>
                
                <p>A new order has been placed.</p>
                
                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Order Summary</h3>
                    <p><strong>Order Number:</strong> {HtmlEncode(order.OrderNumber)}</p>
                    <p><strong>Order Date:</strong> {orderDate}</p>
                    <p><strong>Status:</strong> {order.Status}</p>
                    <p><strong>Payment Method:</strong> {order.PaymentMethod}</p>
                    <p><strong>Customer Email:</strong> {HtmlEncode(customerEmail)}</p>
                    <p><strong>User Id:</strong> {userId}</p>
                    <p><strong>Payment Intent:</strong> {paymentIntent}</p>
                    <p><strong>Estimated Delivery:</strong> {estimatedDelivery}</p>
                    <p><strong>Tracking Number:</strong> {trackingInfo}</p>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Items Ordered</h3>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <thead>
                            <tr style='background-color: #f8f9fa;'>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: left;'>Product</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: center;'>Qty</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: right;'>Price</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: right;'>Total</th>
                            </tr>
                        </thead>
                        <tbody>
                            {itemsHtml}
                        </tbody>
                        <tfoot>
                            <tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Subtotal:</td>
                                <td style='padding: 8px; text-align: right;'>${subtotal:F2}</td>
                            </tr>
                            {(taxAmount > 0 ? $@"<tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Tax:</td>
                                <td style='padding: 8px; text-align: right;'>${taxAmount:F2}</td>
                            </tr>" : string.Empty)}
                            {(shippingAmount > 0 ? $@"<tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Shipping:</td>
                                <td style='padding: 8px; text-align: right;'>${shippingAmount:F2}</td>
                            </tr>" : string.Empty)}
                            <tr style='font-weight: bold; background-color: #f8f9fa; border-top: 2px solid #dee2e6;'>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Total:</td>
                                <td style='padding: 8px; text-align: right;'>${totalAmount:F2}</td>
                            </tr>
                        </tfoot>
                    </table>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Shipping Information</h3>
                    <p>{shippingInfo}</p>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Billing Information</h3>
                    <p>{billingInfo}</p>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Order Notes</h3>
                    <p>{notesInfo}</p>
                </div>

                <p>Please process this order promptly.</p>
            </div>
        </body>
        </html>";
    }

    public async Task SendOrderCancellationAsync(Order order, string customerEmail)
    {
        if (_graphServiceClient == null)
        {
            _logger.LogInformation("Graph client not available");
            return;
        }

        var adminEmail = _configuration["EmailSettings:AdminEmail"];
        if (string.IsNullOrEmpty(adminEmail))
        {
            _logger.LogWarning("Admin email not configured");
            return;
        }

        try
        {
            var subject = $"Order Cancelled - {order.OrderNumber}";
            var customerBody = GenerateOrderCancellationEmail(order, customerEmail);
            var adminBody = GenerateOrderCancellationNotificationEmail(order, customerEmail);

            // Send to customer
            var customerMessage = new Message
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = customerBody
                },
                ToRecipients = new List<Recipient>
                {
                    new() { EmailAddress = new EmailAddress { Address = customerEmail } }
                }
            };

            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = customerMessage
            });

            // Send to admin
            var adminMessage = new Message
            {
                Subject = $"Order Cancelled - {order.OrderNumber}",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = adminBody
                },
                ToRecipients = new List<Recipient>
                {
                    new() { EmailAddress = new EmailAddress { Address = adminEmail } }
                }
            };

            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = adminMessage
            });

            _logger.LogInformation("Order cancellation emails sent for order {OrderId}", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order cancellation emails for order {OrderId}", order.Id);
            throw;
        }
    }

    private string GenerateOrderCancellationEmail(Order order, string customerEmail)
    {
        var itemsHtml = BuildOrderItemsHtml(order);
        var subtotal = order.Subtotal ?? 0m;
        var taxAmount = order.TaxAmount ?? 0m;
        var shippingAmount = order.ShippingAmount ?? 0m;
        var totalAmount = order.TotalAmount ?? subtotal + taxAmount + shippingAmount;
        var orderDate = FormatDate(order.OrderDate, "Unknown");
        var shippingInfo = ConvertToHtmlLines(order.ShippingAddress);
        var billingInfo = ConvertToHtmlLines(order.BillingAddress);
        var notesInfo = ConvertToHtmlLines(order.Notes, "No additional notes provided.");

        return $@"<!DOCTYPE html>
        <html>
        <head>
            <title>Order Cancelled</title>
        </head>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='background-color: #fff3cd; padding: 20px; border-radius: 8px; border-left: 4px solid #ffc107;'>
                <h2 style='color: #856404; margin-top: 0;'>Order Cancelled</h2>
                
                <p>Dear Customer,</p>
                
                <p>We're writing to inform you that your order has been cancelled. If this was unexpected, please contact our customer service team.</p>
                
                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #856404;'>Cancelled Order Details</h3>
                    <p><strong>Order Number:</strong> {HtmlEncode(order.OrderNumber)}</p>
                    <p><strong>Order Date:</strong> {orderDate}</p>
                    <p><strong>Payment Method:</strong> {order.PaymentMethod}</p>
                    <p><strong>Status:</strong> {order.Status}</p>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #856404;'>Items in Cancelled Order</h3>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <thead>
                            <tr style='background-color: #f8f9fa;'>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: left;'>Product</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: center;'>Qty</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: right;'>Price</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: right;'>Total</th>
                            </tr>
                        </thead>
                        <tbody>
                            {itemsHtml}
                        </tbody>
                        <tfoot>
                            <tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Subtotal:</td>
                                <td style='padding: 8px; text-align: right;'>${subtotal:F2}</td>
                            </tr>
                            {(taxAmount > 0 ? $@"<tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Tax:</td>
                                <td style='padding: 8px; text-align: right;'>${taxAmount:F2}</td>
                            </tr>" : string.Empty)}
                            {(shippingAmount > 0 ? $@"<tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Shipping:</td>
                                <td style='padding: 8px; text-align: right;'>${shippingAmount:F2}</td>
                            </tr>" : string.Empty)}
                            <tr style='font-weight: bold; background-color: #f8f9fa;'>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Total:</td>
                                <td style='padding: 8px; text-align: right;'>${totalAmount:F2}</td>
                            </tr>
                        </tfoot>
                    </table>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #856404;'>Shipping Information</h3>
                    <p>{shippingInfo}</p>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #856404;'>Billing Information</h3>
                    <p>{billingInfo}</p>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #856404;'>Order Notes</h3>
                    <p>{notesInfo}</p>
                </div>

                <div style='background-color: #d1ecf1; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #17a2b8;'>
                    <h3 style='margin-top: 0; color: #0c5460;'>Refund Information</h3>
                    <p>If you were charged for this order, a refund will be processed within 3-5 business days to your original payment method.</p>
                    <p>If you have any questions about this cancellation or your refund, please contact our customer service team.</p>
                </div>

                <p>We apologize for any inconvenience this cancellation may have caused.</p>

                <p>Best regards,<br>The ProductReviews Team</p>

                <hr style='margin: 30px 0; border: none; border-top: 1px solid #dee2e6;'>
                <p style='font-size: 12px; color: #6c757d;'>
                    This is an automated notification. If you have questions, please contact customer service.
                </p>
            </div>
        </body>
        </html>";
    }

    private string GenerateOrderCancellationNotificationEmail(Order order, string customerEmail)
    {
        var itemsHtml = BuildOrderItemsHtml(order);
        var subtotal = order.Subtotal ?? 0m;
        var taxAmount = order.TaxAmount ?? 0m;
        var shippingAmount = order.ShippingAmount ?? 0m;
        var totalAmount = order.TotalAmount ?? subtotal + taxAmount + shippingAmount;
        var orderDate = FormatDate(order.OrderDate, "Unknown");
        var shippingInfo = ConvertToHtmlLines(order.ShippingAddress);
        var billingInfo = ConvertToHtmlLines(order.BillingAddress);
        var notesInfo = ConvertToHtmlLines(order.Notes, "No additional notes provided.");
        var paymentIntent = string.IsNullOrWhiteSpace(order.PaymentIntentId)
            ? "<em>Not provided</em>"
            : HtmlEncode(order.PaymentIntentId);
        var userId = string.IsNullOrWhiteSpace(order.UserId)
            ? "<em>Unknown</em>"
            : HtmlEncode(order.UserId);

        return $@"<!DOCTYPE html>
        <html>
        <head>
            <title>Order Cancellation Notification</title>
        </head>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='background-color: #fff3cd; padding: 20px; border-radius: 8px; border-left: 4px solid #ffc107;'>
                <h2 style='color: #856404; margin-top: 0;'>Order Cancelled</h2>
                
                <p>An order has been cancelled.</p>
                
                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #856404;'>Cancelled Order Details</h3>
                    <p><strong>Order Number:</strong> {HtmlEncode(order.OrderNumber)}</p>
                    <p><strong>Customer Email:</strong> {HtmlEncode(customerEmail)}</p>
                    <p><strong>User Id:</strong> {userId}</p>
                    <p><strong>Order Date:</strong> {orderDate}</p>
                    <p><strong>Payment Method:</strong> {order.PaymentMethod}</p>
                    <p><strong>Status:</strong> {order.Status}</p>
                    <p><strong>Payment Intent:</strong> {paymentIntent}</p>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #856404;'>Items in Cancelled Order</h3>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <thead>
                            <tr style='background-color: #f8f9fa;'>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: left;'>Product</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: center;'>Qty</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: right;'>Price</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: right;'>Total</th>
                            </tr>
                        </thead>
                        <tbody>
                            {itemsHtml}
                        </tbody>
                        <tfoot>
                            <tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Subtotal:</td>
                                <td style='padding: 8px; text-align: right;'>${subtotal:F2}</td>
                            </tr>
                            {(taxAmount > 0 ? $@"<tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Tax:</td>
                                <td style='padding: 8px; text-align: right;'>${taxAmount:F2}</td>
                            </tr>" : string.Empty)}
                            {(shippingAmount > 0 ? $@"<tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Shipping:</td>
                                <td style='padding: 8px; text-align: right;'>${shippingAmount:F2}</td>
                            </tr>" : string.Empty)}
                            <tr style='font-weight: bold; background-color: #f8f9fa;'>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Total:</td>
                                <td style='padding: 8px; text-align: right;'>${totalAmount:F2}</td>
                            </tr>
                        </tfoot>
                    </table>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #856404;'>Shipping Information</h3>
                    <p>{shippingInfo}</p>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #856404;'>Billing Information</h3>
                    <p>{billingInfo}</p>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #856404;'>Order Notes</h3>
                    <p>{notesInfo}</p>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #856404;'>Action Required</h3>
                    <p><strong>Process Refund:</strong> If payment was processed, initiate refund for ${order.TotalAmount:F2}</p>
                    <p><strong>Update Inventory:</strong> Return cancelled items to inventory if applicable</p>
                    <p><strong>Customer Contact:</strong> Consider reaching out to customer to understand cancellation reason</p>
                </div>

                <p>Please take appropriate action for this cancelled order.</p>
            </div>
        </body>
        </html>";
    }

    public async Task SendCheckoutCancellationEmailAsync(List<CartProduct> cart, string customerEmail)
    {
        if (_graphServiceClient == null)
        {
            _logger.LogInformation("Graph client not available");
            return;
        }

        var adminEmail = _configuration["EmailSettings:AdminEmail"];
        if (string.IsNullOrEmpty(adminEmail))
        {
            _logger.LogWarning("Admin email not configured");
            return;
        }

        try
        {
            var subject = "Checkout Cancelled";
            var customerBody = GenerateCheckoutCancellationEmail(cart, customerEmail);
            var adminBody = GenerateCheckoutCancellationNotificationEmail(cart, customerEmail);

            // Send to customer
            var customerMessage = new Message
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = customerBody
                },
                ToRecipients = new List<Recipient>
                {
                    new() { EmailAddress = new EmailAddress { Address = customerEmail } }
                }
            };

            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = customerMessage
            });

            // Send to admin
            var adminMessage = new Message
            {
                Subject = "Customer Cancelled Checkout",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = adminBody
                },
                ToRecipients = new List<Recipient>
                {
                    new() { EmailAddress = new EmailAddress { Address = adminEmail } }
                }
            };

            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = adminMessage
            });

            _logger.LogInformation("Checkout cancellation emails sent for customer {CustomerEmail}", customerEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send checkout cancellation emails for customer {CustomerEmail}", customerEmail);
            throw;
        }
    }

    private string GenerateCheckoutCancellationEmail(List<CartProduct> cart, string customerEmail)
    {
        // Separate products from tax/shipping items
        var products = cart?.Where(item => 
            !item.Name?.Contains("Tax", StringComparison.OrdinalIgnoreCase) == true && 
            !item.Name?.Contains("Shipping", StringComparison.OrdinalIgnoreCase) == true).ToList() ?? new List<CartProduct>();
        
        var taxItems = cart?.Where(item => 
            item.Name?.Contains("Tax", StringComparison.OrdinalIgnoreCase) == true).ToList() ?? new List<CartProduct>();
        
        var shippingItems = cart?.Where(item => 
            item.Name?.Contains("Shipping", StringComparison.OrdinalIgnoreCase) == true).ToList() ?? new List<CartProduct>();

        var itemsHtml = products.Any() 
            ? products.Select(item => 
                $@"<tr>
                    <td style='padding: 8px; border-bottom: 1px solid #dee2e6;'>{item.DisplayName ?? item.Name ?? "Unknown Product"}</td>
                    <td style='padding: 8px; border-bottom: 1px solid #dee2e6; text-align: center;'>{item.Quantity}</td>
                    <td style='padding: 8px; border-bottom: 1px solid #dee2e6; text-align: right;'>${item.Price:F2}</td>
                    <td style='padding: 8px; border-bottom: 1px solid #dee2e6; text-align: right;'>${(item.Price * item.Quantity):F2}</td>
                </tr>").Aggregate((a, b) => a + b)
            : "<tr><td colspan='4' style='padding: 8px; text-align: center;'>No items in cart</td></tr>";

        var subtotal = products.Sum(item => (item.Price ?? 0) * item.Quantity);
        var taxAmount = taxItems.Sum(item => (item.Price ?? 0) * item.Quantity);
        var shippingAmount = shippingItems.Sum(item => (item.Price ?? 0) * item.Quantity);
        var total = cart?.Sum(item => (item.Price ?? 0) * item.Quantity) ?? 0;

        return $@"<!DOCTYPE html>
        <html>
        <head>
            <title>Checkout Cancelled</title>
        </head>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='background-color: #fff3cd; padding: 20px; border-radius: 8px; border-left: 4px solid #ffc107;'>
                <h2 style='color: #856404; margin-top: 0;'>Checkout Cancelled</h2>
                
                <p>Dear Customer,</p>
                
                <p>We noticed that you cancelled your checkout process. Don't worry - your items are still saved in your cart for when you're ready to complete your purchase.</p>
                
                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #856404;'>Items in Your Cart</h3>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <thead>
                            <tr style='background-color: #f8f9fa;'>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: left;'>Product</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: center;'>Qty</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: right;'>Price</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: right;'>Total</th>
                            </tr>
                        </thead>
                        <tbody>
                            {itemsHtml}
                        </tbody>
                        <tfoot>
                            <tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Subtotal:</td>
                                <td style='padding: 8px; text-align: right;'>${subtotal:F2}</td>
                            </tr>
                            {(taxAmount > 0 ? $@"<tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Tax:</td>
                                <td style='padding: 8px; text-align: right;'>${taxAmount:F2}</td>
                            </tr>" : "")}
                            {(shippingAmount > 0 ? $@"<tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Shipping:</td>
                                <td style='padding: 8px; text-align: right;'>${shippingAmount:F2}</td>
                            </tr>" : "")}
                            <tr style='font-weight: bold; background-color: #f8f9fa; border-top: 2px solid #dee2e6;'>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Total:</td>
                                <td style='padding: 8px; text-align: right;'>${total:F2}</td>
                            </tr>
                        </tfoot>
                    </table>
                </div>

                <div style='background-color: #d1ecf1; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #17a2b8;'>
                    <h3 style='margin-top: 0; color: #0c5460;'>Complete Your Purchase</h3>
                    <p>Your items are waiting for you! Return to your cart anytime to complete your purchase.</p>
                    <p>If you experienced any issues during checkout, please don't hesitate to contact our customer service team.</p>
                </div>

                <p>Thank you for considering our products!</p>

                <p>Best regards,<br>The ProductReviews Team</p>

                <hr style='margin: 30px 0; border: none; border-top: 1px solid #dee2e6;'>
                <p style='font-size: 12px; color: #6c757d;'>
                    This is an automated notification. If you have questions, please contact customer service.
                </p>
            </div>
        </body>
        </html>";
    }

    private string GenerateCheckoutCancellationNotificationEmail(List<CartProduct> cart, string customerEmail)
    {
        // Separate products from tax/shipping items
        var products = cart?.Where(item => 
            !item.Name?.Contains("Tax", StringComparison.OrdinalIgnoreCase) == true && 
            !item.Name?.Contains("Shipping", StringComparison.OrdinalIgnoreCase) == true).ToList() ?? new List<CartProduct>();
        
        var taxItems = cart?.Where(item => 
            item.Name?.Contains("Tax", StringComparison.OrdinalIgnoreCase) == true).ToList() ?? new List<CartProduct>();
        
        var shippingItems = cart?.Where(item => 
            item.Name?.Contains("Shipping", StringComparison.OrdinalIgnoreCase) == true).ToList() ?? new List<CartProduct>();

        var itemsHtml = products.Any() 
            ? products.Select(item => 
                $@"<tr>
                    <td style='padding: 8px; border-bottom: 1px solid #dee2e6;'>{item.DisplayName ?? item.Name ?? "Unknown Product"}</td>
                    <td style='padding: 8px; border-bottom: 1px solid #dee2e6; text-align: center;'>{item.Quantity}</td>
                    <td style='padding: 8px; border-bottom: 1px solid #dee2e6; text-align: right;'>${item.Price:F2}</td>
                    <td style='padding: 8px; border-bottom: 1px solid #dee2e6; text-align: right;'>${(item.Price * item.Quantity):F2}</td>
                </tr>").Aggregate((a, b) => a + b)
            : "<tr><td colspan='4' style='padding: 8px; text-align: center;'>No items in cart</td></tr>";

        var subtotal = products.Sum(item => (item.Price ?? 0) * item.Quantity);
        var taxAmount = taxItems.Sum(item => (item.Price ?? 0) * item.Quantity);
        var shippingAmount = shippingItems.Sum(item => (item.Price ?? 0) * item.Quantity);
        var total = cart?.Sum(item => (item.Price ?? 0) * item.Quantity) ?? 0;

        return $@"<!DOCTYPE html>
        <html>
        <head>
            <title>Customer Cancelled Checkout</title>
        </head>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='background-color: #fff3cd; padding: 20px; border-radius: 8px; border-left: 4px solid #ffc107;'>
                <h2 style='color: #856404; margin-top: 0;'>Customer Cancelled Checkout</h2>
                
                <p>A customer cancelled their checkout process.</p>
                
                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #856404;'>Customer Details</h3>
                    <p><strong>Customer Email:</strong> {customerEmail}</p>
                    <p><strong>Cancellation Time:</strong> {DateTime.UtcNow:MMM dd, yyyy HH:mm} UTC</p>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #856404;'>Abandoned Cart Items</h3>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <thead>
                            <tr style='background-color: #f8f9fa;'>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: left;'>Product</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: center;'>Qty</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: right;'>Price</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: right;'>Total</th>
                            </tr>
                        </thead>
                        <tbody>
                            {itemsHtml}
                        </tbody>
                        <tfoot>
                            <tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Subtotal:</td>
                                <td style='padding: 8px; text-align: right;'>${subtotal:F2}</td>
                            </tr>
                            {(taxAmount > 0 ? $@"<tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Tax:</td>
                                <td style='padding: 8px; text-align: right;'>${taxAmount:F2}</td>
                            </tr>" : "")}
                            {(shippingAmount > 0 ? $@"<tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Shipping:</td>
                                <td style='padding: 8px; text-align: right;'>${shippingAmount:F2}</td>
                            </tr>" : "")}
                            <tr style='font-weight: bold; background-color: #f8f9fa; border-top: 2px solid #dee2e6;'>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Total:</td>
                                <td style='padding: 8px; text-align: right;'>${total:F2}</td>
                            </tr>
                        </tfoot>
                    </table>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #856404;'>Follow-up Actions</h3>
                    <p><strong>Consider:</strong> Sending a follow-up email with a discount code to encourage completion</p>
                    <p><strong>Review:</strong> Check if there were any technical issues during checkout</p>
                    <p><strong>Analytics:</strong> Track abandonment rates to identify potential improvements</p>
                </div>

                <p>Monitor checkout flow for potential improvements to reduce abandonment.</p>
            </div>
        </body>
        </html>";
    }

    private static string BuildOrderItemsHtml(Order order)
    {
        if (order.Items == null || order.Items.Count == 0)
        {
            return "<tr><td colspan='4' style='padding: 8px; text-align: center;'>No items in order</td></tr>";
        }

        return string.Join(string.Empty, order.Items.Select(item =>
        {
            var productNameRaw = item.Product?.GetDisplayName();
            var productName = HtmlEncode(string.IsNullOrWhiteSpace(productNameRaw)
                ? item.Product?.Name ?? "Unknown Product"
                : productNameRaw);
            var quantity = item.Quantity;
            var unitPrice = item.UnitPrice ?? 0m;
            var lineTotal = unitPrice * quantity;

            return $@"<tr>
                <td style='padding: 8px; border-bottom: 1px solid #dee2e6;'>{productName}</td>
                <td style='padding: 8px; border-bottom: 1px solid #dee2e6; text-align: center;'>{quantity}</td>
                <td style='padding: 8px; border-bottom: 1px solid #dee2e6; text-align: right;'>${unitPrice:F2}</td>
                <td style='padding: 8px; border-bottom: 1px solid #dee2e6; text-align: right;'>${lineTotal:F2}</td>
            </tr>";
        }));
    }

    private static string FormatDate(DateTime? date, string placeholder = "Not provided")
    {
        return date.HasValue
            ? HtmlEncode(date.Value.ToString("MMM dd, yyyy"))
            : $"<em>{HtmlEncode(placeholder)}</em>";
    }

    private static string ConvertToHtmlLines(string? value, string placeholder = "Not provided")
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return $"<em>{HtmlEncode(placeholder)}</em>";
        }

        var normalized = value.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        var encoded = HtmlEncode(normalized);
        return encoded.Replace("\n", "<br>");
    }

    private static string HtmlEncode(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }

    public async Task SendInvoiceEmailAsync(Order order, string customerEmail, byte[] invoicePdf)
    {
        if (_graphServiceClient == null)
        {
            _logger.LogWarning("Microsoft Graph client is not initialized. Cannot send invoice email.");
            return;
        }

        var adminEmail = _configuration["EmailSettings:AdminEmail"];
        if (string.IsNullOrEmpty(adminEmail))
        {
            _logger.LogWarning("Admin email not configured");
            return;
        }

        try
        {
            var subject = $"Invoice for Purchase Order - {order.OrderNumber}";
            var customerBody = GenerateInvoiceEmail(order, customerEmail);
            var adminBody = GenerateInvoiceNotificationEmail(order, customerEmail);

            // Convert PDF to base64 attachment
            var pdfAttachment = new FileAttachment
            {
                Name = $"Invoice-{order.OrderNumber}.pdf",
                ContentType = "application/pdf",
                ContentBytes = invoicePdf
            };

            // Send to customer with PDF attachment
            var customerMessage = new Message
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = customerBody
                },
                ToRecipients = new List<Recipient>
                {
                    new() { EmailAddress = new EmailAddress { Address = customerEmail } }
                },
                Attachments = new List<Attachment> { pdfAttachment }
            };

            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = customerMessage
            });

            // Send to admin with PDF attachment
            var adminMessage = new Message
            {
                Subject = $"New Purchase Order Invoice Generated - {order.OrderNumber}",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = adminBody
                },
                ToRecipients = new List<Recipient>
                {
                    new() { EmailAddress = new EmailAddress { Address = adminEmail } }
                },
                Attachments = new List<Attachment> { pdfAttachment }
            };

            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = adminMessage
            });

            _logger.LogInformation("Invoice emails sent for order {OrderId}", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invoice emails for order {OrderId}", order.Id);
            throw;
        }
    }

    private string GenerateInvoiceEmail(Order order, string customerEmail)
    {
        var itemsHtml = BuildOrderItemsHtml(order);
        var subtotal = order.Subtotal ?? 0m;
        var taxAmount = order.TaxAmount ?? 0m;
        var shippingAmount = order.ShippingAmount ?? 0m;
        var totalAmount = order.TotalAmount ?? subtotal + taxAmount + shippingAmount;
        var orderDate = FormatDate(order.OrderDate);
        var shippingInfo = ConvertToHtmlLines(order.ShippingAddress);
        var billingInfo = ConvertToHtmlLines(order.BillingAddress);
        var notesInfo = ConvertToHtmlLines(order.Notes, "No additional notes provided.");

        return $@"<!DOCTYPE html>
        <html>
        <head>
            <title>Invoice for Purchase Order</title>
        </head>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px;'>
                <h2 style='color: #2c5aa0; margin-top: 0;'>Invoice for Purchase Order</h2>
                
                <p>Dear Customer,</p>
                
                <p>Thank you for your Purchase Order! Please find attached the invoice for your order. This invoice serves as your billing document for processing payment through your accounts payable department.</p>
                
                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Order Details</h3>
                    <p><strong>Order Number:</strong> {HtmlEncode(order.OrderNumber)}</p>
                    <p><strong>Order Date:</strong> {orderDate}</p>
                    <p><strong>Payment Method:</strong> {order.PaymentMethod}</p>
                    <p><strong>Status:</strong> {order.Status}</p>
                    <p><strong>Invoice Amount:</strong> <span style='font-size: 1.2em; font-weight: bold; color: #2c5aa0;'>${totalAmount:F2}</span></p>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Items Ordered</h3>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <thead>
                            <tr style='background-color: #f8f9fa;'>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: left;'>Product</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: center;'>Qty</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: right;'>Price</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: right;'>Total</th>
                            </tr>
                        </thead>
                        <tbody>
                            {itemsHtml}
                        </tbody>
                        <tfoot>
                            <tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Subtotal:</td>
                                <td style='padding: 8px; text-align: right;'>${subtotal:F2}</td>
                            </tr>
                            {(taxAmount > 0 ? $@"<tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Tax:</td>
                                <td style='padding: 8px; text-align: right;'>${taxAmount:F2}</td>
                            </tr>" : string.Empty)}
                            {(shippingAmount > 0 ? $@"<tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Shipping:</td>
                                <td style='padding: 8px; text-align: right;'>${shippingAmount:F2}</td>
                            </tr>" : string.Empty)}
                            <tr style='font-weight: bold; background-color: #f8f9fa; border-top: 2px solid #dee2e6;'>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Total Amount Due:</td>
                                <td style='padding: 8px; text-align: right;'>${totalAmount:F2}</td>
                            </tr>
                        </tfoot>
                    </table>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Purchase Order Information</h3>
                    <p>{notesInfo}</p>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Shipping Address</h3>
                    <p>{shippingInfo}</p>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Billing Address</h3>
                    <p>{billingInfo}</p>
                </div>

                <div style='background-color: #d1ecf1; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #17a2b8;'>
                    <h3 style='margin-top: 0; color: #0c5460;'>Payment Instructions</h3>
                    <p><strong>Payment Terms:</strong> Net 30 days from invoice date</p>
                    <p><strong>Remit Payment To:</strong> ProductReviews Accounts Receivable</p>
                    <p>Please reference order number <strong>{HtmlEncode(order.OrderNumber)}</strong> with your payment.</p>
                    <p>For any billing questions, please contact our accounting department.</p>
                </div>

                <p>We will begin processing your order upon receipt of payment or purchase order approval.</p>

                <p>Thank you for your business!</p>

                <p>Best regards,<br>The ProductReviews Team</p>

                <hr style='margin: 30px 0; border: none; border-top: 1px solid #dee2e6;'>
                <p style='font-size: 12px; color: #6c757d;'>
                    This invoice was automatically generated. Please retain for your records.
                </p>
            </div>
        </body>
        </html>";
    }

    private string GenerateInvoiceNotificationEmail(Order order, string customerEmail)
    {
        var itemsHtml = BuildOrderItemsHtml(order);
        var subtotal = order.Subtotal ?? 0m;
        var taxAmount = order.TaxAmount ?? 0m;
        var shippingAmount = order.ShippingAmount ?? 0m;
        var totalAmount = order.TotalAmount ?? subtotal + taxAmount + shippingAmount;
        var orderDate = FormatDate(order.OrderDate);
        var shippingInfo = ConvertToHtmlLines(order.ShippingAddress);
        var billingInfo = ConvertToHtmlLines(order.BillingAddress);
        var notesInfo = ConvertToHtmlLines(order.Notes, "No additional notes provided.");

        return $@"<!DOCTYPE html>
        <html>
        <head>
            <title>New Purchase Order Invoice Generated</title>
        </head>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px;'>
                <h2 style='color: #2c5aa0; margin-top: 0;'>New Purchase Order Invoice Generated</h2>
                
                <p>A new invoice has been generated for a Purchase Order.</p>
                
                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Invoice Summary</h3>
                    <p><strong>Order Number:</strong> {HtmlEncode(order.OrderNumber)}</p>
                    <p><strong>Customer Email:</strong> {HtmlEncode(customerEmail)}</p>
                    <p><strong>Order Date:</strong> {orderDate}</p>
                    <p><strong>Payment Method:</strong> {order.PaymentMethod}</p>
                    <p><strong>Status:</strong> {order.Status}</p>
                    <p><strong>Invoice Total:</strong> <span style='font-weight: bold; color: #2c5aa0;'>${totalAmount:F2}</span></p>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Items Invoiced</h3>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <thead>
                            <tr style='background-color: #f8f9fa;'>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: left;'>Product</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: center;'>Qty</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: right;'>Price</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: right;'>Total</th>
                            </tr>
                        </thead>
                        <tbody>
                            {itemsHtml}
                        </tbody>
                        <tfoot>
                            <tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Subtotal:</td>
                                <td style='padding: 8px; text-align: right;'>${subtotal:F2}</td>
                            </tr>
                            {(taxAmount > 0 ? $@"<tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Tax:</td>
                                <td style='padding: 8px; text-align: right;'>${taxAmount:F2}</td>
                            </tr>" : string.Empty)}
                            {(shippingAmount > 0 ? $@"<tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Shipping:</td>
                                <td style='padding: 8px; text-align: right;'>${shippingAmount:F2}</td>
                            </tr>" : string.Empty)}
                            <tr style='font-weight: bold; background-color: #f8f9fa; border-top: 2px solid #dee2e6;'>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Total:</td>
                                <td style='padding: 8px; text-align: right;'>${totalAmount:F2}</td>
                            </tr>
                        </tfoot>
                    </table>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Purchase Order Details</h3>
                    <p>{notesInfo}</p>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Customer Information</h3>
                    <div style='display: flex; gap: 20px;'>
                        <div style='flex: 1;'>
                            <h4 style='margin-top: 0; color: #495057;'>Shipping Address</h4>
                            <p>{shippingInfo}</p>
                        </div>
                        <div style='flex: 1;'>
                            <h4 style='margin-top: 0; color: #495057;'>Billing Address</h4>
                            <p>{billingInfo}</p>
                        </div>
                    </div>
                </div>

                <div style='background-color: #d1ecf1; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #17a2b8;'>
                    <h3 style='margin-top: 0; color: #0c5460;'>Next Steps</h3>
                    <p><strong>1. Customer Follow-up:</strong> Monitor for payment or PO processing</p>
                    <p><strong>2. Order Processing:</strong> Begin fulfillment upon payment receipt/PO approval</p>
                    <p><strong>3. Status Updates:</strong> Keep customer informed of order progress</p>
                    <p><strong>4. Payment Terms:</strong> Net 30 days from invoice date</p>
                </div>

                <p>The invoice PDF has been sent to both the customer and saved for your records.</p>
            </div>
        </body>
        </html>";
    }

    public async Task SendCashOrderEmailAsync(Order order, string customerEmail, byte[] deliveryReceiptPdf)
    {
        if (_graphServiceClient == null)
        {
            _logger.LogWarning("Microsoft Graph client is not initialized. Cannot send cash order email.");
            return;
        }

        var adminEmail = _configuration["EmailSettings:AdminEmail"];
        if (string.IsNullOrEmpty(adminEmail))
        {
            _logger.LogWarning("Admin email not configured");
            return;
        }

        try
        {
            var subject = $"Cash Order Delivery Receipt - {order.OrderNumber}";
            var customerBody = GenerateCashOrderEmail(order, customerEmail);
            var adminBody = GenerateCashOrderNotificationEmail(order, customerEmail);

            // Convert PDF to base64 attachment
            var pdfAttachment = new FileAttachment
            {
                Name = $"Delivery-Receipt-{order.OrderNumber}.pdf",
                ContentType = "application/pdf",
                ContentBytes = deliveryReceiptPdf
            };

            // Send to customer with PDF attachment
            var customerMessage = new Message
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = customerBody
                },
                ToRecipients = new List<Recipient>
                {
                    new() { EmailAddress = new EmailAddress { Address = customerEmail } }
                },
                Attachments = new List<Attachment> { pdfAttachment }
            };

            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = customerMessage
            });

            // Send to admin with PDF attachment
            var adminMessage = new Message
            {
                Subject = $"New Cash Order for Delivery - {order.OrderNumber}",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = adminBody
                },
                ToRecipients = new List<Recipient>
                {
                    new() { EmailAddress = new EmailAddress { Address = adminEmail } }
                },
                Attachments = new List<Attachment> { pdfAttachment }
            };

            await _graphServiceClient.Users[senderEmail].SendMail.PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = adminMessage
            });

            _logger.LogInformation("Cash order emails sent for order {OrderId}", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send cash order emails for order {OrderId}", order.Id);
            throw;
        }
    }

    private string GenerateCashOrderEmail(Order order, string customerEmail)
    {
        var itemsHtml = BuildOrderItemsHtml(order);
        var subtotal = order.Subtotal ?? 0m;
        var taxAmount = order.TaxAmount ?? 0m;
        var shippingAmount = order.ShippingAmount ?? 0m;
        var totalAmount = order.TotalAmount ?? subtotal + taxAmount + shippingAmount;
        var orderDate = FormatDate(order.OrderDate);
        var estimatedDelivery = order.EstimatedDeliveryDate.HasValue
            ? $"<p><strong>Estimated Delivery:</strong> {order.EstimatedDeliveryDate.Value:MMM dd, yyyy}</p>"
            : string.Empty;
        var shippingInfo = ConvertToHtmlLines(order.ShippingAddress);
        var billingInfo = ConvertToHtmlLines(order.BillingAddress);
        var notesInfo = ConvertToHtmlLines(order.Notes, "No additional notes provided.");

        return $@"<!DOCTYPE html>
        <html>
        <head>
            <title>Cash Order Delivery Receipt</title>
        </head>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px;'>
                <h2 style='color: #2c5aa0; margin-top: 0;'>Cash Order - Delivery Receipt</h2>
                
                <p>Dear Customer,</p>
                
                <p>Thank you for your cash order! We have prepared your order for delivery. Please find attached your delivery receipt which our driver will use to collect payment upon delivery.</p>
                
                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Order Details</h3>
                    <p><strong>Order Number:</strong> {HtmlEncode(order.OrderNumber)}</p>
                    <p><strong>Order Date:</strong> {orderDate}</p>
                    <p><strong>Payment Method:</strong> Cash on Delivery</p>
                    <p><strong>Status:</strong> {order.Status}</p>
                    {estimatedDelivery}
                    <p><strong>Amount Due on Delivery:</strong> <span style='font-size: 1.2em; font-weight: bold; color: #28a745;'>${totalAmount:F2}</span></p>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Items Ordered</h3>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <thead>
                            <tr style='background-color: #f8f9fa;'>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: left;'>Product</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: center;'>Qty</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: right;'>Price</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: right;'>Total</th>
                            </tr>
                        </thead>
                        <tbody>
                            {itemsHtml}
                        </tbody>
                        <tfoot>
                            <tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Subtotal:</td>
                                <td style='padding: 8px; text-align: right;'>${subtotal:F2}</td>
                            </tr>
                            {(taxAmount > 0 ? $@"<tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Tax:</td>
                                <td style='padding: 8px; text-align: right;'>${taxAmount:F2}</td>
                            </tr>" : string.Empty)}
                            {(shippingAmount > 0 ? $@"<tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Delivery:</td>
                                <td style='padding: 8px; text-align: right;'>${shippingAmount:F2}</td>
                            </tr>" : string.Empty)}
                            <tr style='font-weight: bold; background-color: #f8f9fa; border-top: 2px solid #dee2e6;'>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Total Due:</td>
                                <td style='padding: 8px; text-align: right;'>${totalAmount:F2}</td>
                            </tr>
                        </tfoot>
                    </table>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Delivery Address</h3>
                    <p>{shippingInfo}</p>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Order Notes</h3>
                    <p>{notesInfo}</p>
                </div>

                <div style='background-color: #d4edda; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #28a745;'>
                    <h3 style='margin-top: 0; color: #155724;'>Cash on Delivery Instructions</h3>
                    <p><strong>Payment Due:</strong> ${totalAmount:F2} in cash upon delivery</p>
                    <p><strong>Exact Change:</strong> Please have exact change ready if possible</p>
                    <p><strong>Receipt:</strong> Our driver will provide you with a receipt upon payment</p>
                    <p><strong>Contact:</strong> Please be available at the delivery address during the estimated delivery window</p>
                </div>

                <div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #ffc107;'>
                    <h3 style='margin-top: 0; color: #856404;'>Important Notes</h3>
                    <p>• Our driver will contact you before delivery</p>
                    <p>• If you're not available, we'll schedule a re-delivery</p>
                    <p>• Payment must be made in cash - we cannot accept checks or cards</p>
                    <p>• Keep this receipt for your records</p>
                </div>

                <p>We will contact you when your order is ready for delivery. Thank you for your business!</p>

                <p>Best regards,<br>The ProductReviews Team</p>

                <hr style='margin: 30px 0; border: none; border-top: 1px solid #dee2e6;'>
                <p style='font-size: 12px; color: #6c757d;'>
                    This delivery receipt was automatically generated. Please retain for your records.
                </p>
            </div>
        </body>
        </html>";
    }

    private string GenerateCashOrderNotificationEmail(Order order, string customerEmail)
    {
        var itemsHtml = BuildOrderItemsHtml(order);
        var subtotal = order.Subtotal ?? 0m;
        var taxAmount = order.TaxAmount ?? 0m;
        var shippingAmount = order.ShippingAmount ?? 0m;
        var totalAmount = order.TotalAmount ?? subtotal + taxAmount + shippingAmount;
        var orderDate = FormatDate(order.OrderDate);
        var estimatedDelivery = order.EstimatedDeliveryDate.HasValue
            ? FormatDate(order.EstimatedDeliveryDate, "Not set")
            : "<em>Not set</em>";
        var shippingInfo = ConvertToHtmlLines(order.ShippingAddress);
        var notesInfo = ConvertToHtmlLines(order.Notes, "No additional notes provided.");

        return $@"<!DOCTYPE html>
        <html>
        <head>
            <title>New Cash Order for Delivery</title>
        </head>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px;'>
                <h2 style='color: #2c5aa0; margin-top: 0;'>New Cash Order for Delivery</h2>
                
                <p>A new cash order has been placed and requires delivery coordination.</p>
                
                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Order Summary</h3>
                    <p><strong>Order Number:</strong> {HtmlEncode(order.OrderNumber)}</p>
                    <p><strong>Customer Email:</strong> {HtmlEncode(customerEmail)}</p>
                    <p><strong>Order Date:</strong> {orderDate}</p>
                    <p><strong>Payment Method:</strong> Cash on Delivery</p>
                    <p><strong>Status:</strong> {order.Status}</p>
                    <p><strong>Estimated Delivery:</strong> {estimatedDelivery}</p>
                    <p><strong>Cash to Collect:</strong> <span style='font-weight: bold; color: #28a745;'>${totalAmount:F2}</span></p>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Items for Delivery</h3>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <thead>
                            <tr style='background-color: #f8f9fa;'>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: left;'>Product</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: center;'>Qty</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: right;'>Price</th>
                                <th style='padding: 8px; border-bottom: 2px solid #dee2e6; text-align: right;'>Total</th>
                            </tr>
                        </thead>
                        <tbody>
                            {itemsHtml}
                        </tbody>
                        <tfoot>
                            <tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Subtotal:</td>
                                <td style='padding: 8px; text-align: right;'>${subtotal:F2}</td>
                            </tr>
                            {(taxAmount > 0 ? $@"<tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Tax:</td>
                                <td style='padding: 8px; text-align: right;'>${taxAmount:F2}</td>
                            </tr>" : string.Empty)}
                            {(shippingAmount > 0 ? $@"<tr>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Delivery:</td>
                                <td style='padding: 8px; text-align: right;'>${shippingAmount:F2}</td>
                            </tr>" : string.Empty)}
                            <tr style='font-weight: bold; background-color: #f8f9fa; border-top: 2px solid #dee2e6;'>
                                <td colspan='3' style='padding: 8px; text-align: right;'>Total Cash to Collect:</td>
                                <td style='padding: 8px; text-align: right;'>${totalAmount:F2}</td>
                            </tr>
                        </tfoot>
                    </table>
                </div>

                <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #2c5aa0;'>Delivery Information</h3>
                    <h4 style='margin-top: 0; color: #495057;'>Delivery Address</h4>
                    <p>{shippingInfo}</p>
                    
                    <h4 style='margin-top: 20px; color: #495057;'>Special Instructions</h4>
                    <p>{notesInfo}</p>
                </div>

                <div style='background-color: #d4edda; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #28a745;'>
                    <h3 style='margin-top: 0; color: #155724;'>Driver Instructions</h3>
                    <p><strong>Cash Collection:</strong> Collect exactly ${totalAmount:F2} in cash</p>
                    <p><strong>Customer Contact:</strong> Call customer before delivery</p>
                    <p><strong>Receipt:</strong> Provide customer with delivery receipt (attached)</p>
                    <p><strong>Change:</strong> Be prepared to make change if customer doesn't have exact amount</p>
                </div>

                <div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #ffc107;'>
                    <h3 style='margin-top: 0; color: #856404;'>Next Steps</h3>
                    <p><strong>1. Prepare Order:</strong> Package items for delivery</p>
                    <p><strong>2. Schedule Delivery:</strong> Contact customer to arrange delivery time</p>
                    <p><strong>3. Dispatch Driver:</strong> Provide driver with delivery receipt and change</p>
                    <p><strong>4. Complete Delivery:</strong> Update order status to 'Delivered' after successful cash collection</p>
                </div>

                <p>The delivery receipt has been sent to the customer and is attached for driver reference.</p>
            </div>
        </body>
        </html>";
    }
}