using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using AppProduct.Data;
using AppProduct.Shared.Models;
using System.Security.Claims;

namespace AppProduct.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
[EnableRateLimiting("Fixed")]
public class AddressController(ApplicationDbContext ctx) : ControllerBase
{
    [HttpGet("")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<IQueryable<Address>> Get()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        return Ok(ctx.Address.Where(x => x.UserId == userId));
    }

    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Address>> GetAsync(long key)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var address = await ctx.Address.FirstOrDefaultAsync(x => x.Id == key && x.UserId == userId);
        if (address == null)
            return NotFound();

        return Ok(address);
    }

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Address>> PostAsync(Address address)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        address.UserId = userId;
        address.CreatedDate = DateTime.UtcNow;
        address.ModifiedDate = DateTime.UtcNow;

        // If this is set as default, remove default from other addresses
        if (address.IsDefault)
        {
            var existingDefault = await ctx.Address
                .Where(x => x.UserId == userId && x.Type == address.Type && x.IsDefault)
                .ToListAsync();
            
            foreach (var addr in existingDefault)
            {
                addr.IsDefault = false;
            }
        }

        ctx.Address.Add(address);
        await ctx.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAsync), new { key = address.Id }, address);
    }

    [HttpPut("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Address>> PutAsync(long key, Address update)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var address = await ctx.Address.FirstOrDefaultAsync(x => x.Id == key && x.UserId == userId);
        if (address == null)
            return NotFound();

        // If this is set as default, remove default from other addresses
        if (update.IsDefault && !address.IsDefault)
        {
            var existingDefault = await ctx.Address
                .Where(x => x.UserId == userId && x.Type == update.Type && x.IsDefault && x.Id != key)
                .ToListAsync();
            
            foreach (var addr in existingDefault)
            {
                addr.IsDefault = false;
            }
        }

        address.FirstName = update.FirstName;
        address.LastName = update.LastName;
        address.Company = update.Company;
        address.AddressLine1 = update.AddressLine1;
        address.AddressLine2 = update.AddressLine2;
        address.City = update.City;
        address.State = update.State;
        address.PostalCode = update.PostalCode;
        address.Country = update.Country;
        address.Phone = update.Phone;
        address.IsDefault = update.IsDefault;
        address.Type = update.Type;
        address.ModifiedDate = DateTime.UtcNow;

        await ctx.SaveChangesAsync();

        return Ok(address);
    }

    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(long key)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var address = await ctx.Address.FirstOrDefaultAsync(x => x.Id == key && x.UserId == userId);
        if (address == null)
            return NotFound();

        ctx.Address.Remove(address);
        await ctx.SaveChangesAsync();

        return NoContent();
    }
}