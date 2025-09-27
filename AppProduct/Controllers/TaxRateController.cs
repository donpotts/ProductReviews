using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using AppProduct.Data;
using AppProduct.Data.Seed;
using AppProduct.Shared.Models;

namespace AppProduct.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
[EnableRateLimiting("Fixed")]
public class TaxRateController(ApplicationDbContext ctx) : ControllerBase
{
    [HttpGet("")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<IQueryable<TaxRate>> Get()
    {
        return Ok(ctx.TaxRate);
    }

    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaxRate>> GetAsync(long key)
    {
        var taxRate = await ctx.TaxRate.FindAsync(key);
        if (taxRate == null)
            return NotFound();

        return Ok(taxRate);
    }

    [HttpGet("by-state/{stateCode}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaxRate>> GetByStateAsync(string stateCode)
    {
        var taxRate = await ctx.TaxRate.FirstOrDefaultAsync(x => x.StateCode == stateCode.ToUpper() && x.IsActive);
        if (taxRate == null)
            return NotFound();

        return Ok(taxRate);
    }

    [HttpPost("calculate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TaxCalculationResult>> CalculateAsync([FromBody] TaxCalculationRequest request)
    {
        if (request.Subtotal <= 0)
            return BadRequest("Subtotal must be greater than 0");

        var taxRate = await ctx.TaxRate.FirstOrDefaultAsync(x => x.StateCode == request.StateCode.ToUpper() && x.IsActive);
        
        var result = new TaxCalculationResult
        {
            Subtotal = request.Subtotal,
            StateCode = request.StateCode.ToUpper(),
            TaxRate = taxRate?.CombinedTaxRate ?? 0,
            TaxAmount = 0,
            Total = request.Subtotal
        };

        if (taxRate != null)
        {
            result.TaxAmount = request.Subtotal * taxRate.CombinedTaxRate / 100;
            result.Total = request.Subtotal + result.TaxAmount;
        }

        return Ok(result);
    }

    [HttpPost("seed")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> SeedTaxRatesAsync()
    {
        await TaxRateSeedData.EnsureSeedDataAsync(ctx);
        var activeCount = await ctx.TaxRate.CountAsync(x => x.IsActive);
        return Ok($"Seeded or refreshed {activeCount} tax rates");
    }
}

