namespace MiniDpp.Api.Data;

/// <summary>GTIN ile tanımlanan ürün modeli. Elyaf bileşimi modele aittir.</summary>
public class ProductModel
{
    public int Id { get; set; }

    /// <summary>Baştaki sıfırlar korunsun diye sayı değil metin olarak saklanır.</summary>
    public required string Gtin { get; set; }

    public required string Name { get; set; }
    public decimal CottonPercent { get; set; }
    public decimal ElastanePercent { get; set; }

    public int Version { get; set; } = 1;
    public DateTime UpdatedAt { get; set; }

    public List<Batch> Batches { get; set; } = [];
    public List<ProductVersion> PreviousVersions { get; set; } = [];
}

/// <summary>Modelin bir üretim partisi. Üretim yeri ve karbon değeri partiye göre değişebilir.</summary>
public class Batch
{
    public int Id { get; set; }
    public int ProductModelId { get; set; }
    public ProductModel ProductModel { get; set; } = null!;

    public required string BatchNo { get; set; }
    public required string ProductionPlace { get; set; }
    public decimal CarbonKgCo2e { get; set; }
}

/// <summary>Ürün güncellenmeden önceki halinin değişmez kopyası.</summary>
public class ProductVersion
{
    public int Id { get; set; }
    public int ProductModelId { get; set; }
    public ProductModel ProductModel { get; set; } = null!;

    public int VersionNumber { get; set; }

    /// <summary>Bu sürümün yerini yenisinin aldığı an.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary><see cref="ProductSnapshot"/> JSON'u. Karbon içerir; rol filtresi okurken uygulanır.</summary>
    public required string SnapshotJson { get; set; }
}

public sealed record ProductSnapshot(
    string ModelName,
    decimal CottonPercent,
    decimal ElastanePercent,
    IReadOnlyList<BatchSnapshot> Batches);

public sealed record BatchSnapshot(string BatchNo, string ProductionPlace, decimal CarbonKgCo2e);
