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
public class ServiceExpenseController(ApplicationDbContext ctx) : ControllerBase
{
    [HttpGet("")]
    [EnableQuery]
    public ActionResult<IQueryable<ServiceExpense>> Get()
    {
        return Ok(ctx.ServiceExpense.Include(x => x.ServiceOrder));
    }

    [HttpGet("{key}")]
    [EnableQuery]
    public async Task<ActionResult<ServiceExpense>> Get(long key)
    {
        var expense = await ctx.ServiceExpense
            .Include(x => x.ServiceOrder)
            .FirstOrDefaultAsync(p => p.Id == key);

        if (expense == null) return NotFound();
        return Ok(expense);
    }

    [HttpPost("")]
    public async Task<ActionResult<ServiceExpense>> Post([FromBody] ServiceExpense expense)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        expense.CreatedDate = DateTime.UtcNow;
        expense.ModifiedDate = DateTime.UtcNow;
        expense.ExpenseDate = expense.ExpenseDate == default ? DateTime.UtcNow : expense.ExpenseDate;

        ctx.ServiceExpense.Add(expense);
        await ctx.SaveChangesAsync();

        return Created($"/api/ServiceExpense/{expense.Id}", expense);
    }

    [HttpPatch("{key}")]
    public async Task<ActionResult<ServiceExpense>> Patch(long key, [FromBody] Delta<ServiceExpense> patch)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var expense = await ctx.ServiceExpense.FindAsync(key);
        if (expense == null) return NotFound();

        patch.Patch(expense);
        expense.ModifiedDate = DateTime.UtcNow;
        await ctx.SaveChangesAsync();

        return Ok(expense);
    }

    [HttpPut("{key}")]
    public async Task<ActionResult<ServiceExpense>> Put(long key, [FromBody] ServiceExpense expense)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (key != expense.Id) return BadRequest();

        expense.ModifiedDate = DateTime.UtcNow;
        ctx.Entry(expense).State = EntityState.Modified;
        await ctx.SaveChangesAsync();

        return Ok(expense);
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(long key)
    {
        var expense = await ctx.ServiceExpense.FindAsync(key);
        if (expense == null) return NotFound();

        ctx.ServiceExpense.Remove(expense);
        await ctx.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{key}/approve")]
    public async Task<ActionResult<ServiceExpense>> ApproveExpense(long key, [FromBody] string approverName)
    {
        var expense = await ctx.ServiceExpense.FindAsync(key);
        if (expense == null) return NotFound();

        expense.ApprovalStatus = ServiceExpenseStatus.Approved;
        expense.ApprovedDate = DateTime.UtcNow;
        expense.ApprovedBy = approverName;
        expense.ModifiedDate = DateTime.UtcNow;

        await ctx.SaveChangesAsync();
        return Ok(expense);
    }
}