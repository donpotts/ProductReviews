using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using AppProduct.Data;
using AppProduct.Shared.Models;

namespace AppProduct.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
[EnableRateLimiting("Fixed")]
public class ServiceReviewController(ApplicationDbContext ctx) : ControllerBase
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
}