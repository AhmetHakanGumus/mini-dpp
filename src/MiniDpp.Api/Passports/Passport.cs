namespace MiniDpp.Api.Passports;

/// <summary>
/// HTML ve JSON-LD çıktılarının ortak kaynağı. Karbon değeri yetkisiz görünümde bu modele hiç
/// girmez; böylece iki çıktı rol ayrımı konusunda birbirinden farklılaşamaz.
/// </summary>
public sealed record Passport(
    string Gtin,
    string ModelName,
    decimal CottonPercent,
    decimal ElastanePercent,
    IReadOnlyList<PassportBatch> Batches,
    bool ShowsCarbon);

public sealed record PassportBatch(string BatchNo, string ProductionPlace, decimal? CarbonKgCo2e);
