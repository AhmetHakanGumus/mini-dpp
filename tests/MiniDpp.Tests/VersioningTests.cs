using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniDpp.Api.Data;
using MiniDpp.Api.Import;

namespace MiniDpp.Tests;

public class VersioningTests : IDisposable
{
    private const string Gtin = "08690000000012";
    private const string ChangedCarbonRow = "08690000000012,Slim Denim 501,B2027-01,98,2,Denizli,10.1";

    private readonly TestApp _app = new();

    public void Dispose() => _app.Dispose();

    private async Task<ProductModel> LoadAsync()
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DppDbContext>();
        return await db.ProductModels
            .Include(p => p.PreviousVersions)
            .Include(p => p.Batches)
            .SingleAsync(p => p.Gtin == Gtin);
    }

    [Fact]
    public async Task New_product_starts_at_version_1_without_history()
    {
        await _app.ImportAsync(TestApp.ValidRow);

        var model = await LoadAsync();

        Assert.Equal(1, model.Version);
        Assert.Empty(model.PreviousVersions);
    }

    [Fact]
    public async Task Update_keeps_previous_values_with_version_number_and_date()
    {
        await _app.ImportAsync(TestApp.ValidRow);
        var before = DateTime.UtcNow;

        var result = await _app.ImportAsync(ChangedCarbonRow);

        Assert.Equal(SavedRowStatus.Updated, Assert.Single(result.Saved).Status);
        var model = await LoadAsync();
        Assert.Equal(2, model.Version);
        Assert.Equal(10.1m, Assert.Single(model.Batches).CarbonKgCo2e);

        var previous = Assert.Single(model.PreviousVersions);
        Assert.Equal(1, previous.VersionNumber);
        Assert.InRange(previous.UpdatedAt, before, DateTime.UtcNow);
        var snapshot = JsonSerializer.Deserialize<ProductSnapshot>(previous.SnapshotJson)!;
        Assert.Equal(12.4m, Assert.Single(snapshot.Batches).CarbonKgCo2e);
    }

    [Fact]
    public async Task Each_update_adds_one_version_and_identical_import_adds_none()
    {
        await _app.ImportAsync(TestApp.ValidRow);
        await _app.ImportAsync(ChangedCarbonRow);
        await _app.ImportAsync(ChangedCarbonRow);
        await _app.ImportAsync("08690000000012,Slim Denim 501 V2,B2027-01,98,2,Denizli,10.1");

        var model = await LoadAsync();

        Assert.Equal(3, model.Version);
        Assert.Equal([1, 2], model.PreviousVersions.Select(v => v.VersionNumber).Order());
    }

    [Fact]
    public async Task New_batch_is_also_a_new_version()
    {
        await _app.ImportAsync(TestApp.ValidRow);
        await _app.ImportAsync("08690000000012,Slim Denim 501,B2027-99,98,2,Bursa,9.5");

        var model = await LoadAsync();

        Assert.Equal(2, model.Version);
        var snapshot = JsonSerializer.Deserialize<ProductSnapshot>(Assert.Single(model.PreviousVersions).SnapshotJson)!;
        Assert.Equal("B2027-01", Assert.Single(snapshot.Batches).BatchNo);
    }

    [Theory]
    [InlineData("/dpp/08690000000012")]
    [InlineData("/dpp/08690000000012.jsonld")]
    public async Task Old_carbon_value_is_shown_only_to_authorized_role(string url)
    {
        await _app.ImportAsync(TestApp.ValidRow);
        await _app.ImportAsync(ChangedCarbonRow);
        var client = _app.CreateClient();

        var publicBody = await client.GetStringAsync(url);
        var authorizedBody = await client.GetStringAsync(url + "?rol=yetkili");

        Assert.DoesNotContain("12.4", publicBody);
        Assert.DoesNotContain("10.1", publicBody);
        Assert.Contains("12.4", authorizedBody);
        Assert.Contains("10.1", authorizedBody);
    }

    [Theory]
    [InlineData("/dpp/08690000000012")]
    [InlineData("/dpp/08690000000012.jsonld")]
    public async Task Outputs_show_version_number_and_previous_model_name(string url)
    {
        await _app.ImportAsync(TestApp.ValidRow);
        await _app.ImportAsync("08690000000012,Yeni Ad,B2027-01,98,2,Denizli,12.4");

        var body = await _app.CreateClient().GetStringAsync(url);

        Assert.Contains("Yeni Ad", body);
        Assert.Contains("Slim Denim 501", body);
        Assert.Contains(DateTime.UtcNow.ToString("yyyy-MM-dd"), body);
    }
}
