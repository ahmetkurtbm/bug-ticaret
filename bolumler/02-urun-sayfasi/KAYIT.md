# TZ-4131 · Ürün sayfası beyaz ekran veriyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4131 |
| Seviye | ● (1/5) |
| Nerede görülüyor | Müşteri sitesi · Ürün sayfası |
| Bildiren | Kaan Bozkurt, QA mühendisi |
| Öncelik | Yüksek |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Listeden bir ürüne tıklıyorum. Sayfa bir an gri yükleme kutusu gösteriyor, sonra her şey siliniyor: üst menü, arama kutusu, alt bilgi dahil bütün sayfa bembeyaz kalıyor. F5 yapmak da işe yaramıyor, hangi ürüne girsem aynı.

Tarayıcı konsolunda (F12 → Console) kırmızı bir hata satırı çıkıyor.

Ana sayfa ve kategori sayfaları normal açılıyor, sorun yalnızca ürün sayfasında.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. http://localhost:5173 adresini açın ve tarayıcı konsolunu (F12 → Console) açık tutun.
3. Ana sayfadan ya da bir kategoriden herhangi bir ürüne tıklayın.

## Beklenen

Ürün adı, fiyat, görseller ve "Sepete ekle" düğmesi olan ürün sayfası açılır.

## Gerçekleşen

Sayfa tamamen beyaz kalır, konsolda kırmızı hata görünür.

## Ek bilgi

- Dün öğleden sonraki sürümde ürün sayfası sorunsuzdu.
- En az üç farklı ürünle denedim, hepsinde aynı.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 2
```

---

## English

# TZ-4131 · Product page goes blank

| Field | Value |
| --- | --- |
| Ticket | TZ-4131 |
| Level | ● (1/5) |
| Where it shows | Storefront · Product page |
| Reporter | Kaan Bozkurt, QA engineer |
| Priority | High |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

I click a product in a list. The page shows a grey loading box for a moment, then everything disappears: the top menu, the search box and the footer go too, and the whole page stays white. Refreshing does not help, and it is the same for every product.

The browser console (F12 → Console) shows a red error line.

The home page and category pages open normally; only the product page is affected.

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Open http://localhost:5173 and keep the browser console (F12 → Console) open.
3. Click any product on the home page or in a category.

### Expected

The product page opens, with the product name, price, images and an "Add to cart" button.

### Actual

The page stays completely white and a red error appears in the console.

### Additional information

- Yesterday afternoon's build had a working product page.
- I tried at least three different products and all behave the same.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 2
```
