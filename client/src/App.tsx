import { useEffect, useState, type FormEvent } from "react";
import {
  fetchProducts,
  importCsv,
  type ImportResult,
  type ProductSummary,
  type SavedRowStatus,
} from "./api";

const statusLabels: Record<SavedRowStatus, string> = {
  Created: "Oluşturuldu",
  Updated: "Güncellendi",
  Unchanged: "Değişiklik yok",
};

export function App() {
  const [file, setFile] = useState<File | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<ImportResult | null>(null);
  const [products, setProducts] = useState<ProductSummary[]>([]);

  useEffect(() => {
    fetchProducts()
      .then(setProducts)
      .catch((e: Error) => setError(e.message));
  }, []);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!file) return;

    setBusy(true);
    setError(null);
    setResult(null);
    try {
      setResult(await importCsv(file));
      setProducts(await fetchProducts());
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <main>
      <header>
        <p className="eyebrow">ESG Next teknik değerlendirme</p>
        <h1>Mini Dijital Ürün Pasaportu</h1>
      </header>

      <section>
        <h2>CSV içe aktar</h2>
        <form onSubmit={handleSubmit}>
          <input
            type="file"
            accept=".csv,text/csv"
            aria-label="CSV dosyası"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
          />
          <button type="submit" disabled={!file || busy}>
            {busy ? "Yükleniyor…" : "Yükle"}
          </button>
        </form>
        {error && (
          <p className="alert" role="alert">
            {error}
          </p>
        )}
      </section>

      {result && <ImportReport result={result} />}

      <section>
        <h2>Kayıtlı ürünler ({products.length})</h2>
        {products.length === 0 ? (
          <p className="muted">Henüz ürün yok. Bir CSV dosyası yükleyin.</p>
        ) : (
          <ul className="products">
            {products.map((product) => (
              <ProductCard key={product.gtin} product={product} />
            ))}
          </ul>
        )}
      </section>
    </main>
  );
}

function ImportReport({ result }: { result: ImportResult }) {
  return (
    <section aria-live="polite">
      <h2>İçe aktarma sonucu</h2>
      <p>
        Toplam {result.totalRows} satır: <strong>{result.saved.length} kaydedildi</strong>,{" "}
        <strong className={result.rejected.length > 0 ? "danger" : undefined}>
          {result.rejected.length} hatalı
        </strong>
        .
      </p>

      {result.rejected.length > 0 && (
        <table>
          <thead>
            <tr>
              <th>Satır</th>
              <th>GTIN</th>
              <th>Hata nedeni</th>
            </tr>
          </thead>
          <tbody>
            {result.rejected.map((row) => (
              <tr key={row.row}>
                <td>{row.row}</td>
                <td>{row.gtin ?? "—"}</td>
                <td>
                  {row.errors.map((error) => (
                    <div key={error.field + error.message}>{error.message}</div>
                  ))}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {result.saved.length > 0 && (
        <table>
          <thead>
            <tr>
              <th>Satır</th>
              <th>GTIN</th>
              <th>Model</th>
              <th>Batch</th>
              <th>Durum</th>
            </tr>
          </thead>
          <tbody>
            {result.saved.map((row) => (
              <tr key={row.row}>
                <td>{row.row}</td>
                <td>{row.gtin}</td>
                <td>{row.modelName}</td>
                <td>{row.batchNo}</td>
                <td>{statusLabels[row.status]}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}

function ProductCard({ product }: { product: ProductSummary }) {
  return (
    <li>
      <img src={product.qrUrl} alt={`${product.modelName} QR kodu`} width={96} height={96} />
      <div>
        <strong>{product.modelName}</strong>
        <div className="muted">
          {product.gtin} · {product.batchCount} batch
        </div>
        <div className="links">
          <a href={product.passportUrl}>Pasaport</a>
          <a href={product.jsonLdUrl}>JSON-LD</a>
          <a href={`${product.passportUrl}?rol=yetkili`}>Yetkili görünüm</a>
          <a href={product.qrUrl} download={`${product.gtin}.png`}>
            QR indir
          </a>
        </div>
      </div>
    </li>
  );
}
