using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniDpp.Api.Data;
using MiniDpp.Api.Import;

namespace MiniDpp.Tests;

public class ImportTests : IDisposable
{
    private readonly TestApp _app = new();

    public void Dispose() => _app.Dispose();

    private async Task<T> QueryAsync<T>(Func<DppDbContext, Task<T>> query)
    {
        using var scope = _app.Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<DppDbContext>());
    }

    [Fact]
    public async Task Sample_file_saves_three_rows_and_rejects_rows_4_and_5()
    {
        var result = await _app.ImportSampleAsync();

        Assert.Equal(5, result.TotalRows);
        Assert.Equal([2, 3, 6], result.Saved.Select(r => r.Row));
        Assert.Equal([4, 5], result.Rejected.Select(r => r.Row));

        Assert.Contains("GTIN boş", Assert.Single(result.Rejected[0].Errors).Message);
        Assert.Contains("%100", Assert.Single(result.Rejected[1].Errors).Message);
    }

    [Fact]
    public async Task Gtin_is_stored_with_leading_zero_and_batch_in_related_table()
    {
        await _app.ImportSampleAsync();

        var model = await QueryAsync(db => db.ProductModels.Include(p => p.Batches)
            .SingleAsync(p => p.Gtin == "08690000000012"));

        Assert.Equal("Slim Denim 501", model.Name);
        var batch = Assert.Single(model.Batches);
        Assert.Equal("B2027-01", batch.BatchNo);
        Assert.Equal("Denizli", batch.ProductionPlace);
        Assert.Equal(12.4m, batch.CarbonKgCo2e);
    }

    [Fact]
    public async Task Rows_after_an_invalid_row_are_still_saved()
    {
        var result = await _app.ImportAsync(
            "not-a-gtin,Hatalı,B1,98,2,Denizli,1",
            TestApp.ValidRow);

        Assert.Equal(2, Assert.Single(result.Rejected).Row);
        Assert.Equal(3, Assert.Single(result.Saved).Row);
    }

    [Fact]
    public async Task Blank_lines_do_not_shift_row_numbers()
    {
        var result = await _app.ImportAsync("", "", "bad,Hatalı,B1,98,2,Denizli,1");

        Assert.Equal(4, Assert.Single(result.Rejected).Row);
    }

    [Fact]
    public async Task Wrong_column_count_is_a_row_error()
    {
        // Ondalık virgül, tırnaksız yazılınca fazladan sütun üretir.
        var result = await _app.ImportAsync("08690000000012,Slim,B1,98,2,Denizli,12,4");

        Assert.Contains("sütun sayısı", Assert.Single(Assert.Single(result.Rejected).Errors).Message);
    }

    [Fact]
    public async Task Same_batch_twice_in_a_file_is_rejected()
    {
        var result = await _app.ImportAsync(TestApp.ValidRow, TestApp.ValidRow);

        Assert.Equal(2, Assert.Single(result.Saved).Row);
        Assert.Equal(3, Assert.Single(result.Rejected).Row);
    }

    [Fact]
    public async Task Same_gtin_with_different_composition_in_a_file_is_rejected()
    {
        var result = await _app.ImportAsync(
            TestApp.ValidRow,
            "08690000000012,Slim Denim 501,B2027-99,97,3,Denizli,12.4");

        Assert.Contains("elyaf", Assert.Single(Assert.Single(result.Rejected).Errors).Message);
    }

    [Fact]
    public async Task New_batch_of_existing_gtin_joins_the_same_model()
    {
        var result = await _app.ImportAsync(
            TestApp.ValidRow,
            "08690000000012,Slim Denim 501,B2027-99,98,2,Bursa,9.5");

        Assert.Empty(result.Rejected);
        Assert.Equal(1, await QueryAsync(db => db.ProductModels.CountAsync()));
        Assert.Equal(2, await QueryAsync(db => db.Batches.CountAsync()));
    }

    [Fact]
    public async Task Importing_the_same_file_again_changes_nothing()
    {
        await _app.ImportSampleAsync();
        var second = await _app.ImportSampleAsync();

        Assert.All(second.Saved, r => Assert.Equal(SavedRowStatus.Unchanged, r.Status));
        Assert.Equal(3, await QueryAsync(db => db.Batches.CountAsync()));
    }

    [Fact]
    public async Task Bom_and_crlf_files_are_read()
    {
        var csv = "﻿" + TestApp.Header + "\r\n" + TestApp.ValidRow + "\r\n";

        var response = await _app.UploadAsync(csv);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"saved\":[{\"row\":2", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("gtin,model_adi\n08690000000012,Slim")]
    [InlineData("gtin;model_adi;batch_no;elyaf_pamuk_yuzde;elyaf_elastan_yuzde;uretim_yeri;karbon_kg_co2e\n")]
    public async Task Unusable_files_return_400(string csv)
    {
        var response = await _app.UploadAsync(csv);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
