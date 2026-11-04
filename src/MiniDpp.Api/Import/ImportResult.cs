using MiniDpp.Api.Validation;

namespace MiniDpp.Api.Import;

public enum SavedRowStatus
{
    Created,
    Updated,
    Unchanged,
}

public sealed record SavedRow(int Row, string Gtin, string ModelName, string BatchNo, SavedRowStatus Status);

public sealed record RejectedRow(int Row, string? Gtin, IReadOnlyList<FieldError> Errors);

public sealed record ImportResult(int TotalRows, IReadOnlyList<SavedRow> Saved, IReadOnlyList<RejectedRow> Rejected);
