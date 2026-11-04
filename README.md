# Mini Dijital Ürün Pasaportu (Mini DPP)

ESG Next Yazılım Mühendisi (Full-Stack) teknik değerlendirmesi için geliştirildi. Denim ürünlerini içeren bir CSV dosyasını okur, doğrular, geçerli kayıtları saklar ve her ürün için HTML pasaport sayfası, JSON-LD çıktısı ve QR kod sunar.

**Teknolojiler:** ASP.NET Core 9 Web API (C#), EF Core, SQLite, React + TypeScript (Vite), xUnit

## Kurulum ve çalıştırma

Yalnızca [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) gerekir. Derlenmiş arayüz depoda bulunduğu için Node.js şart değildir. Veritabanı ilk açılışta otomatik oluşturulur.

```bash
dotnet run --project src/MiniDpp.Api
```

| Adres | İçerik |
| --- | --- |
| http://localhost:5031 | CSV yükleme ekranı ve ürün listesi |
| http://localhost:5031/swagger | API dokümantasyonu |

## CSV içe aktarma

Ekrandan `samples/urunler_duzeltilmis_test.csv` dosyasını seçip yükleyebilir veya API'yi doğrudan çağırabilirsiniz:

```bash
curl -F "file=@samples/urunler_duzeltilmis_test.csv" http://localhost:5031/api/import
```

Örnek dosyada 3 satır kaydedilir, 2 satır reddedilir:

```json
{
  "totalRows": 5,
  "saved": [
    { "row": 2, "gtin": "08690000000012", "modelName": "Slim Denim 501", "batchNo": "B2027-01", "status": "Created" },
    { "row": 3, "gtin": "08690000000029", "modelName": "Regular Denim 502", "batchNo": "B2027-02", "status": "Created" },
    { "row": 6, "gtin": "08690000000050", "modelName": "Mom Jean 505", "batchNo": "B2027-05", "status": "Created" }
  ],
  "rejected": [
    { "row": 4, "gtin": null, "errors": [{ "field": "gtin", "message": "GTIN boş olamaz." }] },
    {
      "row": 5,
      "gtin": "08690000000043",
      "errors": [
        {
          "field": "elyaf_pamuk_yuzde+elyaf_elastan_yuzde",
          "message": "Pamuk ve elastan oranlarının toplamı %100 olmalıdır (toplam: %98)."
        }
      ]
    }
  ]
}
```

Satır numarasında başlık satırı 1. satır sayılır. Dosya kullanılamaz durumdaysa (boş veya eksik sütunlu) yanıt `400` olur ve hiçbir satır işlenmez.

> CSV dosyasını Excel'de açıp kaydetmeyin; GTIN'lerin başındaki sıfırlar silinebilir.

## Örnek adresler

Örnek dosya yüklendikten sonra:

| Çıktı | Tüketici görünümü | Yetkili görünüm |
| --- | --- | --- |
| HTML | http://localhost:5031/dpp/08690000000012 | http://localhost:5031/dpp/08690000000012?rol=yetkili |
| JSON-LD | http://localhost:5031/dpp/08690000000012.jsonld | http://localhost:5031/dpp/08690000000012.jsonld?rol=yetkili |
| QR (PNG) | http://localhost:5031/dpp/08690000000012/qr.png | |

Kayıtlı ürünlerin listesi: `GET /api/products`

## Tasarım kararları ve varsayımlar

- **Veri modeli:** `ProductModels` (GTIN, ad, elyaf oranları) ile `Batches` (batch numarası, üretim yeri, karbon) bire-çok ilişkilidir. Bir GTIN'in bir modeli tanımladığını, üretim yeri ve karbon değerinin ise partiye göre değişebileceğini varsaydım. Bu yüzden bir modelin tüm partileri pasaportta listelenir.
- **GTIN metin olarak saklanır**, böylece baştaki sıfırlar korunur.
- **Rol ayrımı tek noktadadır:** HTML ve JSON-LD aynı `Passport` modelinden üretilir. `PassportService` yetkisiz görünümde karbon değerini bu modele hiç koymaz; böylece iki çıktı birbirinden farklılaşamaz. `?rol=yetkili` ödevde istendiği gibi yalnızca demo amaçlıdır, gerçek bir yetkilendirme değildir. Değer birebir eşleşmelidir (`?rol=YETKILI` tüketici görünümü verir).
- **Oranlar `decimal` ile hesaplanır**, böylece `99.9 + 0.1` gibi toplamlarda yuvarlama hatası oluşmaz. Ondalık ayırıcı noktadır.
- **Her satır ayrı kaydedilir**, böylece bir satırdaki sorun diğerlerini geri almaz.
- **Dosya içi çelişkiler hatalı sayılır:** Aynı GTIN + batch ikinci kez geçerse veya aynı GTIN farklı model adı ya da elyaf oranıyla geçerse ilk satır kaydedilir, sonraki reddedilir. Ödevde belirtilmediği için bu kuralı ben ekledim.
- **Veritabanındaki bir kayıt aynı GTIN ile yeniden yüklenirse güncellenir.** Yeni batch numarası modele yeni parti olarak eklenir. Değişiklik yoksa kayıt ve sürüm değişmez, yani aynı dosya tekrar yüklenebilir.
- **QR kod istek anında üretilir** ve rol parametresi içermeyen tüketici sayfasını gösterir. Adres varsayılan olarak isteğin geldiği sunucudan alınır. Telefondan okutmak için dışarıdan erişilebilir adres verin:

  ```bash
  dotnet run --project src/MiniDpp.Api --PublicBaseUrl=http://192.168.1.20:5031 --urls=http://0.0.0.0:5031
  ```

- **JSON-LD:** Temel alanlar `schema.org/Product` özellikleridir (`name`, `gtin14`, `material`, `url`). Batch, üretim yeri, karbon ve sürüm bilgileri için `Product` üzerinde doğrudan karşılık olmadığından `additionalProperty` içinde `PropertyValue` kullanılır.

## Bonus: Sürümleme

Ürün değiştiğinde önceki hali, sürüm numarası ve güncelleme tarihiyle `ProductVersions` tablosuna JSON olarak kopyalanır. Yeni batch eklenmesi de yeni sürüm sayılır. Önceki sürümler HTML ve JSON-LD'de görünür; eski karbon değerleri de rol ayrımına tabidir.

## Testler

```bash
dotnet test
```

Birim testleri doğrulama kurallarını ve GS1 kontrol basamağını kapsar. Entegrasyon testleri uygulamayı gerçek SQLite ile çalıştırıp içe aktarma, HTML, JSON-LD, QR, rol ayrımı ve sürümlemeyi uçtan uca dener.

## Arayüz geliştirme

Yalnızca `client/` altındaki kaynak kodu değiştirirseniz gerekir (Node.js ister):

```bash
cd client
npm ci
npm run dev
npm run build
```

`npm run dev` arayüzü API'ye yönlendirerek geliştirme sunucusunda açar. `npm run build` derlenen dosyaları `src/MiniDpp.Api/wwwroot` klasörüne yazar.

## Proje yapısı

```text
src/MiniDpp.Api/
  Controllers/    Import, Products ve Dpp (HTML, JSON-LD, QR) endpoint'leri
  Data/           EF Core modelleri ve migration'lar
  Import/         CSV okuma ve kaydetme
  Passports/      Rol filtresi ve JSON-LD üretimi
  Validation/     GTIN ve satır doğrulama kuralları
  Views/          HTML pasaport sayfası
  wwwroot/        Derlenmiş React arayüzü
client/           React + TypeScript kaynak kodu
tests/            xUnit testleri
samples/          Örnek CSV
```
