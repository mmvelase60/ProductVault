using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProductVault.Models;

namespace ProductVault.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>(entity =>
        {
            entity.HasIndex(x => new { x.OwnerId, x.CategoryCode }).IsUnique();
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasMany(x => x.Products).WithOne(x => x.Category)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Product>(entity =>
        {
            entity.HasIndex(x => x.ProductCode).IsUnique();
            entity.HasIndex(x => new { x.OwnerId, x.CategoryId });
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.Property(x => x.RowVersion).IsRowVersion();
        });
    }
}
