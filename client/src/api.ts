export type SavedRowStatus = "Created" | "Updated" | "Unchanged";

export interface SavedRow {
  row: number;
  gtin: string;
  modelName: string;
  batchNo: string;
  status: SavedRowStatus;
}

export interface RejectedRow {
  row: number;
  gtin: string | null;
  errors: { field: string; message: string }[];
}

export interface ImportResult {
  totalRows: number;
  saved: SavedRow[];
  rejected: RejectedRow[];
}

export interface ProductSummary {
  gtin: string;
  modelName: string;
  batchCount: number;
  passportUrl: string;
  jsonLdUrl: string;
  qrUrl: string;
}

export async function fetchProducts(): Promise<ProductSummary[]> {
  const response = await fetch("/api/products");
  if (!response.ok) {
    throw new Error("Ürün listesi alınamadı.");
  }
  return response.json();
}

export async function importCsv(file: File): Promise<ImportResult> {
  const body = new FormData();
  body.append("file", file);

  const response = await fetch("/api/import", { method: "POST", body });
  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new Error(problem?.detail ?? "Dosya içe aktarılamadı.");
  }
  return response.json();
}
