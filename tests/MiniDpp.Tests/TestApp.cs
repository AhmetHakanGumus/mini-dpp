using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using MiniDpp.Api.Import;

namespace MiniDpp.Tests;

/// <summary>Her test için kendi temiz SQLite veritabanıyla çalışan gerçek uygulama.</summary>
public sealed class TestApp : WebApplicationFactory<Program>
{
    public const string Header = "gtin,model_adi,batch_no,elyaf_pamuk_yuzde,elyaf_elastan_yuzde,uretim_yeri,karbon_kg_co2e";
    public const string ValidRow = "08690000000012,Slim Denim 501,B2027-01,98,2,Denizli,12.4";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"minidpp-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:Dpp", $"Data Source={_databasePath}");

    public static string SampleCsvPath =>
        Path.Combine(AppContext.BaseDirectory, "samples", "urunler_duzeltilmis_test.csv");

    public Task<HttpResponseMessage> UploadAsync(string csv) => UploadAsync(Encoding.UTF8.GetBytes(csv));

    public async Task<HttpResponseMessage> UploadAsync(byte[] bytes)
    {
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(file, "file", "urunler.csv");
        return await CreateClient().PostAsync("/api/import", content);
    }

    public async Task<ImportResult> ImportAsync(params string[] rows)
    {
        var response = await UploadAsync(string.Join('\n', [Header, .. rows]));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ImportResult>(JsonOptions))!;
    }

    public async Task<ImportResult> ImportSampleAsync()
    {
        var response = await UploadAsync(await File.ReadAllBytesAsync(SampleCsvPath));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ImportResult>(JsonOptions))!;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }
}
