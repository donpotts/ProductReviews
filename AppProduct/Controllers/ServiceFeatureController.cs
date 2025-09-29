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
public class ServiceFeatureController(ApplicationDbContext ctx) : ControllerBase
{
    [HttpGet("")]
    [EnableQuery]
    public ActionResult<IQueryable<ServiceFeature>> Get()
    {
        return Ok(ctx.ServiceFeature.Include(x => x.Service));
    }

    [HttpGet("{key}")]
    [EnableQuery]
    public async Task<ActionResult<ServiceFeature>> Get(long key)
    {
        var feature = await ctx.ServiceFeature
            .Include(x => x.Service)
            .FirstOrDefaultAsync(p => p.Id == key);

        if (feature == null) return NotFound();
        return Ok(feature);
    }

    [HttpPost("")]
    public async Task<ActionResult<ServiceFeature>> Post([FromBody] ServiceFeature feature)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        feature.CreatedDate = DateTime.UtcNow;
        feature.ModifiedDate = DateTime.UtcNow;

        ctx.ServiceFeature.Add(feature);
        await ctx.SaveChangesAsync();

        return Created($"/api/ServiceFeature/{feature.Id}", feature);
    }

    [HttpPatch("{key}")]
    public async Task<ActionResult<ServiceFeature>> Patch(long key, [FromBody] Delta<ServiceFeature> patch)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var feature = await ctx.ServiceFeature.FindAsync(key);
        if (feature == null) return NotFound();

        patch.Patch(feature);
        feature.ModifiedDate = DateTime.UtcNow;
        await ctx.SaveChangesAsync();

        return Ok(feature);
    }

    [HttpPut("{key}")]
    public async Task<ActionResult<ServiceFeature>> Put(long key, [FromBody] ServiceFeature feature)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (key != feature.Id) return BadRequest();

        feature.ModifiedDate = DateTime.UtcNow;
        ctx.Entry(feature).State = EntityState.Modified;
        await ctx.SaveChangesAsync();

        return Ok(feature);
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(long key)
    {
        var feature = await ctx.ServiceFeature.FindAsync(key);
        if (feature == null) return NotFound();

        ctx.ServiceFeature.Remove(feature);
        await ctx.SaveChangesAsync();

        return NoContent();
    }
}