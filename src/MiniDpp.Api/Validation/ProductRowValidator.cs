using System.Globalization;

namespace MiniDpp.Api.Validation;

public static class ProductRowValidator
{
    public const string GtinColumn = "gtin";
    public const string ModelNameColumn = "model_adi";
    public const string BatchNoColumn = "batch_no";
    public const string CottonColumn = "elyaf_pamuk_yuzde";
    public const string ElastaneColumn = "elyaf_elastan_yuzde";
    public const string ProductionPlaceColumn = "uretim_yeri";
    public const string CarbonColumn = "karbon_kg_co2e";

    public static RowValidationResult Validate(RawProductRow raw)
    {
        var errors = new List<FieldError>();

        var gtin = raw.Gtin?.Trim() ?? "";
        ValidateGtin(gtin, errors);

        var modelName = Required(raw.ModelName, ModelNameColumn, "Model adı", errors);
        var batchNo = Required(raw.BatchNo, BatchNoColumn, "Batch numarası", errors);
        var place = Required(raw.ProductionPlace, ProductionPlaceColumn, "Üretim yeri", errors);

        var cotton = ParsePercent(raw.CottonPercent, CottonColumn, "Pamuk oranı", errors);
        var elastane = ParsePercent(raw.ElastanePercent, ElastaneColumn, "Elastan oranı", errors);
        if (cotton is not null && elastane is not null && cotton + elastane != 100m)
        {
            errors.Add(new FieldError(
                $"{CottonColumn}+{ElastaneColumn}",
                $"Pamuk ve elastan oranlarının toplamı %100 olmalıdır (toplam: %{Format(cotton.Value + elastane.Value)})."));
        }

        var carbon = ParseCarbon(raw.CarbonKgCo2e, errors);

        if (errors.Count > 0)
        {
            return new RowValidationResult(null, errors);
        }

        return new RowValidationResult(
            new ValidProductRow(gtin, modelName, batchNo, cotton!.Value, elastane!.Value, place, carbon!.Value),
            errors);
    }

    private static void ValidateGtin(string gtin, List<FieldError> errors)
    {
        if (gtin.Length == 0)
        {
            errors.Add(new FieldError(GtinColumn, "GTIN boş olamaz."));
        }
        else if (!Gtin.IsFourteenDigits(gtin))
        {
            errors.Add(new FieldError(GtinColumn, $"GTIN 14 haneli sayısal bir metin olmalıdır (gelen: '{gtin}')."));
        }
        else
        {
            var expected = Gtin.ComputeCheckDigit(gtin);
            if (gtin[^1] - '0' != expected)
            {
                errors.Add(new FieldError(
                    GtinColumn,
                    $"GTIN kontrol basamağı geçersiz (gelen: {gtin[^1]}, beklenen: {expected})."));
            }
        }
    }

    private static string Required(string? value, string column, string label, List<FieldError> errors)
    {
        var trimmed = value?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            errors.Add(new FieldError(column, $"{label} boş olamaz."));
        }

        return trimmed;
    }

    private static decimal? ParsePercent(string? value, string column, string label, List<FieldError> errors)
    {
        var number = ParseNumber(value, column, label, errors);
        if (number is < 0m or > 100m)
        {
            errors.Add(new FieldError(column, $"{label} 0 ile 100 arasında olmalıdır (gelen: {Format(number.Value)})."));
            return null;
        }

        return number;
    }

    private static decimal? ParseCarbon(string? value, List<FieldError> errors)
    {
        var number = ParseNumber(value, CarbonColumn, "Karbon değeri", errors);
        if (number < 0m)
        {
            errors.Add(new FieldError(CarbonColumn, $"Karbon değeri negatif olamaz (gelen: {Format(number.Value)})."));
            return null;
        }

        return number;
    }

    private static decimal? ParseNumber(string? value, string column, string label, List<FieldError> errors)
    {
        var trimmed = value?.Trim() ?? "";
        const NumberStyles style = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        if (decimal.TryParse(trimmed, style, CultureInfo.InvariantCulture, out var number))
        {
            return number;
        }

        errors.Add(new FieldError(
            column,
            trimmed.Length == 0 ? $"{label} boş olamaz." : $"{label} sayısal olmalıdır (gelen: '{trimmed}')."));
        return null;
    }

    private static string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture);
}
