using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
public class ShoppingCartController(ApplicationDbContext ctx) : ControllerBase
{
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ShoppingCart>> GetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var cart = await ctx.ShoppingCart
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (cart == null)
        {
            cart = new ShoppingCart
            {
                UserId = userId,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Items = new List<ShoppingCartItem>()
            };
            ctx.ShoppingCart.Add(cart);
            await ctx.SaveChangesAsync();
        }

        return Ok(cart);
    }

    [HttpPost("add-item")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShoppingCart>> AddItemAsync([FromBody] AddToCartRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var product = await ctx.Product.FindAsync(request.ProductId);
        if (product == null)
            return NotFound("Product not found");

        if (!product.InStock)
            return BadRequest("Product is out of stock");

        var cart = await ctx.ShoppingCart
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (cart == null)
        {
            cart = new ShoppingCart
            {
                UserId = userId,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Items = new List<ShoppingCartItem>()
            };
            ctx.ShoppingCart.Add(cart);
        }

        var existingItem = cart.Items?.FirstOrDefault(x => x.ProductId == request.ProductId);
        if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
        }
        else
        {
            var newItem = new ShoppingCartItem
            {
                ShoppingCartId = cart.Id,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                UnitPrice = product.Price,
                AddedDate = DateTime.UtcNow
            };
            cart.Items ??= new List<ShoppingCartItem>();
            cart.Items.Add(newItem);
            ctx.ShoppingCartItem.Add(newItem);
        }

        cart.ModifiedDate = DateTime.UtcNow;
        await ctx.SaveChangesAsync();

        return await GetAsync();
    }

    [HttpPut("update-item/{itemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShoppingCart>> UpdateItemAsync(long itemId, [FromBody] UpdateCartItemRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var cart = await ctx.ShoppingCart
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (cart == null)
            return NotFound("Cart not found");

        var item = cart.Items?.FirstOrDefault(x => x.Id == itemId);
        if (item == null)
            return NotFound("Item not found in cart");

        if (request.Quantity <= 0)
        {
            cart.Items!.Remove(item);
            ctx.ShoppingCartItem.Remove(item);
        }
        else
        {
            item.Quantity = request.Quantity;
        }

        cart.ModifiedDate = DateTime.UtcNow;
        await ctx.SaveChangesAsync();

        return await GetAsync();
    }

    [HttpDelete("remove-item/{itemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShoppingCart>> RemoveItemAsync(long itemId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var cart = await ctx.ShoppingCart
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (cart == null)
            return NotFound("Cart not found");

        var item = cart.Items?.FirstOrDefault(x => x.Id == itemId);
        if (item == null)
            return NotFound("Item not found in cart");

        cart.Items!.Remove(item);
        ctx.ShoppingCartItem.Remove(item);

        cart.ModifiedDate = DateTime.UtcNow;
        await ctx.SaveChangesAsync();

        return await GetAsync();
    }

    [HttpDelete("clear")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ShoppingCart>> ClearAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var cart = await ctx.ShoppingCart
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (cart != null && cart.Items != null)
        {
            ctx.ShoppingCartItem.RemoveRange(cart.Items);
            cart.Items.Clear();
            cart.ModifiedDate = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }

        return await GetAsync();
    }
}

public class AddToCartRequest
{
    public long ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemRequest
{
    public int Quantity { get; set; }
}