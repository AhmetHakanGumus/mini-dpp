using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;

namespace MiniDpp.Api.Passports;

public static class PassportJsonLd
{
    public const string ContentType = "application/ld+json";

    // Türkçe karakterler okunur kalır; < > & kaçışlandığı için HTML içine gömmek de güvenlidir.
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    public static string Serialize(Passport passport, string pageUrl)
    {
        var additionalProperties = new JsonArray(
            Property("elyaf_pamuk_yuzde", "Pamuk oranı", JsonValue.Create(passport.CottonPercent), "%"),
            Property("elyaf_elastan_yuzde", "Elastan oranı", JsonValue.Create(passport.ElastanePercent), "%"),
            Property("surum", "Sürüm", JsonValue.Create(passport.Version)),
            Property("guncelleme_tarihi", "Son güncelleme", FormatDate(passport.UpdatedAt)));

        foreach (var batch in passport.Batches)
        {
            additionalProperties.Add(BatchProperty(batch));
        }

        foreach (var version in passport.PreviousVersions)
        {
            additionalProperties.Add(VersionProperty(version));
        }

        var product = new JsonObject
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Product",
            ["@id"] = pageUrl,
            ["url"] = pageUrl,
            ["name"] = passport.ModelName,
            ["gtin14"] = passport.Gtin,
            ["material"] = Material(passport.CottonPercent, passport.ElastanePercent),
            ["additionalProperty"] = additionalProperties,
        };

        return product.ToJsonString(Options);
    }

    private static string Material(decimal cottonPercent, decimal elastanePercent)
    {
        var cotton = cottonPercent.ToString(CultureInfo.InvariantCulture);
        var elastane = elastanePercent.ToString(CultureInfo.InvariantCulture);
        return $"%{cotton} pamuk, %{elastane} elastan";
    }

    private static string FormatDate(DateTime value) =>
        value.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private static JsonObject VersionProperty(PassportVersion version)
    {
        var details = new JsonArray(
            Property("guncelleme_tarihi", "Güncelleme tarihi", FormatDate(version.UpdatedAt)),
            Property("model_adi", "Model adı", version.ModelName),
            Property("material", "Malzeme", Material(version.CottonPercent, version.ElastanePercent)));
        foreach (var batch in version.Batches)
        {
            details.Add(BatchProperty(batch));
        }

        var property = Property("onceki_surum", "Önceki sürüm", JsonValue.Create(version.VersionNumber));
        property["valueReference"] = details;
        return property;
    }

    private static JsonObject BatchProperty(PassportBatch batch)
    {
        var details = new JsonArray(Property("uretim_yeri", "Üretim yeri", batch.ProductionPlace));
        if (batch.CarbonKgCo2e is { } carbon)
        {
            details.Add(Property("karbon_kg_co2e", "Karbon ayak izi", JsonValue.Create(carbon), "kg CO2e"));
        }

        var property = Property("batch_no", "Batch numarası", batch.BatchNo);
        property["valueReference"] = details;
        return property;
    }

    private static JsonObject Property(string id, string name, JsonNode? value, string? unit = null)
    {
        var property = new JsonObject
        {
            ["@type"] = "PropertyValue",
            ["propertyID"] = id,
            ["name"] = name,
            ["value"] = value,
        };
        if (unit is not null)
        {
            property["unitText"] = unit;
        }

        return property;
    }
}
