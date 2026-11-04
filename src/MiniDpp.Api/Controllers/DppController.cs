using Microsoft.AspNetCore.Mvc;
using MiniDpp.Api.Passports;

namespace MiniDpp.Api.Controllers;

public sealed record PassportPage(Passport Passport, string JsonLd, string JsonLdUrl);

[ApiExplorerSettings(IgnoreApi = true)]
public class DppController(PassportService passports, IConfiguration configuration) : Controller
{
    [HttpGet("/dpp/{gtin}")]
    public async Task<IActionResult> Page(string gtin, [FromQuery] string? rol, CancellationToken cancellationToken)
    {
        var passport = await passports.GetAsync(gtin, rol, cancellationToken);
        if (passport is null)
        {
            return NotFound("Bu GTIN için kayıtlı ürün pasaportu bulunamadı.");
        }

        var jsonLdUrl = $"/dpp/{gtin}.jsonld" + (passport.ShowsCarbon ? $"?rol={PassportService.AuthorizedRole}" : "");
        var jsonLd = PassportJsonLd.Serialize(passport, PageUrl(gtin));
        return View("Passport", new PassportPage(passport, jsonLd, jsonLdUrl));
    }

    [HttpGet("/dpp/{gtin}.jsonld")]
    public async Task<IActionResult> JsonLd(string gtin, [FromQuery] string? rol, CancellationToken cancellationToken)
    {
        var passport = await passports.GetAsync(gtin, rol, cancellationToken);
        if (passport is null)
        {
            return NotFound();
        }

        return Content(PassportJsonLd.Serialize(passport, PageUrl(gtin)), PassportJsonLd.ContentType);
    }

    /// <summary>QR'ın telefondan açılabilmesi için dışarıdan erişilen adres PublicBaseUrl ile verilebilir.</summary>
    private string PageUrl(string gtin)
    {
        var baseUrl = configuration["PublicBaseUrl"]?.TrimEnd('/') ?? $"{Request.Scheme}://{Request.Host}";
        return $"{baseUrl}/dpp/{gtin}";
    }
}
