using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using AppProduct.Data;
using AppProduct.Shared.Models;
using AppProduct.Services;

namespace AppProduct.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
[EnableRateLimiting("Fixed")]
public class ProductReviewController(ApplicationDbContext ctx, IEmailNotificationService emailNotificationService) : ControllerBase
{
    [HttpGet("")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<IQueryable<ProductReview>> Get()
    {
        return Ok(ctx.ProductReview);
    }

    [HttpGet("{key}")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductReview>> GetAsync(long key)
    {
        var productReview = await ctx.ProductReview.FirstOrDefaultAsync(x => x.Id == key);

        if (productReview == null)
        {
            return NotFound();
        }
        else
        {
            return Ok(productReview);
        }
    }

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProductReview>> PostAsync(ProductReview productReview)
    {
        var record = await ctx.ProductReview.FindAsync(productReview.Id);
        if (record != null)
        {
            return Conflict();
        }
    
        await ctx.ProductReview.AddAsync(productReview);
        await ctx.SaveChangesAsync();

        // Send new review notification email
        _ = Task.Run(async () =>
        {
            try
            {
                var product = await ctx.Product.FindAsync(productReview.ProductId);
                if (product != null)
                {
                    await emailNotificationService.SendNewReviewNotificationAsync(productReview, product);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the review creation
                Console.WriteLine($"Failed to send new review notification: {ex.Message}");
            }
        });

        return Created($"/productreview/{productReview.Id}", productReview);
    }

    [HttpPut("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductReview>> PutAsync(long key, ProductReview update)
    {
        var productReview = await ctx.ProductReview.FirstOrDefaultAsync(x => x.Id == key);

        if (productReview == null)
        {
            return NotFound();
        }

        var wasResponseEmpty = string.IsNullOrEmpty(productReview.Response);
        var hasNewResponse = !string.IsNullOrEmpty(update.Response);

        ctx.Entry(productReview).CurrentValues.SetValues(update);
        await ctx.SaveChangesAsync();

        // Send response notification email if a response was added
        if (wasResponseEmpty && hasNewResponse)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var product = await ctx.Product.FindAsync(productReview.ProductId);
                    if (product != null)
                    {
                        await emailNotificationService.SendReviewResponseNotificationAsync(productReview, product);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the update
                    Console.WriteLine($"Failed to send response notification: {ex.Message}");
                }
            });
        }

        return Ok(productReview);
    }

    [HttpPatch("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductReview>> PatchAsync(long key, Delta<ProductReview> delta)
    {
        var productReview = await ctx.ProductReview.FirstOrDefaultAsync(x => x.Id == key);

        if (productReview == null)
        {
            return NotFound();
        }

        delta.Patch(productReview);

        await ctx.SaveChangesAsync();

        return Ok(productReview);
    }

    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(long key)
    {
        var productReview = await ctx.ProductReview.FindAsync(key);

        if (productReview != null)
        {
            ctx.ProductReview.Remove(productReview);
            await ctx.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpPost("bulk-upsert")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<object>> BulkUpsertAsync(List<ProductReview> productReviews)
    {
        if (productReviews == null || !productReviews.Any())
        {
            return BadRequest("No product reviews provided");
        }

        var processedCount = 0;
        var addedCount = 0;
        var updatedCount = 0;
        var errors = new List<string>();

        try
        {
            foreach (var productReview in productReviews)
            {
                try
                {
                    // Set defaults for required fields if missing
                    if (string.IsNullOrWhiteSpace(productReview.CustomerName))
                    {
                        productReview.CustomerName = $"Customer {processedCount + 1}";
                    }
                    if (string.IsNullOrWhiteSpace(productReview.CustomerEmail))
                    {
                        productReview.CustomerEmail = $"customer{processedCount + 1}@example.com";
                    }
                    if (!productReview.Rating.HasValue)
                    {
                        productReview.Rating = 5.0m; // Default to 5 stars
                    }
                    
                    // Set timestamps
                    var now = DateTime.UtcNow;
                    if (productReview.Id.HasValue && productReview.Id.Value > 0)
                    {
                        // Update existing product review
                        var existingReview = await ctx.ProductReview.FindAsync(productReview.Id.Value);
                        if (existingReview != null)
                        {
                            existingReview.ProductId = productReview.ProductId;
                            existingReview.CustomerName = productReview.CustomerName;
                            existingReview.CustomerEmail = productReview.CustomerEmail;
                            existingReview.Rating = productReview.Rating;
                            existingReview.Title = productReview.Title;
                            existingReview.ReviewText = productReview.ReviewText;
                            existingReview.ReviewDate = productReview.ReviewDate ?? now;
                            existingReview.IsVerifiedPurchase = productReview.IsVerifiedPurchase;
                            existingReview.HelpfulVotes = productReview.HelpfulVotes ?? 0;
                            existingReview.Notes = productReview.Notes;
                            existingReview.UserId = productReview.UserId;
                            existingReview.Response = productReview.Response;
                            existingReview.ResponseDate = productReview.ResponseDate;
                            existingReview.ResponseUserId = productReview.ResponseUserId;
                            existingReview.ModifiedDate = now;
                            
                            ctx.ProductReview.Update(existingReview);
                            updatedCount++;
                        }
                        else
                        {
                            // ID provided but review doesn't exist, create new one
                            productReview.CreatedDate = now;
                            productReview.ModifiedDate = now;
                            productReview.ReviewDate ??= now;
                            productReview.HelpfulVotes ??= 0;
                            await ctx.ProductReview.AddAsync(productReview);
                            addedCount++;
                        }
                    }
                    else
                    {
                        // Create new product review (reviews are typically unique by content)
                        productReview.CreatedDate = now;
                        productReview.ModifiedDate = now;
                        productReview.ReviewDate ??= now;
                        productReview.HelpfulVotes ??= 0;
                        await ctx.ProductReview.AddAsync(productReview);
                        addedCount++;
                    }
                    
                    processedCount++;
                }
                catch (Exception ex)
                {
                    var reviewTitle = productReview?.Title ?? "Unknown";
                    errors.Add($"Row {processedCount + 1} (Review: {reviewTitle}): {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        errors.Add($"  Inner error: {ex.InnerException.Message}");
                    }
                }
            }

            await ctx.SaveChangesAsync();

            return Ok(new 
            { 
                ProcessedCount = processedCount,
                AddedCount = addedCount,
                UpdatedCount = updatedCount,
                Errors = errors,
                Success = true
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new 
            { 
                ProcessedCount = processedCount,
                AddedCount = addedCount,
                UpdatedCount = updatedCount,
                Errors = new[] { ex.Message },
                Success = false
            });
        }
    }
}
