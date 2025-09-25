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

    [HttpPost("bulk-upsert")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<object>> BulkUpsertAsync(List<Brand> brands)
    {
        if (brands == null || !brands.Any())
        {
            return BadRequest("No brands provided");
        }

        var processedCount = 0;
        var addedCount = 0;
        var updatedCount = 0;
        var errors = new List<string>();

        try
        {
            foreach (var brand in brands)
            {
                try
                {
                    // Set timestamps
                    var now = DateTime.UtcNow;
                    if (brand.Id.HasValue && brand.Id.Value > 0)
                    {
                        // Update existing brand
                        var existingBrand = await ctx.Brand.FindAsync(brand.Id.Value);
                        if (existingBrand != null)
                        {
                            existingBrand.Name = brand.Name;
                            existingBrand.Description = brand.Description;
                            existingBrand.LogoUrl = brand.LogoUrl;
                            existingBrand.Website = brand.Website;
                            existingBrand.EstablishedDate = brand.EstablishedDate;
                            existingBrand.Notes = brand.Notes;
                            existingBrand.UserId = brand.UserId;
                            existingBrand.ModifiedDate = now;
                            
                            ctx.Brand.Update(existingBrand);
                            updatedCount++;
                        }
                        else
                        {
                            // ID provided but brand doesn't exist, create new one
                            brand.CreatedDate = now;
                            brand.ModifiedDate = now;
                            await ctx.Brand.AddAsync(brand);
                            addedCount++;
                        }
                    }
                    else
                    {
                        // Check if brand with same name already exists
                        var existingByName = await ctx.Brand
                            .FirstOrDefaultAsync(c => c.Name == brand.Name && !string.IsNullOrEmpty(brand.Name));
                        
                        if (existingByName != null)
                        {
                            // Update existing brand by name
                            existingByName.Description = brand.Description;
                            existingByName.LogoUrl = brand.LogoUrl;
                            existingByName.Website = brand.Website;
                            existingByName.EstablishedDate = brand.EstablishedDate;
                            existingByName.Notes = brand.Notes;
                            existingByName.UserId = brand.UserId;
                            existingByName.ModifiedDate = now;
                            
                            ctx.Brand.Update(existingByName);
                            updatedCount++;
                        }
                        else
                        {
                            // Create new brand
                            brand.CreatedDate = now;
                            brand.ModifiedDate = now;
                            await ctx.Brand.AddAsync(brand);
                            addedCount++;
                        }
                    }
                    
                    processedCount++;
                }
                catch (Exception ex)
                {
                    var brandName = brand?.Name ?? "Unknown";
                    errors.Add($"Row {processedCount + 1} (Brand: {brandName}): {ex.Message}");
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
