using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using AppProduct.Data;
using AppProduct.Shared.Models;

namespace AppProduct.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
[EnableRateLimiting("Fixed")]
public class ProductController(ApplicationDbContext ctx) : ControllerBase
{
    [HttpGet("")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<IQueryable<Product>> Get()
    {
        return Ok(ctx.Product.Include(x => x.Category).Include(x => x.Brand).Include(x => x.Feature).Include(x => x.Tag).Include(x => x.ProductReview));
    }

    [HttpGet("{key}")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> GetAsync(long key)
    {
        var product = await ctx.Product.Include(x => x.Category).Include(x => x.Brand).Include(x => x.Feature).Include(x => x.Tag).Include(x => x.ProductReview).FirstOrDefaultAsync(x => x.Id == key);

        if (product == null)
        {
            return NotFound();
        }
        else
        {
            return Ok(product);
        }
    }

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Product>> PostAsync(Product product)
    {
        var record = await ctx.Product.FindAsync(product.Id);
        if (record != null)
        {
            return Conflict();
        }
    
        var category = product.Category;
        product.Category = null;

        var brand = product.Brand;
        product.Brand = null;

        var feature = product.Feature;
        product.Feature = null;

        var tag = product.Tag;
        product.Tag = null;

        var productReview = product.ProductReview;
        product.ProductReview = null;

        await ctx.Product.AddAsync(product);

        if (category != null)
        {
            var newValues = await ctx.Category.Where(x => category.Select(y => y.Id).Contains(x.Id)).ToListAsync();
            product.Category = [..newValues];
        }

        if (brand != null)
        {
            var newValues = await ctx.Brand.Where(x => brand.Select(y => y.Id).Contains(x.Id)).ToListAsync();
            product.Brand = [..newValues];
        }

        if (feature != null)
        {
            var newValues = await ctx.Feature.Where(x => feature.Select(y => y.Id).Contains(x.Id)).ToListAsync();
            product.Feature = [..newValues];
        }

        if (tag != null)
        {
            var newValues = await ctx.Tag.Where(x => tag.Select(y => y.Id).Contains(x.Id)).ToListAsync();
            product.Tag = [..newValues];
        }

        if (productReview != null)
        {
            var newValues = await ctx.ProductReview.Where(x => productReview.Select(y => y.Id).Contains(x.Id)).ToListAsync();
            product.ProductReview = [..newValues];
        }

        await ctx.SaveChangesAsync();

        return Created($"/product/{product.Id}", product);
    }

    [HttpPut("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> PutAsync(long key, Product update)
    {
        var product = await ctx.Product.Include(x => x.Category).Include(x => x.Brand).Include(x => x.Feature).Include(x => x.Tag).Include(x => x.ProductReview).FirstOrDefaultAsync(x => x.Id == key);

        if (product == null)
        {
            return NotFound();
        }

        ctx.Entry(product).CurrentValues.SetValues(update);

        if (update.Category != null)
        {
            var updateValues = update.Category.Select(x => x.Id);
            product.Category ??= [];
            product.Category.RemoveAll(x => !updateValues.Contains(x.Id));
            var addValues = updateValues.Where(x => !product.Category.Select(y => y.Id).Contains(x));
            var newValues = await ctx.Category.Where(x => addValues.Contains(x.Id)).ToListAsync();
            product.Category.AddRange(newValues);
        }

        if (update.Brand != null)
        {
            var updateValues = update.Brand.Select(x => x.Id);
            product.Brand ??= [];
            product.Brand.RemoveAll(x => !updateValues.Contains(x.Id));
            var addValues = updateValues.Where(x => !product.Brand.Select(y => y.Id).Contains(x));
            var newValues = await ctx.Brand.Where(x => addValues.Contains(x.Id)).ToListAsync();
            product.Brand.AddRange(newValues);
        }

        if (update.Feature != null)
        {
            var updateValues = update.Feature.Select(x => x.Id);
            product.Feature ??= [];
            product.Feature.RemoveAll(x => !updateValues.Contains(x.Id));
            var addValues = updateValues.Where(x => !product.Feature.Select(y => y.Id).Contains(x));
            var newValues = await ctx.Feature.Where(x => addValues.Contains(x.Id)).ToListAsync();
            product.Feature.AddRange(newValues);
        }

        if (update.Tag != null)
        {
            var updateValues = update.Tag.Select(x => x.Id);
            product.Tag ??= [];
            product.Tag.RemoveAll(x => !updateValues.Contains(x.Id));
            var addValues = updateValues.Where(x => !product.Tag.Select(y => y.Id).Contains(x));
            var newValues = await ctx.Tag.Where(x => addValues.Contains(x.Id)).ToListAsync();
            product.Tag.AddRange(newValues);
        }

        if (update.ProductReview != null)
        {
            var updateValues = update.ProductReview.Select(x => x.Id);
            product.ProductReview ??= [];
            product.ProductReview.RemoveAll(x => !updateValues.Contains(x.Id));
            var addValues = updateValues.Where(x => !product.ProductReview.Select(y => y.Id).Contains(x));
            var newValues = await ctx.ProductReview.Where(x => addValues.Contains(x.Id)).ToListAsync();
            product.ProductReview.AddRange(newValues);
        }

        await ctx.SaveChangesAsync();

        return Ok(product);
    }

    [HttpPatch("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> PatchAsync(long key, Delta<Product> delta)
    {
        var product = await ctx.Product.Include(x => x.Category).Include(x => x.Brand).Include(x => x.Feature).Include(x => x.Tag).Include(x => x.ProductReview).FirstOrDefaultAsync(x => x.Id == key);

        if (product == null)
        {
            return NotFound();
        }

        delta.Patch(product);

        await ctx.SaveChangesAsync();

        return Ok(product);
    }

    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(long key)
    {
        var product = await ctx.Product.FindAsync(key);

        if (product != null)
        {
            ctx.Product.Remove(product);
            await ctx.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpGet("search")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<IQueryable<Product>> Search([FromQuery] string? query, [FromQuery] long? categoryId, [FromQuery] long? brandId, 
        [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] bool? inStock)
    {
        var products = ctx.Product
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.Feature)
            .Include(x => x.Tag)
            .Include(x => x.ProductReview)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var searchTerm = query.ToLower();
            products = products.Where(p => 
                (p.Name != null && p.Name.ToLower().Contains(searchTerm)) ||
                (p.Description != null && p.Description.ToLower().Contains(searchTerm)) ||
                (p.DetailedSpecs != null && p.DetailedSpecs.ToLower().Contains(searchTerm)) ||
                (p.SKU != null && p.SKU.ToLower().Contains(searchTerm))
            );
        }

        if (categoryId.HasValue)
        {
            products = products.Where(p => p.Category != null && p.Category.Any(c => c.Id == categoryId.Value));
        }

        if (brandId.HasValue)
        {
            products = products.Where(p => p.Brand != null && p.Brand.Any(b => b.Id == brandId.Value));
        }

        if (minPrice.HasValue)
        {
            products = products.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            products = products.Where(p => p.Price <= maxPrice.Value);
        }

        if (inStock.HasValue)
        {
            products = products.Where(p => p.InStock == inStock.Value);
        }

        return Ok(products);
    }

    [HttpPost("bulk-upsert")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<object>> BulkUpsertAsync(List<Product> products)
    {
        if (products == null || !products.Any())
        {
            return BadRequest("No products provided");
        }

        var processedCount = 0;
        var addedCount = 0;
        var updatedCount = 0;
        var errors = new List<string>();

        try
        {
            foreach (var product in products)
            {
                try
                {
                    // Set defaults for required fields if missing
                    if (string.IsNullOrWhiteSpace(product.Name))
                    {
                        product.Name = $"Product {processedCount + 1}";
                    }
                    
                    // Set timestamps
                    var now = DateTime.UtcNow;
                    if (product.Id.HasValue && product.Id.Value > 0)
                    {
                        // Update existing product
                        var existingProduct = await ctx.Product.FindAsync(product.Id.Value);
                        if (existingProduct != null)
                        {
                            existingProduct.Name = product.Name;
                            existingProduct.Description = product.Description;
                            existingProduct.DetailedSpecs = product.DetailedSpecs;
                            existingProduct.Price = product.Price ?? 0;
                            existingProduct.ImageUrl = product.ImageUrl;
                            existingProduct.InStock = product.InStock;
                            existingProduct.ReleaseDate = product.ReleaseDate;
                            existingProduct.Notes = product.Notes;
                            existingProduct.UserId = product.UserId;
                            existingProduct.ModifiedDate = now;
                            
                            ctx.Product.Update(existingProduct);
                            updatedCount++;
                        }
                        else
                        {
                            // ID provided but product doesn't exist, create new one
                            product.CreatedDate = now;
                            product.ModifiedDate = now;
                            if (!product.Price.HasValue) product.Price = 0;
                            await ctx.Product.AddAsync(product);
                            addedCount++;
                        }
                    }
                    else
                    {
                        // Check if product with same name already exists
                        var existingByName = await ctx.Product
                            .FirstOrDefaultAsync(c => c.Name == product.Name && !string.IsNullOrEmpty(product.Name));
                        
                        if (existingByName != null)
                        {
                            // Update existing product by name
                            existingByName.Description = product.Description;
                            existingByName.DetailedSpecs = product.DetailedSpecs;
                            existingByName.Price = product.Price ?? existingByName.Price;
                            existingByName.ImageUrl = product.ImageUrl;
                            existingByName.InStock = product.InStock;
                            existingByName.ReleaseDate = product.ReleaseDate;
                            existingByName.Notes = product.Notes;
                            existingByName.UserId = product.UserId;
                            existingByName.ModifiedDate = now;
                            
                            ctx.Product.Update(existingByName);
                            updatedCount++;
                        }
                        else
                        {
                            // Create new product
                            product.CreatedDate = now;
                            product.ModifiedDate = now;
                            if (!product.Price.HasValue) product.Price = 0;
                            await ctx.Product.AddAsync(product);
                            addedCount++;
                        }
                    }
                    
                    processedCount++;
                }
                catch (Exception ex)
                {
                    var productName = product?.Name ?? "Unknown";
                    errors.Add($"Row {processedCount + 1} (Product: {productName}): {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        errors.Add($"  Inner error: {ex.InnerException.Message}");
                    }
                }
            }

            await ctx.SaveChangesAsync();

            return Ok(new 
            { 
                ProcessedCount = processedCount,
                AddedCount = addedCount,
                UpdatedCount = updatedCount,
                Errors = errors,
                Success = true
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new 
            { 
                ProcessedCount = processedCount,
                AddedCount = addedCount,
                UpdatedCount = updatedCount,
                Errors = new[] { ex.Message },
                Success = false
            });
        }
    }
}
