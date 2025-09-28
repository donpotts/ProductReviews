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
    
        var productIds = tag.Product.Select(y => y.Id).ToList();
        tag.Product = new();

        await ctx.Tag.AddAsync(tag);

        if (productIds.Count > 0)
        {
            var newValues = await ctx.Product.Where(x => productIds.Contains(x.Id)).ToListAsync();
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

    [HttpPost("bulk-upsert")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<object>> BulkUpsertAsync(List<Tag> tags)
    {
        if (tags == null || !tags.Any())
        {
            return BadRequest("No tags provided");
        }

        var processedCount = 0;
        var addedCount = 0;
        var updatedCount = 0;
        var errors = new List<string>();

        try
        {
            foreach (var tag in tags)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    if (tag.Id.HasValue && tag.Id.Value > 0)
                    {
                        var existingTag = await ctx.Tag.FindAsync(tag.Id.Value);
                        if (existingTag != null)
                        {
                            existingTag.Name = tag.Name;
                            existingTag.Description = tag.Description;
                            existingTag.Color = tag.Color;
                            existingTag.Notes = tag.Notes;
                            existingTag.UserId = tag.UserId;
                            existingTag.ModifiedDate = now;
                            ctx.Tag.Update(existingTag);
                            updatedCount++;
                        }
                        else
                        {
                            tag.CreatedDate = now;
                            tag.ModifiedDate = now;
                            await ctx.Tag.AddAsync(tag);
                            addedCount++;
                        }
                    }
                    else
                    {
                        var existingByName = await ctx.Tag
                            .FirstOrDefaultAsync(c => c.Name == tag.Name && !string.IsNullOrEmpty(tag.Name));
                        if (existingByName != null)
                        {
                            existingByName.Description = tag.Description;
                            existingByName.Color = tag.Color;
                            existingByName.Notes = tag.Notes;
                            existingByName.UserId = tag.UserId;
                            existingByName.ModifiedDate = now;
                            ctx.Tag.Update(existingByName);
                            updatedCount++;
                        }
                        else
                        {
                            tag.CreatedDate = now;
                            tag.ModifiedDate = now;
                            await ctx.Tag.AddAsync(tag);
                            addedCount++;
                        }
                    }
                    processedCount++;
                }
                catch (Exception ex)
                {
                    var tagName = tag?.Name ?? "Unknown";
                    errors.Add($"Row {processedCount + 1} (Tag: {tagName}): {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        errors.Add($"  Inner error: {ex.InnerException.Message}");
                    }
                }
            }
            await ctx.SaveChangesAsync();
            return Ok(new { ProcessedCount = processedCount, AddedCount = addedCount, UpdatedCount = updatedCount, Errors = errors, Success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { ProcessedCount = processedCount, AddedCount = addedCount, UpdatedCount = updatedCount, Errors = new[] { ex.Message }, Success = false });
        }
    }
}
