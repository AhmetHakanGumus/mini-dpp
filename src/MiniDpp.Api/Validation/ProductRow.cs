namespace MiniDpp.Api.Validation;

/// <summary>CSV'den okunan, henüz doğrulanmamış ham satır.</summary>
public sealed record RawProductRow(
    string? Gtin,
    string? ModelName,
    string? BatchNo,
    string? CottonPercent,
    string? ElastanePercent,
    string? ProductionPlace,
    string? CarbonKgCo2e);

/// <summary>Tüm kurallardan geçmiş, tipleri çözülmüş satır.</summary>
public sealed record ValidProductRow(
    string Gtin,
    string ModelName,
    string BatchNo,
    decimal CottonPercent,
    decimal ElastanePercent,
    string ProductionPlace,
    decimal CarbonKgCo2e);

public sealed record FieldError(string Field, string Message);

public sealed record RowValidationResult(ValidProductRow? Row, IReadOnlyList<FieldError> Errors)
{
    public bool IsValid => Row is not null;
}
