using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using AppProduct.Data;
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
        var existingTaxRates = await ctx.TaxRate.AnyAsync();
        if (existingTaxRates)
            return Ok("Tax rates already exist");

        var taxRates = GetDefaultTaxRates();
        await ctx.TaxRate.AddRangeAsync(taxRates);
        await ctx.SaveChangesAsync();

        return Ok($"Seeded {taxRates.Count} tax rates");
    }

    private static List<TaxRate> GetDefaultTaxRates()
    {
        var now = DateTime.UtcNow;
        return new List<TaxRate>
        {
            new() { State = "Alabama", StateCode = "AL", StateTaxRate = 4.00m, LocalTaxRate = 5.22m, CombinedTaxRate = 9.22m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Alaska", StateCode = "AK", StateTaxRate = 0.00m, LocalTaxRate = 1.43m, CombinedTaxRate = 1.43m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Arizona", StateCode = "AZ", StateTaxRate = 5.60m, LocalTaxRate = 2.77m, CombinedTaxRate = 8.37m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Arkansas", StateCode = "AR", StateTaxRate = 6.50m, LocalTaxRate = 2.93m, CombinedTaxRate = 9.43m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "California", StateCode = "CA", StateTaxRate = 7.25m, LocalTaxRate = 3.33m, CombinedTaxRate = 10.58m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Colorado", StateCode = "CO", StateTaxRate = 2.90m, LocalTaxRate = 4.87m, CombinedTaxRate = 7.77m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Connecticut", StateCode = "CT", StateTaxRate = 6.35m, LocalTaxRate = 0.00m, CombinedTaxRate = 6.35m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Delaware", StateCode = "DE", StateTaxRate = 0.00m, LocalTaxRate = 0.00m, CombinedTaxRate = 0.00m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Florida", StateCode = "FL", StateTaxRate = 6.00m, LocalTaxRate = 1.05m, CombinedTaxRate = 7.05m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Georgia", StateCode = "GA", StateTaxRate = 4.00m, LocalTaxRate = 3.29m, CombinedTaxRate = 7.29m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Hawaii", StateCode = "HI", StateTaxRate = 4.17m, LocalTaxRate = 0.41m, CombinedTaxRate = 4.58m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Idaho", StateCode = "ID", StateTaxRate = 6.00m, LocalTaxRate = 0.03m, CombinedTaxRate = 6.03m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Illinois", StateCode = "IL", StateTaxRate = 6.25m, LocalTaxRate = 2.49m, CombinedTaxRate = 8.74m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Indiana", StateCode = "IN", StateTaxRate = 7.00m, LocalTaxRate = 0.00m, CombinedTaxRate = 7.00m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Iowa", StateCode = "IA", StateTaxRate = 6.00m, LocalTaxRate = 0.82m, CombinedTaxRate = 6.82m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Kansas", StateCode = "KS", StateTaxRate = 6.50m, LocalTaxRate = 2.17m, CombinedTaxRate = 8.67m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Kentucky", StateCode = "KY", StateTaxRate = 6.00m, LocalTaxRate = 0.00m, CombinedTaxRate = 6.00m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Louisiana", StateCode = "LA", StateTaxRate = 4.45m, LocalTaxRate = 5.00m, CombinedTaxRate = 9.45m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Maine", StateCode = "ME", StateTaxRate = 5.50m, LocalTaxRate = 0.00m, CombinedTaxRate = 5.50m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Maryland", StateCode = "MD", StateTaxRate = 6.00m, LocalTaxRate = 0.00m, CombinedTaxRate = 6.00m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Massachusetts", StateCode = "MA", StateTaxRate = 6.25m, LocalTaxRate = 0.00m, CombinedTaxRate = 6.25m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Michigan", StateCode = "MI", StateTaxRate = 6.00m, LocalTaxRate = 0.00m, CombinedTaxRate = 6.00m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Minnesota", StateCode = "MN", StateTaxRate = 6.88m, LocalTaxRate = 0.55m, CombinedTaxRate = 7.43m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Mississippi", StateCode = "MS", StateTaxRate = 7.00m, LocalTaxRate = 0.07m, CombinedTaxRate = 7.07m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Missouri", StateCode = "MO", StateTaxRate = 4.23m, LocalTaxRate = 3.90m, CombinedTaxRate = 8.13m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Montana", StateCode = "MT", StateTaxRate = 0.00m, LocalTaxRate = 0.00m, CombinedTaxRate = 0.00m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Nebraska", StateCode = "NE", StateTaxRate = 5.50m, LocalTaxRate = 1.35m, CombinedTaxRate = 6.85m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Nevada", StateCode = "NV", StateTaxRate = 4.60m, LocalTaxRate = 3.55m, CombinedTaxRate = 8.15m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "New Hampshire", StateCode = "NH", StateTaxRate = 0.00m, LocalTaxRate = 0.00m, CombinedTaxRate = 0.00m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "New Jersey", StateCode = "NJ", StateTaxRate = 6.63m, LocalTaxRate = -0.03m, CombinedTaxRate = 6.60m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "New Mexico", StateCode = "NM", StateTaxRate = 5.13m, LocalTaxRate = 2.69m, CombinedTaxRate = 7.82m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "New York", StateCode = "NY", StateTaxRate = 4.00m, LocalTaxRate = 4.49m, CombinedTaxRate = 8.49m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "North Carolina", StateCode = "NC", StateTaxRate = 4.75m, LocalTaxRate = 2.22m, CombinedTaxRate = 6.97m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "North Dakota", StateCode = "ND", StateTaxRate = 5.00m, LocalTaxRate = 1.85m, CombinedTaxRate = 6.85m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Ohio", StateCode = "OH", StateTaxRate = 5.75m, LocalTaxRate = 1.42m, CombinedTaxRate = 7.17m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Oklahoma", StateCode = "OK", StateTaxRate = 4.50m, LocalTaxRate = 4.42m, CombinedTaxRate = 8.92m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Oregon", StateCode = "OR", StateTaxRate = 0.00m, LocalTaxRate = 0.00m, CombinedTaxRate = 0.00m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Pennsylvania", StateCode = "PA", StateTaxRate = 6.00m, LocalTaxRate = 0.34m, CombinedTaxRate = 6.34m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Rhode Island", StateCode = "RI", StateTaxRate = 7.00m, LocalTaxRate = 0.00m, CombinedTaxRate = 7.00m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "South Carolina", StateCode = "SC", StateTaxRate = 6.00m, LocalTaxRate = 1.43m, CombinedTaxRate = 7.43m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "South Dakota", StateCode = "SD", StateTaxRate = 4.20m, LocalTaxRate = 1.90m, CombinedTaxRate = 6.10m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Tennessee", StateCode = "TN", StateTaxRate = 7.00m, LocalTaxRate = 2.55m, CombinedTaxRate = 9.55m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Texas", StateCode = "TX", StateTaxRate = 6.25m, LocalTaxRate = 1.94m, CombinedTaxRate = 8.19m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Utah", StateCode = "UT", StateTaxRate = 6.10m, LocalTaxRate = 0.99m, CombinedTaxRate = 7.09m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Vermont", StateCode = "VT", StateTaxRate = 6.00m, LocalTaxRate = 0.18m, CombinedTaxRate = 6.18m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Virginia", StateCode = "VA", StateTaxRate = 5.30m, LocalTaxRate = 0.00m, CombinedTaxRate = 5.30m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Washington", StateCode = "WA", StateTaxRate = 6.50m, LocalTaxRate = 3.05m, CombinedTaxRate = 9.55m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "West Virginia", StateCode = "WV", StateTaxRate = 6.00m, LocalTaxRate = 0.59m, CombinedTaxRate = 6.59m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Wisconsin", StateCode = "WI", StateTaxRate = 5.00m, LocalTaxRate = 0.44m, CombinedTaxRate = 5.44m, IsActive = true, CreatedDate = now, ModifiedDate = now },
            new() { State = "Wyoming", StateCode = "WY", StateTaxRate = 4.00m, LocalTaxRate = 1.29m, CombinedTaxRate = 5.29m, IsActive = true, CreatedDate = now, ModifiedDate = now }
        };
    }
}

