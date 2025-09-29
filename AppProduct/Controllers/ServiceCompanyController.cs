using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
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
public class ServiceCompanyController(ApplicationDbContext ctx) : ControllerBase
{
    [HttpGet("")]
    [EnableQuery]
    public ActionResult<IQueryable<ServiceCompany>> Get()
    {
        return Ok(ctx.ServiceCompany.Include(x => x.Service));
    }

    [HttpGet("{key}")]
    [EnableQuery]
    public async Task<ActionResult<ServiceCompany>> Get(long key)
    {
        var company = await ctx.ServiceCompany
            .Include(x => x.Service)
            .FirstOrDefaultAsync(p => p.Id == key);

        if (company == null) return NotFound();
        return Ok(company);
    }

    [HttpPost("")]
    public async Task<ActionResult<ServiceCompany>> Post([FromBody] ServiceCompany company)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        company.CreatedDate = DateTime.UtcNow;
        company.ModifiedDate = DateTime.UtcNow;

        ctx.ServiceCompany.Add(company);
        await ctx.SaveChangesAsync();

        return Created($"/api/ServiceCompany/{company.Id}", company);
    }

    [HttpPatch("{key}")]
    public async Task<ActionResult<ServiceCompany>> Patch(long key, [FromBody] Delta<ServiceCompany> patch)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var company = await ctx.ServiceCompany.FindAsync(key);
        if (company == null) return NotFound();

        patch.Patch(company);
        company.ModifiedDate = DateTime.UtcNow;
        await ctx.SaveChangesAsync();

        return Ok(company);
    }

    [HttpPut("{key}")]
    public async Task<ActionResult<ServiceCompany>> Put(long key, [FromBody] ServiceCompany company)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (key != company.Id) return BadRequest();

        company.ModifiedDate = DateTime.UtcNow;
        ctx.Entry(company).State = EntityState.Modified;
        await ctx.SaveChangesAsync();

        return Ok(company);
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(long key)
    {
        var company = await ctx.ServiceCompany.FindAsync(key);
        if (company == null) return NotFound();

        ctx.ServiceCompany.Remove(company);
        await ctx.SaveChangesAsync();

        return NoContent();
    }
}