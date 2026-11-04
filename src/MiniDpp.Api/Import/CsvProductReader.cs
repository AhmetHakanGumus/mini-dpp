using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using MiniDpp.Api.Validation;

namespace MiniDpp.Api.Import;

/// <summary>Satır numarası, başlık satırı 1 sayılarak fiziksel CSV satırını gösterir.</summary>
public sealed record CsvRow(int RowNumber, RawProductRow? Raw, FieldError? StructureError);

public sealed class InvalidCsvFileException(string message) : Exception(message);

public static class CsvProductReader
{
    private static readonly string[] RequiredColumns =
    [
        ProductRowValidator.GtinColumn,
        ProductRowValidator.ModelNameColumn,
        ProductRowValidator.BatchNoColumn,
        ProductRowValidator.CottonColumn,
        ProductRowValidator.ElastaneColumn,
        ProductRowValidator.ProductionPlaceColumn,
        ProductRowValidator.CarbonColumn,
    ];

    public static List<CsvRow> Read(Stream stream)
    {
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant(),
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null,
        };
        using var csv = new CsvReader(reader, configuration);

        if (!csv.Read() || !csv.ReadHeader())
        {
            throw new InvalidCsvFileException("CSV dosyası boş.");
        }

        var headers = csv.HeaderRecord!.Select(h => h.Trim().ToLowerInvariant()).ToList();
        var missing = RequiredColumns.Except(headers).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidCsvFileException($"CSV dosyasında eksik sütunlar var: {string.Join(", ", missing)}.");
        }

        var rows = new List<CsvRow>();
        while (csv.Read())
        {
            var rowNumber = csv.Parser.RawRow;
            if (csv.Parser.Count != headers.Count)
            {
                // Tırnaksız "12,4" gibi değerler sütunları kaydırır; yanlış sütunu okumak yerine satırı reddederiz.
                rows.Add(new CsvRow(rowNumber, null, new FieldError(
                    "satir",
                    $"Satırdaki sütun sayısı başlıkla uyuşmuyor (beklenen: {headers.Count}, gelen: {csv.Parser.Count}).")));
                continue;
            }

            rows.Add(new CsvRow(rowNumber, new RawProductRow(
                csv.GetField(ProductRowValidator.GtinColumn),
                csv.GetField(ProductRowValidator.ModelNameColumn),
                csv.GetField(ProductRowValidator.BatchNoColumn),
                csv.GetField(ProductRowValidator.CottonColumn),
                csv.GetField(ProductRowValidator.ElastaneColumn),
                csv.GetField(ProductRowValidator.ProductionPlaceColumn),
                csv.GetField(ProductRowValidator.CarbonColumn)), null));
        }

        return rows;
    }
}
