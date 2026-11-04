using System.Net;
using System.Net.Http.Json;
using MiniDpp.Api.Controllers;

namespace MiniDpp.Tests;

public class QrCodeTests : IDisposable
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private readonly TestApp _app = new();

    public void Dispose() => _app.Dispose();

    [Theory]
    [InlineData("08690000000012")]
    [InlineData("08690000000029")]
    [InlineData("08690000000050")]
    public async Task Every_valid_product_has_a_png_qr_code(string gtin)
    {
        await _app.ImportSampleAsync();

        var response = await _app.CreateClient().GetAsync($"/dpp/{gtin}/qr.png");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType!.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(PngSignature, bytes[..8]);
    }

    [Fact]
    public async Task Unknown_product_has_no_qr_code()
    {
        var response = await _app.CreateClient().GetAsync("/dpp/08690000000012/qr.png");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Product_list_returns_only_saved_products_with_output_urls()
    {
        await _app.ImportSampleAsync();

        var products = await _app.CreateClient().GetFromJsonAsync<List<ProductSummary>>("/api/products");

        Assert.Equal(["08690000000012", "08690000000029", "08690000000050"], products!.Select(p => p.Gtin));
        Assert.Equal("/dpp/08690000000012", products![0].PassportUrl);
        Assert.Equal("/dpp/08690000000012.jsonld", products[0].JsonLdUrl);
        Assert.Equal("/dpp/08690000000012/qr.png", products[0].QrUrl);
    }
}
