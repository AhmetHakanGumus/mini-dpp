using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniDpp.Api.Data;

namespace MiniDpp.Api.Controllers;

public sealed record ProductSummary(
    string Gtin,
    string ModelName,
    int BatchCount,
    string PassportUrl,
    string JsonLdUrl,
    string QrUrl);

[ApiController]
[Route("api/products")]
public class ProductsController(DppDbContext db) : ControllerBase
{
    /// <summary>Kayıtlı ürünleri ve çıktı adreslerini listeler. Karbon değeri içermez.</summary>
    [HttpGet]
    public async Task<IReadOnlyList<ProductSummary>> List(CancellationToken cancellationToken) =>
        await db.ProductModels
            .AsNoTracking()
            .OrderBy(p => p.Gtin)
            .Select(p => new ProductSummary(
                p.Gtin,
                p.Name,
                p.Batches.Count,
                "/dpp/" + p.Gtin,
                "/dpp/" + p.Gtin + ".jsonld",
                "/dpp/" + p.Gtin + "/qr.png"))
            .ToListAsync(cancellationToken);
}
