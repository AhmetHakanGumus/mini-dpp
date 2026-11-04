using System.Net;

namespace MiniDpp.Tests;

public class AppSmokeTests : IDisposable
{
    private readonly TestApp _app = new();

    public void Dispose() => _app.Dispose();

    [Fact]
    public async Task Root_serves_the_built_interface()
    {
        var response = await _app.CreateClient().GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<div id=\"root\">", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Swagger_documents_the_api_but_not_the_passport_pages()
    {
        var json = await _app.CreateClient().GetStringAsync("/swagger/v1/swagger.json");

        Assert.Contains("/api/import", json);
        Assert.Contains("/api/products", json);
        Assert.DoesNotContain("/dpp/", json);
    }
}
