using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using AppProduct.Data;
using AppProduct.Shared.Models;
using AppProduct.Services;

namespace AppProduct.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
[EnableRateLimiting("Fixed")]
public class ServiceOrderController(ApplicationDbContext ctx, IServiceScopeFactory scopeFactory, ILogger<ServiceOrderController> logger) : ControllerBase
{
    [HttpGet("")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<IQueryable<ServiceOrder>> Get()
    {
        return Ok(ctx.ServiceOrder
            .Include(x => x.Items)
                .ThenInclude(x => x.Service)
            .Include(x => x.Expenses));
    }

    [HttpGet("{key}")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceOrder>> Get(long key)
    {
        var serviceOrder = await ctx.ServiceOrder
            .Include(x => x.Items)
                .ThenInclude(x => x.Service)
            .Include(x => x.Expenses)
            .FirstOrDefaultAsync(p => p.Id == key);

        if (serviceOrder == null)
        {
            return NotFound();
        }

        return Ok(serviceOrder);
    }

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ServiceOrder>> Post([FromBody] ServiceOrder serviceOrder)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        serviceOrder.OrderDate = DateTime.UtcNow;

        // Ensure we don't try to insert related entities that already exist
        foreach (var item in serviceOrder.Items)
        {
            if (item.ServiceId.HasValue)
            {
                // Clear the Service navigation property to avoid EF trying to insert it
                item.Service = null;
            }
        }

        ctx.ServiceOrder.Add(serviceOrder);
        await ctx.SaveChangesAsync();

        // Send email notification
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var scopedCtx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var scopedEmailService = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();
                
                var fullOrder = await scopedCtx.ServiceOrder
                    .Include(s => s.Items)
                        .ThenInclude(i => i.Service)
                    .Include(s => s.Expenses)
                    .FirstOrDefaultAsync(s => s.Id == serviceOrder.Id);
                
                if (fullOrder != null)
                {
                    var customerEmail = fullOrder.ContactEmail ?? "customer@example.com";
                    await scopedEmailService.SendServiceOrderConfirmationAsync(fullOrder, customerEmail);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed sending service order confirmation email for order {OrderId}", serviceOrder.Id);
            }
        });

        return Created($"/api/ServiceOrder/{serviceOrder.Id}", serviceOrder);
    }

    [HttpPatch("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceOrder>> Patch(long key, [FromBody] Delta<ServiceOrder> patch)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var serviceOrder = await ctx.ServiceOrder.FindAsync(key);
        if (serviceOrder == null)
        {
            return NotFound();
        }

        var oldStatus = serviceOrder.Status;
        patch.Patch(serviceOrder);

        try
        {
            await ctx.SaveChangesAsync();
            
            // Send status update email if status changed
            if (oldStatus != serviceOrder.Status)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = scopeFactory.CreateScope();
                        var scopedEmailService = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();
                        
                        var customerEmail = serviceOrder.ContactEmail ?? "customer@example.com";
                        await scopedEmailService.SendServiceOrderStatusUpdateAsync(serviceOrder, customerEmail, oldStatus);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed sending service order status update email for order {OrderId}", serviceOrder.Id);
                    }
                });
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ServiceOrderExists(key))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return Ok(serviceOrder);
    }

    [HttpPut("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceOrder>> Put(long key, [FromBody] ServiceOrder serviceOrder)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (key != serviceOrder.Id)
        {
            return BadRequest();
        }
        ctx.Entry(serviceOrder).State = EntityState.Modified;

        try
        {
            await ctx.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ServiceOrderExists(key))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return Ok(serviceOrder);
    }

    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long key)
    {
        var serviceOrder = await ctx.ServiceOrder.FindAsync(key);
        if (serviceOrder == null)
        {
            return NotFound();
        }

        ctx.ServiceOrder.Remove(serviceOrder);
        await ctx.SaveChangesAsync();

        return NoContent();
    }

    private bool ServiceOrderExists(long id)
    {
        return ctx.ServiceOrder.Any(e => e.Id == id);
    }
}