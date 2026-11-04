using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MiniDpp.Tests;

public class PassportTests : IDisposable
{
    private const string Gtin = "08690000000012";
    private readonly TestApp _app = new();

    public void Dispose() => _app.Dispose();

    private async Task<string> GetStringAsync(string url)
    {
        var response = await _app.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    private static IEnumerable<JsonElement> Properties(JsonElement product) =>
        product.GetProperty("additionalProperty").EnumerateArray();

    private static JsonElement Property(JsonElement product, string id) =>
        Properties(product).Single(p => p.GetProperty("propertyID").GetString() == id);

    [Fact]
    public async Task Html_page_shows_basic_product_information()
    {
        await _app.ImportSampleAsync();

        var response = await _app.CreateClient().GetAsync($"/dpp/{Gtin}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);
        Assert.Contains("Slim Denim 501", html);
        Assert.Contains(Gtin, html);
        Assert.Contains("%98", html);
        Assert.Contains("%2", html);
        Assert.Contains("B2027-01", html);
        Assert.Contains("Denizli", html);
    }

    [Fact]
    public async Task JsonLd_uses_schema_org_product_with_same_record()
    {
        await _app.ImportSampleAsync();

        var response = await _app.CreateClient().GetAsync($"/dpp/{Gtin}.jsonld");
        var json = await response.Content.ReadAsStringAsync();
        var product = JsonDocument.Parse(json).RootElement;

        Assert.Equal("application/ld+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("https://schema.org", product.GetProperty("@context").GetString());
        Assert.Equal("Product", product.GetProperty("@type").GetString());
        Assert.Equal("Slim Denim 501", product.GetProperty("name").GetString());
        Assert.Equal(Gtin, product.GetProperty("gtin14").GetString());
        Assert.Equal(98m, Property(product, "elyaf_pamuk_yuzde").GetProperty("value").GetDecimal());
        Assert.Equal("98", Property(product, "elyaf_pamuk_yuzde").GetProperty("value").GetRawText());

        var batch = Property(product, "batch_no");
        Assert.Equal("B2027-01", batch.GetProperty("value").GetString());
        var place = Assert.Single(batch.GetProperty("valueReference").EnumerateArray());
        Assert.Equal("Denizli", place.GetProperty("value").GetString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("?rol=tuketici")]
    [InlineData("?rol=YETKILI")]
    [InlineData("?rol=")]
    public async Task Carbon_is_hidden_from_both_outputs_by_default(string query)
    {
        await _app.ImportSampleAsync();

        foreach (var url in new[] { $"/dpp/{Gtin}{query}", $"/dpp/{Gtin}.jsonld{query}" })
        {
            var body = await GetStringAsync(url);
            Assert.DoesNotContain("12.4", body);
            Assert.DoesNotContain("12,4", body);
            Assert.DoesNotContain("karbon", body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Carbon_is_visible_in_both_outputs_for_authorized_role()
    {
        await _app.ImportSampleAsync();

        var html = await GetStringAsync($"/dpp/{Gtin}?rol=yetkili");
        var json = await GetStringAsync($"/dpp/{Gtin}.jsonld?rol=yetkili");

        Assert.Contains("<td>12.4</td>", html);
        var carbon = Property(JsonDocument.Parse(json).RootElement, "batch_no")
            .GetProperty("valueReference").EnumerateArray()
            .Single(p => p.GetProperty("propertyID").GetString() == "karbon_kg_co2e");
        Assert.Equal(12.4m, carbon.GetProperty("value").GetDecimal());
    }

    [Fact]
    public async Task Html_embeds_the_same_json_ld_as_the_endpoint()
    {
        await _app.ImportSampleAsync();

        foreach (var query in new[] { "", "?rol=yetkili" })
        {
            var html = await GetStringAsync($"/dpp/{Gtin}{query}");
            var json = await GetStringAsync($"/dpp/{Gtin}.jsonld{query}");

            var embedded = Regex.Match(html, "<script type=\"application/ld\\+json\">(.*?)</script>", RegexOptions.Singleline);
            Assert.Equal(json, embedded.Groups[1].Value);
        }
    }

    [Fact]
    public async Task Html_special_characters_from_csv_are_escaped()
    {
        var result = await _app.ImportAsync("08690000000012,<script>alert(1)</script>,B1,98,2,Denizli,1");
        Assert.Empty(result.Rejected);

        var html = await GetStringAsync($"/dpp/{Gtin}");

        Assert.DoesNotContain("<script>alert", html);
    }

    [Fact]
    public async Task Turkish_characters_stay_readable_in_outputs()
    {
        await _app.ImportAsync("08690000000012,Şık Kot Üçlü,B1,98,2,Muğla,1");

        Assert.Contains("Şık Kot Üçlü", await GetStringAsync($"/dpp/{Gtin}"));
        Assert.Contains("Şık Kot Üçlü", await GetStringAsync($"/dpp/{Gtin}.jsonld"));
    }

    [Theory]
    [InlineData("/dpp/08690000000099")]
    [InlineData("/dpp/08690000000099.jsonld")]
    [InlineData("/dpp/8690000000012")]
    public async Task Unknown_products_return_404(string url)
    {
        await _app.ImportSampleAsync();

        var response = await _app.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Rejected_rows_have_no_passport()
    {
        await _app.ImportSampleAsync();

        // 5. satır: elyaf toplamı %98 olduğu için kaydedilmedi.
        var response = await _app.CreateClient().GetAsync("/dpp/08690000000043");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task All_batches_of_a_model_appear_in_both_outputs()
    {
        await _app.ImportAsync(TestApp.ValidRow, "08690000000012,Slim Denim 501,B2027-99,98,2,Bursa,9.5");

        var html = await GetStringAsync($"/dpp/{Gtin}");
        var json = await GetStringAsync($"/dpp/{Gtin}.jsonld");

        foreach (var value in new[] { "B2027-01", "B2027-99", "Denizli", "Bursa" })
        {
            Assert.Contains(value, html);
            Assert.Contains(value, json);
        }
    }
}
