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

    public List<Batch> Batches { get; set; } = [];
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
