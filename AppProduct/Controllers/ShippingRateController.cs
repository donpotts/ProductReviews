using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppProduct.Data;
using AppProduct.Shared.Models;

namespace AppProduct.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ShippingRateController(ApplicationDbContext ctx) : ControllerBase
{
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ShippingRate>>> GetAllAsync()
    {
        var rates = await ctx.ShippingRate
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .ToListAsync();

        return Ok(rates);
    }

    [HttpGet("default")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShippingRate>> GetDefaultAsync()
    {
        var rate = await ctx.ShippingRate
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedDate)
            .FirstOrDefaultAsync();

        if (rate == null)
            return NotFound();

        return Ok(rate);
    }

    [HttpPut("default")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ShippingRate>> UpdateDefaultAsync([FromBody] UpdateShippingRateRequest request)
    {
        if (request.Amount < 0)
            return BadRequest("Shipping amount cannot be negative");

        var rate = await ctx.ShippingRate
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedDate)
            .FirstOrDefaultAsync();

        if (rate == null)
        {
            rate = new ShippingRate
            {
                Name = string.IsNullOrWhiteSpace(request.Name) ? "Standard Shipping" : request.Name!,
                Amount = request.Amount,
                IsDefault = true,
                IsActive = request.IsActive ?? true,
                Notes = request.Notes,
                CreatedDate = DateTime.UtcNow
            };

            ctx.ShippingRate.Add(rate);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.Name))
                rate.Name = request.Name!;

            rate.Amount = request.Amount;
            rate.IsActive = request.IsActive ?? rate.IsActive;
            rate.Notes = request.Notes;
            rate.ModifiedDate = DateTime.UtcNow;
        }

        await ctx.SaveChangesAsync();

        return Ok(rate);
    }
}

public class UpdateShippingRateRequest
{
    public string? Name { get; set; }
    public decimal Amount { get; set; }
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }
}
