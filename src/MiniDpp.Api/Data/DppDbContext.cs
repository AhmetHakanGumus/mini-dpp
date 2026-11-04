using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace MiniDpp.Api.Data;

public class DppDbContext(DbContextOptions<DppDbContext> options) : DbContext(options)
{
    public DbSet<ProductModel> ProductModels => Set<ProductModel>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<ProductVersion> ProductVersions => Set<ProductVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductModel>(entity =>
        {
            entity.Property(p => p.Gtin).HasMaxLength(14).IsUnicode(false);
            entity.Property(p => p.Version).HasDefaultValue(1);
            entity.Property(p => p.UpdatedAt).HasConversion(AsUtc, FromUtc);
            entity.HasIndex(p => p.Gtin).IsUnique();
            entity.HasMany(p => p.Batches)
                .WithOne(b => b.ProductModel)
                .HasForeignKey(b => b.ProductModelId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(p => p.PreviousVersions)
                .WithOne(v => v.ProductModel)
                .HasForeignKey(v => v.ProductModelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Batch>(entity =>
        {
            // Aynı parti numarası farklı modellerde kullanılabilir, aynı modelde tekrar edemez.
            entity.HasIndex(b => new { b.ProductModelId, b.BatchNo }).IsUnique();
        });

        modelBuilder.Entity<ProductVersion>(entity =>
        {
            entity.Property(v => v.UpdatedAt).HasConversion(AsUtc, FromUtc);
            entity.HasIndex(v => new { v.ProductModelId, v.VersionNumber }).IsUnique();
        });
    }

    // SQLite tarihin UTC olduğu bilgisini saklamaz; okurken geri eklenir.
    private static readonly Expression<Func<DateTime, DateTime>> AsUtc = value => value;

    private static readonly Expression<Func<DateTime, DateTime>> FromUtc =
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
