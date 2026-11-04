using Microsoft.EntityFrameworkCore;
using MiniDpp.Api.Data;
using MiniDpp.Api.Validation;

namespace MiniDpp.Api.Import;

public class ProductImportService(DppDbContext db)
{
    public async Task<ImportResult> ImportAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        var rows = CsvProductReader.Read(csvStream);
        var saved = new List<SavedRow>();
        var rejected = new List<RejectedRow>();

        // Dosya içi çelişkiler: aynı GTIN için farklı model bilgisi veya tekrar eden batch.
        var modelsInFile = new Dictionary<string, ValidProductRow>();
        var batchesInFile = new HashSet<(string Gtin, string BatchNo)>();

        foreach (var row in rows)
        {
            if (row.StructureError is not null)
            {
                rejected.Add(new RejectedRow(row.RowNumber, null, [row.StructureError]));
                continue;
            }

            var validation = ProductRowValidator.Validate(row.Raw!);
            var errors = validation.IsValid
                ? FindConflicts(validation.Row!, modelsInFile, batchesInFile)
                : validation.Errors;
            if (errors.Count > 0)
            {
                rejected.Add(new RejectedRow(row.RowNumber, NullIfBlank(row.Raw!.Gtin), errors));
                continue;
            }

            var valid = validation.Row!;
            modelsInFile.TryAdd(valid.Gtin, valid);
            batchesInFile.Add((valid.Gtin, valid.BatchNo));

            try
            {
                // Her satır kendi kaydıyla yazılır: bir satırın hatası diğerlerini geri almaz.
                var status = await SaveAsync(valid, cancellationToken);
                saved.Add(new SavedRow(row.RowNumber, valid.Gtin, valid.ModelName, valid.BatchNo, status));
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear();
                rejected.Add(new RejectedRow(row.RowNumber, valid.Gtin,
                    [new FieldError("satir", "Satır veritabanına kaydedilemedi.")]));
            }
        }

        return new ImportResult(rows.Count, saved, rejected);
    }

    private static IReadOnlyList<FieldError> FindConflicts(
        ValidProductRow row,
        Dictionary<string, ValidProductRow> modelsInFile,
        HashSet<(string Gtin, string BatchNo)> batchesInFile)
    {
        var errors = new List<FieldError>();
        if (batchesInFile.Contains((row.Gtin, row.BatchNo)))
        {
            errors.Add(new FieldError(
                ProductRowValidator.BatchNoColumn,
                $"Aynı GTIN ve batch numarası dosyada birden fazla kez geçiyor ({row.BatchNo})."));
        }
        else if (modelsInFile.TryGetValue(row.Gtin, out var first)
                 && (first.ModelName != row.ModelName
                     || first.CottonPercent != row.CottonPercent
                     || first.ElastanePercent != row.ElastanePercent))
        {
            errors.Add(new FieldError(
                ProductRowValidator.GtinColumn,
                "Aynı GTIN dosyada farklı model adı veya elyaf oranlarıyla geçiyor."));
        }

        return errors;
    }

    private async Task<SavedRowStatus> SaveAsync(ValidProductRow row, CancellationToken cancellationToken)
    {
        var model = await db.ProductModels
            .Include(p => p.Batches)
            .SingleOrDefaultAsync(p => p.Gtin == row.Gtin, cancellationToken);

        if (model is null)
        {
            model = new ProductModel
            {
                Gtin = row.Gtin,
                Name = row.ModelName,
                CottonPercent = row.CottonPercent,
                ElastanePercent = row.ElastanePercent,
                Batches = [ToBatch(row)],
            };
            db.ProductModels.Add(model);
            await db.SaveChangesAsync(cancellationToken);
            return SavedRowStatus.Created;
        }

        var batch = model.Batches.SingleOrDefault(b => b.BatchNo == row.BatchNo);
        if (!HasChanges(model, batch, row))
        {
            return SavedRowStatus.Unchanged;
        }

        model.Name = row.ModelName;
        model.CottonPercent = row.CottonPercent;
        model.ElastanePercent = row.ElastanePercent;
        if (batch is null)
        {
            model.Batches.Add(ToBatch(row));
        }
        else
        {
            batch.ProductionPlace = row.ProductionPlace;
            batch.CarbonKgCo2e = row.CarbonKgCo2e;
        }

        await db.SaveChangesAsync(cancellationToken);
        return SavedRowStatus.Updated;
    }

    private static bool HasChanges(ProductModel model, Batch? batch, ValidProductRow row) =>
        batch is null
        || model.Name != row.ModelName
        || model.CottonPercent != row.CottonPercent
        || model.ElastanePercent != row.ElastanePercent
        || batch.ProductionPlace != row.ProductionPlace
        || batch.CarbonKgCo2e != row.CarbonKgCo2e;

    private static Batch ToBatch(ValidProductRow row) => new()
    {
        BatchNo = row.BatchNo,
        ProductionPlace = row.ProductionPlace,
        CarbonKgCo2e = row.CarbonKgCo2e,
    };

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
