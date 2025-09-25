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
public class FeatureController(ApplicationDbContext ctx) : ControllerBase
{
    [HttpGet("")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<IQueryable<Feature>> Get()
    {
        return Ok(ctx.Feature.Include(x => x.Product));
    }

    [HttpGet("{key}")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Feature>> GetAsync(long key)
    {
        var feature = await ctx.Feature.Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == key);

        if (feature == null)
        {
            return NotFound();
        }
        else
        {
            return Ok(feature);
        }
    }

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Feature>> PostAsync(Feature feature)
    {
        var record = await ctx.Feature.FindAsync(feature.Id);
        if (record != null)
        {
            return Conflict();
        }
    
        var product = feature.Product;
        feature.Product = null;

        await ctx.Feature.AddAsync(feature);

        if (product != null)
        {
            var newValues = await ctx.Product.Where(x => product.Select(y => y.Id).Contains(x.Id)).ToListAsync();
            feature.Product = [..newValues];
        }

        await ctx.SaveChangesAsync();

        return Created($"/feature/{feature.Id}", feature);
    }

    [HttpPut("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Feature>> PutAsync(long key, Feature update)
    {
        var feature = await ctx.Feature.Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == key);

        if (feature == null)
        {
            return NotFound();
        }

        ctx.Entry(feature).CurrentValues.SetValues(update);

        if (update.Product != null)
        {
            var updateValues = update.Product.Select(x => x.Id);
            feature.Product ??= [];
            feature.Product.RemoveAll(x => !updateValues.Contains(x.Id));
            var addValues = updateValues.Where(x => !feature.Product.Select(y => y.Id).Contains(x));
            var newValues = await ctx.Product.Where(x => addValues.Contains(x.Id)).ToListAsync();
            feature.Product.AddRange(newValues);
        }

        await ctx.SaveChangesAsync();

        return Ok(feature);
    }

    [HttpPatch("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Feature>> PatchAsync(long key, Delta<Feature> delta)
    {
        var feature = await ctx.Feature.Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == key);

        if (feature == null)
        {
            return NotFound();
        }

        delta.Patch(feature);

        await ctx.SaveChangesAsync();

        return Ok(feature);
    }

    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(long key)
    {
        var feature = await ctx.Feature.FindAsync(key);

        if (feature != null)
        {
            ctx.Feature.Remove(feature);
            await ctx.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpPost("bulk-upsert")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<object>> BulkUpsertAsync(List<Feature> features)
    {
        if (features == null || !features.Any())
        {
            return BadRequest("No features provided");
        }

        var processedCount = 0;
        var addedCount = 0;
        var updatedCount = 0;
        var errors = new List<string>();

        try
        {
            foreach (var feature in features)
            {
                try
                {
                    // Set timestamps
                    var now = DateTime.UtcNow;
                    if (feature.Id.HasValue && feature.Id.Value > 0)
                    {
                        // Update existing feature
                        var existingFeature = await ctx.Feature.FindAsync(feature.Id.Value);
                        if (existingFeature != null)
                        {
                            existingFeature.Name = feature.Name;
                            existingFeature.Description = feature.Description;
                            existingFeature.IconUrl = feature.IconUrl;
                            existingFeature.Type = feature.Type;
                            existingFeature.Notes = feature.Notes;
                            existingFeature.UserId = feature.UserId;
                            existingFeature.ModifiedDate = now;
                            
                            ctx.Feature.Update(existingFeature);
                            updatedCount++;
                        }
                        else
                        {
                            // ID provided but feature doesn't exist, create new one
                            feature.CreatedDate = now;
                            feature.ModifiedDate = now;
                            await ctx.Feature.AddAsync(feature);
                            addedCount++;
                        }
                    }
                    else
                    {
                        // Check if feature with same name already exists
                        var existingByName = await ctx.Feature
                            .FirstOrDefaultAsync(c => c.Name == feature.Name && !string.IsNullOrEmpty(feature.Name));
                        
                        if (existingByName != null)
                        {
                            // Update existing feature by name
                            existingByName.Description = feature.Description;
                            existingByName.IconUrl = feature.IconUrl;
                            existingByName.Type = feature.Type;
                            existingByName.Notes = feature.Notes;
                            existingByName.UserId = feature.UserId;
                            existingByName.ModifiedDate = now;
                            
                            ctx.Feature.Update(existingByName);
                            updatedCount++;
                        }
                        else
                        {
                            // Create new feature
                            feature.CreatedDate = now;
                            feature.ModifiedDate = now;
                            await ctx.Feature.AddAsync(feature);
                            addedCount++;
                        }
                    }
                    
                    processedCount++;
                }
                catch (Exception ex)
                {
                    var featureName = feature?.Name ?? "Unknown";
                    errors.Add($"Row {processedCount + 1} (Feature: {featureName}): {ex.Message}");
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
