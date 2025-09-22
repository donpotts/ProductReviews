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
public class ProductController(ApplicationDbContext ctx) : ControllerBase
{
    [HttpGet("")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<IQueryable<Product>> Get()
    {
        return Ok(ctx.Product.Include(x => x.Category).Include(x => x.Brand).Include(x => x.Feature).Include(x => x.Tag).Include(x => x.ProductReview));
    }

    [HttpGet("{key}")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> GetAsync(long key)
    {
        var product = await ctx.Product.Include(x => x.Category).Include(x => x.Brand).Include(x => x.Feature).Include(x => x.Tag).Include(x => x.ProductReview).FirstOrDefaultAsync(x => x.Id == key);

        if (product == null)
        {
            return NotFound();
        }
        else
        {
            return Ok(product);
        }
    }

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Product>> PostAsync(Product product)
    {
        var record = await ctx.Product.FindAsync(product.Id);
        if (record != null)
        {
            return Conflict();
        }
    
        var category = product.Category;
        product.Category = null;

        var brand = product.Brand;
        product.Brand = null;

        var feature = product.Feature;
        product.Feature = null;

        var tag = product.Tag;
        product.Tag = null;

        var productReview = product.ProductReview;
        product.ProductReview = null;

        await ctx.Product.AddAsync(product);

        if (category != null)
        {
            var newValues = await ctx.Category.Where(x => category.Select(y => y.Id).Contains(x.Id)).ToListAsync();
            product.Category = [..newValues];
        }

        if (brand != null)
        {
            var newValues = await ctx.Brand.Where(x => brand.Select(y => y.Id).Contains(x.Id)).ToListAsync();
            product.Brand = [..newValues];
        }

        if (feature != null)
        {
            var newValues = await ctx.Feature.Where(x => feature.Select(y => y.Id).Contains(x.Id)).ToListAsync();
            product.Feature = [..newValues];
        }

        if (tag != null)
        {
            var newValues = await ctx.Tag.Where(x => tag.Select(y => y.Id).Contains(x.Id)).ToListAsync();
            product.Tag = [..newValues];
        }

        if (productReview != null)
        {
            var newValues = await ctx.ProductReview.Where(x => productReview.Select(y => y.Id).Contains(x.Id)).ToListAsync();
            product.ProductReview = [..newValues];
        }

        await ctx.SaveChangesAsync();

        return Created($"/product/{product.Id}", product);
    }

    [HttpPut("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> PutAsync(long key, Product update)
    {
        var product = await ctx.Product.Include(x => x.Category).Include(x => x.Brand).Include(x => x.Feature).Include(x => x.Tag).Include(x => x.ProductReview).FirstOrDefaultAsync(x => x.Id == key);

        if (product == null)
        {
            return NotFound();
        }

        ctx.Entry(product).CurrentValues.SetValues(update);

        if (update.Category != null)
        {
            var updateValues = update.Category.Select(x => x.Id);
            product.Category ??= [];
            product.Category.RemoveAll(x => !updateValues.Contains(x.Id));
            var addValues = updateValues.Where(x => !product.Category.Select(y => y.Id).Contains(x));
            var newValues = await ctx.Category.Where(x => addValues.Contains(x.Id)).ToListAsync();
            product.Category.AddRange(newValues);
        }

        if (update.Brand != null)
        {
            var updateValues = update.Brand.Select(x => x.Id);
            product.Brand ??= [];
            product.Brand.RemoveAll(x => !updateValues.Contains(x.Id));
            var addValues = updateValues.Where(x => !product.Brand.Select(y => y.Id).Contains(x));
            var newValues = await ctx.Brand.Where(x => addValues.Contains(x.Id)).ToListAsync();
            product.Brand.AddRange(newValues);
        }

        if (update.Feature != null)
        {
            var updateValues = update.Feature.Select(x => x.Id);
            product.Feature ??= [];
            product.Feature.RemoveAll(x => !updateValues.Contains(x.Id));
            var addValues = updateValues.Where(x => !product.Feature.Select(y => y.Id).Contains(x));
            var newValues = await ctx.Feature.Where(x => addValues.Contains(x.Id)).ToListAsync();
            product.Feature.AddRange(newValues);
        }

        if (update.Tag != null)
        {
            var updateValues = update.Tag.Select(x => x.Id);
            product.Tag ??= [];
            product.Tag.RemoveAll(x => !updateValues.Contains(x.Id));
            var addValues = updateValues.Where(x => !product.Tag.Select(y => y.Id).Contains(x));
            var newValues = await ctx.Tag.Where(x => addValues.Contains(x.Id)).ToListAsync();
            product.Tag.AddRange(newValues);
        }

        if (update.ProductReview != null)
        {
            var updateValues = update.ProductReview.Select(x => x.Id);
            product.ProductReview ??= [];
            product.ProductReview.RemoveAll(x => !updateValues.Contains(x.Id));
            var addValues = updateValues.Where(x => !product.ProductReview.Select(y => y.Id).Contains(x));
            var newValues = await ctx.ProductReview.Where(x => addValues.Contains(x.Id)).ToListAsync();
            product.ProductReview.AddRange(newValues);
        }

        await ctx.SaveChangesAsync();

        return Ok(product);
    }

    [HttpPatch("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> PatchAsync(long key, Delta<Product> delta)
    {
        var product = await ctx.Product.Include(x => x.Category).Include(x => x.Brand).Include(x => x.Feature).Include(x => x.Tag).Include(x => x.ProductReview).FirstOrDefaultAsync(x => x.Id == key);

        if (product == null)
        {
            return NotFound();
        }

        delta.Patch(product);

        await ctx.SaveChangesAsync();

        return Ok(product);
    }

    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(long key)
    {
        var product = await ctx.Product.FindAsync(key);

        if (product != null)
        {
            ctx.Product.Remove(product);
            await ctx.SaveChangesAsync();
        }

        return NoContent();
    }
}
