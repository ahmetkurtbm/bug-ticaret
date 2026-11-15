# TZ-4176 · Kategori sayfası durmadan istek atıyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4176 |
| Seviye | ● (1/5) |
| Nerede görülüyor | Müşteri sitesi · Kategori sayfası |
| Bildiren | Ozan Gündüz, QA mühendisi |
| Öncelik | Yüksek |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Üst menüden bir kategoriye (örneğin Kadın) girdiğimde sayfa takılıyor gibi. Kaydırmak ağırlaştı, dizüstü bilgisayarımın fanı çalışmaya başladı. Geliştirici araçlarında Network sekmesini açtığımda aynı ürün listesi isteği durmadan tekrarlanıyor; saniyede onlarca kez. API'nin terminali de aynı istekle doluyor.

Sıralamayı değiştirince (örneğin "Fiyat: artan") durum aynı.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. http://localhost:5173 adresini açın, F12 → Network sekmesini açık tutun.
3. Üst menüden bir kategoriye girin.
4. Sıralamayı değiştirin ve istekleri izlemeye devam edin.

## Beklenen

Kategoriye girince ürün listesi bir kez istenir. Sıralama ya da filtre değişince bir kez daha istenir.

## Gerçekleşen

İstek saniyede onlarca kez tekrarlanıyor, sayfa kasıyor.

## Ek bilgi

- Dün öğleden sonraki sürümde kategori sayfasında bu yoktu; liste hızlı açılıyordu.
- Ana sayfa ve ürün sayfasında sorun yok, yalnızca kategori (ve arama) sayfasında.
- Sayfa arka sekmedeyken de fan çalışmaya devam ediyor.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 9
```

---

## English

# TZ-4176 · Category page keeps sending requests

| Field | Value |
| --- | --- |
| Ticket | TZ-4176 |
| Level | ● (1/5) |
| Where it shows | Storefront · Category page |
| Reporter | Ozan Gündüz, QA engineer |
| Priority | High |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

When I enter a category from the top menu (for example Women), the page seems to hang. Scrolling got heavy and my laptop's fan started spinning. When I open the Network tab in developer tools, the same product list request keeps repeating, dozens of times per second. The API terminal fills with the same request too.

Changing the sort order (for example "Fiyat: artan", "Price: ascending") behaves the same.

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Open http://localhost:5173 and keep F12 → Network open.
3. Enter a category from the top menu.
4. Change the sort order and keep watching the requests.

### Expected

When entering a category, the product list is requested once. When the sort order or a filter changes, it is requested once more.

### Actual

The request repeats dozens of times per second, and the page lags.

### Additional information

- In yesterday afternoon's build the category page did not do this; the list opened quickly.
- The home page and the product page are fine; only the category (and search) page is affected.
- The fan keeps spinning even when the page is in a background tab.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 9
```
