using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MiniDpp.Api.Data;

namespace MiniDpp.Api.Passports;

public class PassportService(DppDbContext db)
{
    /// <summary>Yalnızca demo amaçlı görünürlük ayrımı; gerçek bir kimlik doğrulama değildir.</summary>
    public const string AuthorizedRole = "yetkili";

    public async Task<Passport?> GetAsync(string gtin, string? role, CancellationToken cancellationToken = default)
    {
        var model = await db.ProductModels
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Batches.OrderBy(b => b.Id))
            .Include(p => p.PreviousVersions.OrderByDescending(v => v.VersionNumber))
            .SingleOrDefaultAsync(p => p.Gtin == gtin, cancellationToken);
        if (model is null)
        {
            return null;
        }

        var showsCarbon = role == AuthorizedRole;
        return new Passport(
            model.Gtin,
            model.Name,
            Normalize(model.CottonPercent),
            Normalize(model.ElastanePercent),
            model.Batches
                .Select(b => ToPassportBatch(b.BatchNo, b.ProductionPlace, b.CarbonKgCo2e, showsCarbon))
                .ToList(),
            model.Version,
            model.UpdatedAt,
            model.PreviousVersions.Select(v => ToPassportVersion(v, showsCarbon)).ToList(),
            showsCarbon);
    }

    private static PassportVersion ToPassportVersion(ProductVersion version, bool showsCarbon)
    {
        var snapshot = JsonSerializer.Deserialize<ProductSnapshot>(version.SnapshotJson)!;
        return new PassportVersion(
            version.VersionNumber,
            version.UpdatedAt,
            snapshot.ModelName,
            Normalize(snapshot.CottonPercent),
            Normalize(snapshot.ElastanePercent),
            snapshot.Batches
                .Select(b => ToPassportBatch(b.BatchNo, b.ProductionPlace, b.CarbonKgCo2e, showsCarbon))
                .ToList());
    }

    private static PassportBatch ToPassportBatch(string batchNo, string place, decimal carbon, bool showsCarbon) =>
        new(batchNo, place, showsCarbon ? Normalize(carbon) : null);

    // SQLite decimal'i metin olarak saklar ve 98 değeri 98.0 diye geri döner.
    private static decimal Normalize(decimal value) => value / 1.000000000000000000000000000000000m;
}
