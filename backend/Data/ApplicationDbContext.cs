using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProductVault.Models;

namespace ProductVault.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.FirstName).HasMaxLength(100);
            entity.Property(x => x.Surname).HasMaxLength(100);
        });

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

        builder.Entity<AuditEvent>(entity =>
        {
            entity.HasIndex(x => new { x.OwnerId, x.OccurredAt });
            entity.Property(x => x.OccurredAt).HasPrecision(6);
        });

        builder.Entity<InventoryMovement>(entity =>
        {
            entity.HasIndex(x => new { x.OwnerId, x.ProductId, x.OccurredAt });
            entity.Property(x => x.OccurredAt).HasPrecision(6);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.ExpiresAt });
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
