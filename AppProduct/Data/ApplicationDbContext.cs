using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AppProduct.Models;
using AppProduct.Shared.Models;

namespace AppProduct.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Product> Product => Set<Product>();
    public DbSet<Brand> Brand => Set<Brand>();
    public DbSet<Category> Category => Set<Category>();
    public DbSet<Feature> Feature => Set<Feature>();
    public DbSet<ProductReview> ProductReview => Set<ProductReview>();
    public DbSet<Tag> Tag => Set<Tag>();
    public DbSet<Notification> Notification => Set<Notification>();
    public DbSet<ShoppingCart> ShoppingCart => Set<ShoppingCart>();
    public DbSet<ShoppingCartItem> ShoppingCartItem => Set<ShoppingCartItem>();
    public DbSet<Order> Order => Set<Order>();
    public DbSet<OrderItem> OrderItem => Set<OrderItem>();
    public DbSet<TaxRate> TaxRate => Set<TaxRate>();
    public DbSet<Address> Address => Set<Address>();
    public DbSet<ShippingRate> ShippingRate => Set<ShippingRate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>()
            .Property(e => e.Price)
            .HasConversion<double>();
        modelBuilder.Entity<Product>()
            .Property(e => e.Price)
            .HasPrecision(19, 4);
        modelBuilder.Entity<Product>()
            .HasMany(x => x.Category);
        modelBuilder.Entity<Product>()
            .HasMany(x => x.Brand);
        modelBuilder.Entity<Product>()
            .HasMany(x => x.Feature);
        modelBuilder.Entity<Product>()
            .HasMany(x => x.Tag);
        modelBuilder.Entity<Product>()
            .HasMany(x => x.ProductReview);
        modelBuilder.Entity<Category>()
            .HasMany(x => x.Product);
        modelBuilder.Entity<Feature>()
            .HasMany(x => x.Product);
        modelBuilder.Entity<Tag>()
            .HasMany(x => x.Product);

        // Shopping Cart configuration
        modelBuilder.Entity<ShoppingCart>()
            .HasMany(x => x.Items)
            .WithOne(x => x.ShoppingCart)
            .HasForeignKey(x => x.ShoppingCartId);

        modelBuilder.Entity<ShoppingCartItem>()
            .Property(e => e.UnitPrice)
            .HasPrecision(19, 4);

        // Order configuration
        modelBuilder.Entity<Order>()
            .HasMany(x => x.Items)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId);

        modelBuilder.Entity<Order>()
            .Property(e => e.Subtotal)
            .HasPrecision(19, 4);

        modelBuilder.Entity<Order>()
            .Property(e => e.TaxAmount)
            .HasPrecision(19, 4);

        modelBuilder.Entity<Order>()
            .Property(e => e.ShippingAmount)
            .HasPrecision(19, 4);

        modelBuilder.Entity<Order>()
            .Property(e => e.TotalAmount)
            .HasPrecision(19, 4);

        modelBuilder.Entity<OrderItem>()
            .Property(e => e.UnitPrice)
            .HasPrecision(19, 4);

        modelBuilder.Entity<OrderItem>()
            .Property(e => e.TotalPrice)
            .HasPrecision(19, 4);

        modelBuilder.Entity<ShippingRate>()
            .Property(e => e.Amount)
            .HasPrecision(19, 4);

        // Tax Rate configuration
        modelBuilder.Entity<TaxRate>()
            .Property(e => e.StateTaxRate)
            .HasPrecision(10, 4);

        modelBuilder.Entity<TaxRate>()
            .Property(e => e.LocalTaxRate)
            .HasPrecision(10, 4);

        modelBuilder.Entity<TaxRate>()
            .Property(e => e.CombinedTaxRate)
            .HasPrecision(10, 4);

        // Product additional fields
        modelBuilder.Entity<Product>()
            .Property(e => e.Weight)
            .HasPrecision(10, 2);
    }
}
