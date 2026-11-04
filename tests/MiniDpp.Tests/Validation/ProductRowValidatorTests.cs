using MiniDpp.Api.Validation;

namespace MiniDpp.Tests.Validation;

public class ProductRowValidatorTests
{
    private static RawProductRow Row(
        string? gtin = "08690000000012",
        string? model = "Slim Denim 501",
        string? batch = "B2027-01",
        string? cotton = "98",
        string? elastane = "2",
        string? place = "Denizli",
        string? carbon = "12.4") => new(gtin, model, batch, cotton, elastane, place, carbon);

    private static FieldError SingleError(RawProductRow row) =>
        Assert.Single(ProductRowValidator.Validate(row).Errors);

    [Fact]
    public void Valid_row_keeps_leading_zero_and_parses_numbers()
    {
        var result = ProductRowValidator.Validate(Row());

        Assert.True(result.IsValid);
        Assert.Equal("08690000000012", result.Row!.Gtin);
        Assert.Equal(98m, result.Row.CottonPercent);
        Assert.Equal(12.4m, result.Row.CarbonKgCo2e);
    }

    [Theory]
    [InlineData("", "boş")]
    [InlineData("8690000000012", "14 haneli")]
    [InlineData("0869000000001X", "14 haneli")]
    [InlineData("08690000000013", "kontrol basamağı")]
    public void Invalid_gtin_is_reported(string gtin, string expected)
    {
        var error = SingleError(Row(gtin: gtin));

        Assert.Equal("gtin", error.Field);
        Assert.Contains(expected, error.Message);
    }

    [Theory]
    [InlineData("abc", "2", "elyaf_pamuk_yuzde", "sayısal")]
    [InlineData("98", "", "elyaf_elastan_yuzde", "boş")]
    [InlineData("-1", "101", "elyaf_pamuk_yuzde", "0 ile 100")]
    [InlineData("101", "-1", "elyaf_pamuk_yuzde", "0 ile 100")]
    public void Invalid_percentages_are_reported(string cotton, string elastane, string field, string expected)
    {
        var errors = ProductRowValidator.Validate(Row(cotton: cotton, elastane: elastane)).Errors;

        Assert.Contains(errors, e => e.Field == field && e.Message.Contains(expected));
    }

    [Fact]
    public void Percentages_must_total_exactly_100()
    {
        var error = SingleError(Row(cotton: "95", elastane: "3"));

        Assert.Contains("%100", error.Message);
        Assert.Contains("%98", error.Message);
    }

    [Fact]
    public void Decimal_percentages_total_without_rounding_errors() =>
        Assert.True(ProductRowValidator.Validate(Row(cotton: "99.9", elastane: "0.1")).IsValid);

    [Theory]
    [InlineData("100", "0")]
    [InlineData("0", "100")]
    public void Percentage_boundaries_are_accepted(string cotton, string elastane) =>
        Assert.True(ProductRowValidator.Validate(Row(cotton: cotton, elastane: elastane)).IsValid);

    [Theory]
    [InlineData("model_adi", null, "x", "x")]
    [InlineData("batch_no", "x", "  ", "x")]
    [InlineData("uretim_yeri", "x", "x", "")]
    public void Required_text_fields_cannot_be_blank(string field, string? model, string batch, string place)
    {
        var error = SingleError(Row(model: model, batch: batch, place: place));

        Assert.Equal(field, error.Field);
    }

    [Theory]
    [InlineData("-0.1", "negatif")]
    [InlineData("abc", "sayısal")]
    [InlineData("12,4", "sayısal")]
    [InlineData("1e3", "sayısal")]
    [InlineData("", "boş")]
    public void Invalid_carbon_is_reported(string carbon, string expected)
    {
        var error = SingleError(Row(carbon: carbon));

        Assert.Equal("karbon_kg_co2e", error.Field);
        Assert.Contains(expected, error.Message);
    }

    [Fact]
    public void Zero_carbon_is_valid() =>
        Assert.True(ProductRowValidator.Validate(Row(carbon: "0")).IsValid);

    [Fact]
    public void All_errors_of_a_row_are_returned_together()
    {
        var errors = ProductRowValidator.Validate(new RawProductRow("", "", "", "x", "y", "", "z")).Errors;

        Assert.Equal(7, errors.Count);
    }
}
