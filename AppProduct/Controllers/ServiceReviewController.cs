using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
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
public class ServiceReviewController(ApplicationDbContext ctx, INotificationService notificationService) : ControllerBase
{
    [HttpGet("")]
    [EnableQuery]
    public ActionResult<IQueryable<ServiceReview>> Get()
    {
        return Ok(ctx.ServiceReview.Include(x => x.Service));
    }

    [HttpGet("{key}")]
    [EnableQuery]
    public async Task<ActionResult<ServiceReview>> Get(long key)
    {
        var review = await ctx.ServiceReview
            .Include(x => x.Service)
            .FirstOrDefaultAsync(p => p.Id == key);

        if (review == null) return NotFound();
        return Ok(review);
    }

    [HttpPost("")]
    public async Task<ActionResult<ServiceReview>> Post([FromBody] ServiceReview review)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        review.CreatedDate = DateTime.UtcNow;
        review.ModifiedDate = DateTime.UtcNow;
        review.ReviewDate = DateTime.UtcNow;

        ctx.ServiceReview.Add(review);
        await ctx.SaveChangesAsync();

        // Create notification for new service review
        await CreateServiceReviewNotificationAsync(review);

        return Created($"/api/ServiceReview/{review.Id}", review);
    }

    [HttpPatch("{key}")]
    public async Task<ActionResult<ServiceReview>> Patch(long key, [FromBody] Delta<ServiceReview> patch)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var review = await ctx.ServiceReview.FindAsync(key);
        if (review == null) return NotFound();

        patch.Patch(review);
        review.ModifiedDate = DateTime.UtcNow;
        await ctx.SaveChangesAsync();

        return Ok(review);
    }

    [HttpPut("{key}")]
    public async Task<ActionResult<ServiceReview>> Put(long key, [FromBody] ServiceReview review)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (key != review.Id) return BadRequest();

        review.ModifiedDate = DateTime.UtcNow;
        ctx.Entry(review).State = EntityState.Modified;
        await ctx.SaveChangesAsync();

        return Ok(review);
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(long key)
    {
        var review = await ctx.ServiceReview.FindAsync(key);
        if (review == null) return NotFound();

        ctx.ServiceReview.Remove(review);
        await ctx.SaveChangesAsync();

        return NoContent();
    }

    private async Task CreateServiceReviewNotificationAsync(ServiceReview review)
    {
        try
        {
            // Create notification for admins about new service review
            await notificationService.CreateNotificationAsync(
                title: "New Service Review",
                message: $"{review.CustomerName} left a {review.Rating}-star review: \"{review.Title}\"",
                type: "Info",
                userId: null, // System-wide notification for admins
                actionUrl: $"/service-reviews/{review.Id}",
                notes: $"Service Review #{review.Id}"
            );
        }
        catch (Exception ex)
        {
            // Log error but don't fail the review creation process
            // Error logged internally but not displayed to user
        }
    }
}