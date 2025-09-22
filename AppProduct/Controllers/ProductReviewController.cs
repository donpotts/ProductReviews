using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using AppProduct.Data;
using AppProduct.Shared.Models;

namespace AppProduct.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
[EnableRateLimiting("Fixed")]
public class ProductReviewController(ApplicationDbContext ctx) : ControllerBase
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

        ctx.Entry(productReview).CurrentValues.SetValues(update);

        await ctx.SaveChangesAsync();

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
}
