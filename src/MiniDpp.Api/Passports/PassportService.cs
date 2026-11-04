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
            .Include(p => p.Batches.OrderBy(b => b.Id))
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
                .Select(b => new PassportBatch(
                    b.BatchNo,
                    b.ProductionPlace,
                    showsCarbon ? Normalize(b.CarbonKgCo2e) : null))
                .ToList(),
            showsCarbon);
    }

    // SQLite decimal'i metin olarak saklar ve 98 değeri 98.0 diye geri döner.
    private static decimal Normalize(decimal value) => value / 1.000000000000000000000000000000000m;
}
