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
public class BrandController(ApplicationDbContext ctx) : ControllerBase
{
    [HttpGet("")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<IQueryable<Brand>> Get()
    {
        return Ok(ctx.Brand);
    }

    [HttpGet("{key}")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Brand>> GetAsync(long key)
    {
        var brand = await ctx.Brand.FirstOrDefaultAsync(x => x.Id == key);

        if (brand == null)
        {
            return NotFound();
        }
        else
        {
            return Ok(brand);
        }
    }

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Brand>> PostAsync(Brand brand)
    {
        var record = await ctx.Brand.FindAsync(brand.Id);
        if (record != null)
        {
            return Conflict();
        }
    
        await ctx.Brand.AddAsync(brand);

        await ctx.SaveChangesAsync();

        return Created($"/brand/{brand.Id}", brand);
    }

    [HttpPut("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Brand>> PutAsync(long key, Brand update)
    {
        var brand = await ctx.Brand.FirstOrDefaultAsync(x => x.Id == key);

        if (brand == null)
        {
            return NotFound();
        }

        ctx.Entry(brand).CurrentValues.SetValues(update);

        await ctx.SaveChangesAsync();

        return Ok(brand);
    }

    [HttpPatch("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Brand>> PatchAsync(long key, Delta<Brand> delta)
    {
        var brand = await ctx.Brand.FirstOrDefaultAsync(x => x.Id == key);

        if (brand == null)
        {
            return NotFound();
        }

        delta.Patch(brand);

        await ctx.SaveChangesAsync();

        return Ok(brand);
    }

    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(long key)
    {
        var brand = await ctx.Brand.FindAsync(key);

        if (brand != null)
        {
            ctx.Brand.Remove(brand);
            await ctx.SaveChangesAsync();
        }

        return NoContent();
    }
}
