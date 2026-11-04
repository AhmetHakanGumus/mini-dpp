using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiniDpp.Api.Data;

public class DppDbContext(DbContextOptions<DppDbContext> options) : DbContext(options)
{
    public DbSet<ProductModel> ProductModels => Set<ProductModel>();
    public DbSet<Batch> Batches => Set<Batch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductModel>(entity =>
        {
            entity.Property(p => p.Gtin).HasMaxLength(14).IsUnicode(false);
            entity.HasIndex(p => p.Gtin).IsUnique();
            entity.HasMany(p => p.Batches)
                .WithOne(b => b.ProductModel)
                .HasForeignKey(b => b.ProductModelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Batch>(entity =>
        {
            // Aynı parti numarası farklı modellerde kullanılabilir, aynı modelde tekrar edemez.
            entity.HasIndex(b => new { b.ProductModelId, b.BatchNo }).IsUnique();
        });
    }
}
