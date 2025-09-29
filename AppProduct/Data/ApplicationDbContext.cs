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
    
    // Service-related entities
    public DbSet<Service> Service => Set<Service>();
    public DbSet<ServiceCategory> ServiceCategory => Set<ServiceCategory>();
    public DbSet<ServiceCompany> ServiceCompany => Set<ServiceCompany>();
    public DbSet<ServiceFeature> ServiceFeature => Set<ServiceFeature>();
    public DbSet<ServiceTag> ServiceTag => Set<ServiceTag>();
    public DbSet<ServiceReview> ServiceReview => Set<ServiceReview>();
    public DbSet<ServiceCart> ServiceCart => Set<ServiceCart>();
    public DbSet<ServiceCartItem> ServiceCartItem => Set<ServiceCartItem>();
    public DbSet<ServiceOrder> ServiceOrder => Set<ServiceOrder>();
    public DbSet<ServiceOrderItem> ServiceOrderItem => Set<ServiceOrderItem>();
    public DbSet<ServiceExpense> ServiceExpense => Set<ServiceExpense>();

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

        // Service configuration
        modelBuilder.Entity<Service>()
            .Property(e => e.HourlyRate)
            .HasPrecision(19, 4);
        modelBuilder.Entity<Service>()
            .Property(e => e.DailyRate)
            .HasPrecision(19, 4);
        modelBuilder.Entity<Service>()
            .Property(e => e.ProjectRate)
            .HasPrecision(19, 4);
        modelBuilder.Entity<Service>()
            .HasMany(x => x.ServiceCategory);
        modelBuilder.Entity<Service>()
            .HasMany(x => x.ServiceCompany);
        modelBuilder.Entity<Service>()
            .HasMany(x => x.ServiceFeature);
        modelBuilder.Entity<Service>()
            .HasMany(x => x.ServiceTag);
        modelBuilder.Entity<Service>()
            .HasMany(x => x.ServiceReview);

        // Service relationships
        modelBuilder.Entity<ServiceCategory>()
            .HasMany(x => x.Service);
        modelBuilder.Entity<ServiceCompany>()
            .HasMany(x => x.Service);
        modelBuilder.Entity<ServiceFeature>()
            .HasMany(x => x.Service);
        modelBuilder.Entity<ServiceTag>()
            .HasMany(x => x.Service);

        // Service Cart configuration
        modelBuilder.Entity<ServiceCart>()
            .HasMany(x => x.Items)
            .WithOne(x => x.ServiceCart)
            .HasForeignKey(x => x.ServiceCartId);

        modelBuilder.Entity<ServiceCartItem>()
            .Property(e => e.UnitPrice)
            .HasPrecision(19, 4);
        modelBuilder.Entity<ServiceCartItem>()
            .Property(e => e.EstimatedHours)
            .HasPrecision(10, 2);

        // Service Order configuration
        modelBuilder.Entity<ServiceOrder>()
            .HasMany(x => x.Items)
            .WithOne(x => x.ServiceOrder)
            .HasForeignKey(x => x.ServiceOrderId);
        modelBuilder.Entity<ServiceOrder>()
            .HasMany(x => x.Expenses)
            .WithOne(x => x.ServiceOrder)
            .HasForeignKey(x => x.ServiceOrderId);

        modelBuilder.Entity<ServiceOrder>()
            .Property(e => e.Subtotal)
            .HasPrecision(19, 4);
        modelBuilder.Entity<ServiceOrder>()
            .Property(e => e.TaxAmount)
            .HasPrecision(19, 4);
        modelBuilder.Entity<ServiceOrder>()
            .Property(e => e.ExpenseAmount)
            .HasPrecision(19, 4);
        modelBuilder.Entity<ServiceOrder>()
            .Property(e => e.TotalAmount)
            .HasPrecision(19, 4);

        // Service Order Item configuration
        modelBuilder.Entity<ServiceOrderItem>()
            .Property(e => e.HoursEstimated)
            .HasPrecision(10, 2);
        modelBuilder.Entity<ServiceOrderItem>()
            .Property(e => e.HoursActual)
            .HasPrecision(10, 2);
        modelBuilder.Entity<ServiceOrderItem>()
            .Property(e => e.UnitPrice)
            .HasPrecision(19, 4);
        modelBuilder.Entity<ServiceOrderItem>()
            .Property(e => e.TotalPrice)
            .HasPrecision(19, 4);

        // Service Expense configuration
        modelBuilder.Entity<ServiceExpense>()
            .Property(e => e.Amount)
            .HasPrecision(19, 4);

        // Service Review configuration  
        modelBuilder.Entity<ServiceReview>()
            .Property(e => e.Rating)
            .HasPrecision(3, 2);
        modelBuilder.Entity<ServiceReview>()
            .Property(e => e.QualityRating)
            .HasPrecision(3, 2);
        modelBuilder.Entity<ServiceReview>()
            .Property(e => e.TimelinessRating)
            .HasPrecision(3, 2);
        modelBuilder.Entity<ServiceReview>()
            .Property(e => e.CommunicationRating)
            .HasPrecision(3, 2);
        modelBuilder.Entity<ServiceReview>()
            .Property(e => e.ValueRating)
            .HasPrecision(3, 2);

        // Service Company configuration
        modelBuilder.Entity<ServiceCompany>()
            .Property(e => e.AverageRating)
            .HasPrecision(3, 2);
    }
}
