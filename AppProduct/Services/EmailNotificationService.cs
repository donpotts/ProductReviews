using Microsoft.Graph;
using Microsoft.Graph.Models;
using Azure.Identity;
using AppProduct.Shared.Models;

namespace AppProduct.Services;

public interface IEmailNotificationService
{
    Task SendNewReviewNotificationAsync(ProductReview review, Product product);
    Task SendReviewResponseNotificationAsync(ProductReview review, Product product);
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

            var subject = $"New Review Received for {product.Name}";
            var body = BuildNewReviewEmailBody(review, product);

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

            var subject = $"Response to Your Review of {product.Name}";
            var body = BuildResponseEmailBody(review, product);

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

    private string BuildNewReviewEmailBody(ProductReview review, Product product)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h2 style='color: #2c5aa0;'>New Review Received</h2>
                
                <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='margin-top: 0; color: #495057;'>Product: {product.Name}</h3>
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
                        <strong>Customer Email:</strong> <a href='mailto:{review.CustomerEmail}?subject=Re: Your review of {product.Name}' style='color: #2c5aa0; text-decoration: none; font-weight: bold;'>{review.CustomerEmail}</a>
                    </p>
                    <p style='margin-bottom: 0;'>
                        <a href='mailto:{review.CustomerEmail}?subject=Re: Your review of {product.Name}&body=Dear {review.CustomerName},%0A%0AThank you for your review of {product.Name}. ' 
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

    private string BuildResponseEmailBody(ProductReview review, Product product)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h2 style='color: #2c5aa0;'>Thank you for your review!</h2>
                
                <p>Dear {review.CustomerName},</p>
                
                <p>Thank you for taking the time to review <strong>{product.Name}</strong>. We have responded to your feedback:</p>

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
}