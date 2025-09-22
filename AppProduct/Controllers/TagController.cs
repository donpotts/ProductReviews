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
public class TagController(ApplicationDbContext ctx) : ControllerBase
{
    [HttpGet("")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<IQueryable<Tag>> Get()
    {
        return Ok(ctx.Tag.Include(x => x.Product));
    }

    [HttpGet("{key}")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Tag>> GetAsync(long key)
    {
        var tag = await ctx.Tag.Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == key);

        if (tag == null)
        {
            return NotFound();
        }
        else
        {
            return Ok(tag);
        }
    }

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Tag>> PostAsync(Tag tag)
    {
        var record = await ctx.Tag.FindAsync(tag.Id);
        if (record != null)
        {
            return Conflict();
        }
    
        var product = tag.Product;
        tag.Product = null;

        await ctx.Tag.AddAsync(tag);

        if (product != null)
        {
            var newValues = await ctx.Product.Where(x => product.Select(y => y.Id).Contains(x.Id)).ToListAsync();
            tag.Product = [..newValues];
        }

        await ctx.SaveChangesAsync();

        return Created($"/tag/{tag.Id}", tag);
    }

    [HttpPut("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Tag>> PutAsync(long key, Tag update)
    {
        var tag = await ctx.Tag.Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == key);

        if (tag == null)
        {
            return NotFound();
        }

        ctx.Entry(tag).CurrentValues.SetValues(update);

        if (update.Product != null)
        {
            var updateValues = update.Product.Select(x => x.Id);
            tag.Product ??= [];
            tag.Product.RemoveAll(x => !updateValues.Contains(x.Id));
            var addValues = updateValues.Where(x => !tag.Product.Select(y => y.Id).Contains(x));
            var newValues = await ctx.Product.Where(x => addValues.Contains(x.Id)).ToListAsync();
            tag.Product.AddRange(newValues);
        }

        await ctx.SaveChangesAsync();

        return Ok(tag);
    }

    [HttpPatch("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Tag>> PatchAsync(long key, Delta<Tag> delta)
    {
        var tag = await ctx.Tag.Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == key);

        if (tag == null)
        {
            return NotFound();
        }

        delta.Patch(tag);

        await ctx.SaveChangesAsync();

        return Ok(tag);
    }

    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(long key)
    {
        var tag = await ctx.Tag.FindAsync(key);

        if (tag != null)
        {
            ctx.Tag.Remove(tag);
            await ctx.SaveChangesAsync();
        }

        return NoContent();
    }
}
