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
public class ServiceCategoryController(ApplicationDbContext ctx) : ControllerBase
{
    [HttpGet("")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<IQueryable<ServiceCategory>> Get()
    {
        return Ok(ctx.ServiceCategory.Include(x => x.Service));
    }

    [HttpGet("{key}")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceCategory>> Get(long key)
    {
        var category = await ctx.ServiceCategory
            .Include(x => x.Service)
            .FirstOrDefaultAsync(p => p.Id == key);

        if (category == null)
        {
            return NotFound();
        }

        return Ok(category);
    }

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ServiceCategory>> Post([FromBody] ServiceCategory category)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        category.CreatedDate = DateTime.UtcNow;
        category.ModifiedDate = DateTime.UtcNow;

        ctx.ServiceCategory.Add(category);
        await ctx.SaveChangesAsync();

        return Created($"/api/ServiceCategory/{category.Id}", category);
    }

    [HttpPatch("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceCategory>> Patch(long key, [FromBody] Delta<ServiceCategory> patch)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var category = await ctx.ServiceCategory.FindAsync(key);
        if (category == null)
        {
            return NotFound();
        }

        patch.Patch(category);
        category.ModifiedDate = DateTime.UtcNow;

        try
        {
            await ctx.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ServiceCategoryExists(key))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return Ok(category);
    }

    [HttpPut("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceCategory>> Put(long key, [FromBody] ServiceCategory category)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (key != category.Id)
        {
            return BadRequest();
        }

        category.ModifiedDate = DateTime.UtcNow;
        ctx.Entry(category).State = EntityState.Modified;

        try
        {
            await ctx.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ServiceCategoryExists(key))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return Ok(category);
    }

    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long key)
    {
        var category = await ctx.ServiceCategory.FindAsync(key);
        if (category == null)
        {
            return NotFound();
        }

        ctx.ServiceCategory.Remove(category);
        await ctx.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("bulk-upsert")]
    public async Task<ActionResult<object>> BulkUpsertAsync(List<ServiceCategory> categories)
    {
        if (categories == null || !categories.Any())
        {
            return BadRequest("No categories provided");
        }

        var processedCount = 0;
        var addedCount = 0;
        var updatedCount = 0;
        var errors = new List<string>();

        try
        {
            foreach (var category in categories)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(category.Name))
                    {
                        category.Name = $"Category {processedCount + 1}";
                    }
                    category.Name = category.Name?.Trim();
                    
                    var now = DateTime.UtcNow;
                    if (category.Id.HasValue && category.Id.Value > 0)
                    {
                        var existing = await ctx.ServiceCategory.FindAsync(category.Id.Value);
                        if (existing != null)
                        {
                            existing.Name = category.Name;
                            existing.Description = category.Description;
                            existing.IconClass = category.IconClass;
                            existing.ColorCode = category.ColorCode;
                            existing.IsActive = category.IsActive;
                            existing.SortOrder = category.SortOrder;
                            existing.ModifiedDate = now;
                            updatedCount++;
                        }
                        else
                        {
                            errors.Add($"Category with ID {category.Id} not found for update");
                        }
                    }
                    else
                    {
                        category.Id = null;
                        category.CreatedDate = now;
                        category.ModifiedDate = now;
                        ctx.ServiceCategory.Add(category);
                        addedCount++;
                    }
                    processedCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Error processing category {processedCount + 1}: {ex.Message}");
                }
            }

            await ctx.SaveChangesAsync();

            return Ok(new
            {
                ProcessedCount = processedCount,
                AddedCount = addedCount,
                UpdatedCount = updatedCount,
                ErrorCount = errors.Count,
                Errors = errors
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Bulk upsert failed: {ex.Message}");
        }
    }

    private bool ServiceCategoryExists(long id)
    {
        return ctx.ServiceCategory.Any(e => e.Id == id);
    }
}