# TZ-4176 · Kategori sayfası durmadan istek atıyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4176 |
| Seviye | ● (1/5) |
| Modül | Müşteri sitesi · Kategori |
| Bildiren | Ozan Gündüz, QA mühendisi |
| Öncelik | Yüksek |
| Ortam | Yerel geliştirme; API http://localhost:5080, müşteri sitesi http://localhost:5173 (`npm run dev:storefront`), Chrome |
| Durum | Açık |

## Ne oldu

Üst menüden bir kategoriye (örneğin Kadın) girdiğimde sayfa takılıyor gibi. Kaydırmak ağırlaştı, dizüstü bilgisayarımın fanı çalışmaya başladı. Geliştirici araçlarında Network sekmesini açtığımda durmadan aynı istek akıyor; saniyede onlarca kez:

> `GET http://localhost:5080/api/products?category=kadin&sort=popular&page=1&pageSize=24`

Çoğu isteğin durumu "(canceled)" ya da çok kısa 200. Sayfadan başka bir sayfaya geçince istekler duruyor. API'nin terminali de aynı istekle doluyor.

Sıralamayı değiştirince (örneğin "Fiyat: artan") durum aynı, sadece istek adresindeki `sort` değişiyor.

## Tekrar etme adımları

1. API çalışsın: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
2. `cd frontend && npm run dev:storefront`
3. http://localhost:5173 adresini açın, F12 → Network sekmesini açık tutun.
4. Üst menüden bir kategoriye girin.
5. Sıralamayı değiştirin ve istekleri izlemeye devam edin.

## Beklenen

Kategoriye girince ürün listesi bir kez istenir. Sıralama ya da filtre değişince bir kez daha istenir.

## Gerçekleşen

İstek saniyede onlarca kez tekrarlanıyor, sayfa kasıyor.

## Ek bilgi

- Dün öğleden sonraki sürümde kategori sayfasında bu yoktu; liste hızlı açılıyordu.
- Ana sayfa ve ürün detay sayfasında sorun yok, yalnızca kategori (ve arama) sayfasında.
- Sayfa arka sekmedeyken de fan çalışmaya devam ediyor.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 9
```

Bu kontrol bir birim testi çalıştırır (`frontend/apps/storefront/src/__bolum__/fe02-kategori-sayfasi.test.tsx`): kategori sayfası açılınca ürün aramasının en fazla birkaç kez yapıldığını sınar.

---

## English

# TZ-4176 · Category page keeps sending requests

| Field | Value |
| --- | --- |
| Ticket | TZ-4176 |
| Level | ● (1/5) |
| Module | Storefront · Category |
| Reporter | Ozan Gündüz, QA engineer |
| Priority | High |
| Environment | Local development; API http://localhost:5080, storefront http://localhost:5173 (`npm run dev:storefront`), Chrome |
| Status | Open |

### What happened

When I enter a category from the top menu (for example Women), the page seems to hang. Scrolling got heavy and my laptop's fan started spinning. When I open the Network tab in developer tools, the same request keeps flowing, dozens of times per second:

> `GET http://localhost:5080/api/products?category=kadin&sort=popular&page=1&pageSize=24`

Most requests show the status "(canceled)" or a very short 200. When I navigate away to another page, the requests stop. The API terminal fills with the same request too.

Changing the sort order (for example "Fiyat: artan", "Price: ascending") behaves the same; only the `sort` in the request address changes.

### Steps to reproduce

1. Start the API: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
2. `cd frontend && npm run dev:storefront`
3. Open http://localhost:5173 and keep F12 → Network open.
4. Enter a category from the top menu.
5. Change the sort order and keep watching the requests.

### Expected

When entering a category, the product list is requested once. When the sort order or a filter changes, it is requested once more.

### Actual

The request repeats dozens of times per second, and the page lags.

### Additional information

- In yesterday afternoon's build the category page did not do this; the list opened quickly.
- The home page and the product detail page are fine; only the category (and search) page is affected.
- The fan keeps spinning even when the page is in a background tab.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 9
```

This check runs a unit test (`frontend/apps/storefront/src/__bolum__/fe02-kategori-sayfasi.test.tsx`): it verifies that the product search is called only a few times when the category page opens.
