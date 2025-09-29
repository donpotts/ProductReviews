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
public class ServiceTagController(ApplicationDbContext ctx) : ControllerBase
{
    [HttpGet("")]
    [EnableQuery]
    public ActionResult<IQueryable<ServiceTag>> Get()
    {
        return Ok(ctx.ServiceTag.Include(x => x.Service));
    }

    [HttpGet("{key}")]
    [EnableQuery]
    public async Task<ActionResult<ServiceTag>> Get(long key)
    {
        var tag = await ctx.ServiceTag
            .Include(x => x.Service)
            .FirstOrDefaultAsync(p => p.Id == key);

        if (tag == null) return NotFound();
        return Ok(tag);
    }

    [HttpPost("")]
    public async Task<ActionResult<ServiceTag>> Post([FromBody] ServiceTag tag)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        tag.CreatedDate = DateTime.UtcNow;
        tag.ModifiedDate = DateTime.UtcNow;

        ctx.ServiceTag.Add(tag);
        await ctx.SaveChangesAsync();

        return Created($"/api/ServiceTag/{tag.Id}", tag);
    }

    [HttpPatch("{key}")]
    public async Task<ActionResult<ServiceTag>> Patch(long key, [FromBody] Delta<ServiceTag> patch)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var tag = await ctx.ServiceTag.FindAsync(key);
        if (tag == null) return NotFound();

        patch.Patch(tag);
        tag.ModifiedDate = DateTime.UtcNow;
        await ctx.SaveChangesAsync();

        return Ok(tag);
    }

    [HttpPut("{key}")]
    public async Task<ActionResult<ServiceTag>> Put(long key, [FromBody] ServiceTag tag)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (key != tag.Id) return BadRequest();

        tag.ModifiedDate = DateTime.UtcNow;
        ctx.Entry(tag).State = EntityState.Modified;
        await ctx.SaveChangesAsync();

        return Ok(tag);
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(long key)
    {
        var tag = await ctx.ServiceTag.FindAsync(key);
        if (tag == null) return NotFound();

        ctx.ServiceTag.Remove(tag);
        await ctx.SaveChangesAsync();

        return NoContent();
    }
}