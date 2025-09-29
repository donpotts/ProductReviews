using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using AppProduct.Data;
using AppProduct.Shared.Models;
using AppProduct.Services; // added

namespace AppProduct.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
[EnableRateLimiting("Fixed")]
public class ServiceController(ApplicationDbContext ctx, IEmailNotificationService emailService, ILogger<ServiceController> logger) : ControllerBase // modified constructor
{
    [HttpGet("")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<IQueryable<Service>> Get()
    {
        return Ok(ctx.Service.Include(x => x.ServiceCategory).Include(x => x.ServiceCompany).Include(x => x.ServiceFeature).Include(x => x.ServiceTag).Include(x => x.ServiceReview));
    }

    [HttpGet("{key}")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Service>> Get(long key)
    {
        var service = await ctx.Service
            .Include(x => x.ServiceCategory)
            .Include(x => x.ServiceCompany)
            .Include(x => x.ServiceFeature)
            .Include(x => x.ServiceTag)
            .Include(x => x.ServiceReview)
            .FirstOrDefaultAsync(p => p.Id == key);

        if (service == null)
        {
            return NotFound();
        }

        return Ok(service);
    }

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Service>> Post([FromBody] Service service)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        service.CreatedDate = DateTime.UtcNow;
        service.ModifiedDate = DateTime.UtcNow;

        ctx.Service.Add(service);
        await ctx.SaveChangesAsync();

        // fire and forget email (avoid blocking request)
        _ = Task.Run(async () =>
        {
            try
            {
                // reload with related data for richer email (category/company names)
                var fullService = await ctx.Service
                    .Include(s => s.ServiceCategory)
                    .Include(s => s.ServiceCompany)
                    .FirstOrDefaultAsync(s => s.Id == service.Id);
                if (fullService != null)
                {
                    await emailService.SendServiceCreatedNotificationAsync(fullService);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed sending service created email for service {ServiceId}", service.Id);
            }
        });

        return Created($"/api/Service/{service.Id}", service);
    }

    [HttpPatch("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Service>> Patch(long key, [FromBody] Delta<Service> patch)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var service = await ctx.Service.FindAsync(key);
        if (service == null)
        {
            return NotFound();
        }

        patch.Patch(service);
        service.ModifiedDate = DateTime.UtcNow;

        try
        {
            await ctx.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ServiceExists(key))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return Ok(service);
    }

    [HttpPut("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Service>> Put(long key, [FromBody] Service service)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (key != service.Id)
        {
            return BadRequest();
        }

        service.ModifiedDate = DateTime.UtcNow;
        ctx.Entry(service).State = EntityState.Modified;

        try
        {
            await ctx.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ServiceExists(key))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return Ok(service);
    }

    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long key)
    {
        var service = await ctx.Service.FindAsync(key);
        if (service == null)
        {
            return NotFound();
        }

        ctx.Service.Remove(service);
        await ctx.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("bulk-upsert")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<object>> BulkUpsertAsync(List<Service> services)
    {
        if (services == null || !services.Any())
        {
            return BadRequest("No services provided");
        }

        var processedCount = 0;
        var addedCount = 0;
        var updatedCount = 0;
        var errors = new List<string>();

        try
        {
            foreach (var service in services)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(service.Name))
                    {
                        service.Name = $"Service {processedCount + 1}";
                    }
                    service.Name = service.Name?.Trim();
                    
                    var now = DateTime.UtcNow;
                    if (service.Id.HasValue && service.Id.Value > 0)
                    {
                        var existingService = await ctx.Service.FindAsync(service.Id.Value);
                        if (existingService != null)
                        {
                            existingService.Name = service.Name;
                            existingService.Description = service.Description;
                            existingService.DetailedDescription = service.DetailedDescription;
                            existingService.HourlyRate = service.HourlyRate;
                            existingService.DailyRate = service.DailyRate;
                            existingService.ProjectRate = service.ProjectRate;
                            existingService.PricingType = service.PricingType;
                            existingService.ServiceCategoryId = service.ServiceCategoryId;
                            existingService.ServiceCompanyId = service.ServiceCompanyId;
                            existingService.ImageUrl = service.ImageUrl;
                            existingService.IsAvailable = service.IsAvailable;
                            existingService.EstimatedDurationHours = service.EstimatedDurationHours;
                            existingService.RequiresOnsite = service.RequiresOnsite;
                            existingService.IncludesTravel = service.IncludesTravel;
                            existingService.Requirements = service.Requirements;
                            existingService.Deliverables = service.Deliverables;
                            existingService.Complexity = service.Complexity;
                            existingService.SKU = service.SKU;
                            existingService.Notes = service.Notes;
                            existingService.ModifiedDate = now;
                            
                            updatedCount++;
                        }
                        else
                        {
                            errors.Add($"Service with ID {service.Id} not found for update");
                        }
                    }
                    else
                    {
                        service.Id = null;
                        service.CreatedDate = now;
                        service.ModifiedDate = now;
                        
                        ctx.Service.Add(service);
                        addedCount++;
                    }
                    
                    processedCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Error processing service {processedCount + 1}: {ex.Message}");
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

    private bool ServiceExists(long id)
    {
        return ctx.Service.Any(e => e.Id == id);
    }
}