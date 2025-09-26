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
    }
}
