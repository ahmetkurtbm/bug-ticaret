# TZ-4131 · Ürün sayfası beyaz ekran veriyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4131 |
| Seviye | ● (1/5) |
| Modül | Müşteri sitesi · Ürün detayı |
| Bildiren | Kaan Bozkurt, QA mühendisi |
| Öncelik | Yüksek |
| Ortam | Yerel geliştirme; API http://localhost:5080, müşteri sitesi http://localhost:5173 (`npm run dev:storefront`), Chrome |
| Durum | Açık |

## Ne oldu

Listeden bir ürüne tıklıyorum. Sayfa bir an gri yükleme kutusu gösteriyor, sonra her şey siliniyor: üst menü, arama kutusu, alt bilgi dahil bütün sayfa bembeyaz kalıyor. F5 yapmak da işe yaramıyor, hangi ürüne girsem aynı.

Tarayıcı konsolunda kırmızı olarak şu yazıyor:

> Uncaught Error: Rendered more hooks than during the previous render.

Ana sayfa ve kategori sayfaları normal açılıyor, sorun yalnızca ürün detayında.

## Tekrar etme adımları

1. API'yi başlatın: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
2. `cd frontend && npm run dev:storefront`
3. http://localhost:5173 adresini açın ve tarayıcı konsolunu (F12 → Console) açık tutun.
4. Ana sayfadan ya da bir kategoriden herhangi bir ürüne tıklayın.

## Beklenen

Ürün adı, fiyat, görseller ve "Sepete ekle" düğmesi olan ürün detay sayfası açılır.

## Gerçekleşen

Sayfa tamamen beyaz kalır, konsolda yukarıdaki hata görünür.

## Ek bilgi

- Dün öğleden sonraki sürümde ürün sayfası sorunsuzdu.
- Ürünün kendisinin bozuk olduğunu düşünmüyorum; en az üç farklı ürünle denedim, hepsi aynı.
- Ağ sekmesinde ürün isteği 200 dönüyor ve gövdesi dolu.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 2
```

Bu kontrol bir birim testi çalıştırır (`frontend/apps/storefront/src/__bolum__/fe01-urun-sayfasi.test.tsx`); API'nin çalışması gerekmez.

---

## English

# TZ-4131 · Product page goes blank

| Field | Value |
| --- | --- |
| Ticket | TZ-4131 |
| Level | ● (1/5) |
| Module | Storefront · Product detail |
| Reporter | Kaan Bozkurt, QA engineer |
| Priority | High |
| Environment | Local development; API http://localhost:5080, storefront http://localhost:5173 (`npm run dev:storefront`), Chrome |
| Status | Open |

### What happened

I click a product in a list. The page shows a grey loading box for a moment, then everything disappears: the top menu, the search box and the footer go too, and the whole page stays white. Refreshing does not help, and it is the same for every product.

The browser console shows this in red:

> Uncaught Error: Rendered more hooks than during the previous render.

The home page and category pages open normally; only the product detail page is affected.

### Steps to reproduce

1. Start the API: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
2. `cd frontend && npm run dev:storefront`
3. Open http://localhost:5173 and keep the browser console (F12 → Console) open.
4. Click any product on the home page or in a category.

### Expected

The product detail page opens, with the product name, price, images and an "Add to cart" button.

### Actual

The page stays completely white and the error above appears in the console.

### Additional information

- Yesterday afternoon's build had a working product page.
- I do not think the product data is broken; I tried at least three different products and all behave the same.
- In the Network tab the product request returns 200 with a full body.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 2
```

This check runs a unit test (`frontend/apps/storefront/src/__bolum__/fe01-urun-sayfasi.test.tsx`); the API does not need to be running.
