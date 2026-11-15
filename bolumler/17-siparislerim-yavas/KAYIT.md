# TZ-4520 · Siparişlerim sayfası saniyelerce bekliyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4520 |
| Seviye | ●●● (3/5) |
| Nerede görülüyor | Müşteri sitesi · Siparişlerim |
| Bildiren | Burcu Yalçın, müşteri başarı uzmanı |
| Öncelik | Yüksek |
| Ortam | Büyük veri içeren test ortamı (yaklaşık 1 milyon sipariş); yerelde `medium` veri profili, Chrome |
| Durum | Açık |

## Ne oldu

Çok sipariş vermiş müşterilerimizden şikâyet geliyor: "Siparişlerim" sayfasını açtıklarında sayfa uzun süre "Yükleniyor" durumunda kalıyor, sipariş listesi saniyeler sonra geliyor. Bizim ekipten biri sipariş geçmişi uzun olan bir test hesabıyla denedi, aynı şeyi gördü. Hata mesajı yok, sonunda doğru liste açılıyor.

Dikkat çeken şey: az siparişi olan hesaplarda sayfa hızlı. Küçük veriyle çalışan geliştirici ortamlarında da hızlı; orada bir sorun görünmüyor. Büyük veride ve çok siparişi olan kullanıcıda belirgin şekilde yavaş. Bu sayfa geçen hafta hızlıydı.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. Orta boy veriyi üretin (3-5 dakika sürer): `dotnet run --project backend/tools/Tezgah.DataGen -- --profile medium --reset`
3. http://localhost:5173 adresinde `akas.velioglu@posta.test` / `Tezgah123!` ile giriş yapın (şikâyet eden, çok siparişi olan müşteri; örnek veri hesaplarının şifresi hepsinde aynıdır) ve **Siparişlerim** sayfasını açın. Tarayıcıda Network sekmesinde sayfanın yüklenme süresine bakın.

`small` profilde fark çok küçük kalır; orta veri profiliyle (büyük veri) daha belirgindir.

## Beklenen

Sipariş geçmişi kaç sipariş olursa olsun hızlı açılır (onlarca milisaniye).

## Gerçekleşen

Çok siparişi olan müşteride, orta veri profilinde sayfa saniyeler sürüyor.

## Ek bilgi

- Sayfa geçen hafta bu veriyle hızlıydı.
- Diğer sayfalar etkilenmiyor; yalnızca müşterinin Siparişlerim sayfası yavaş.

## Bitirdiğinizde

Orta veri profiliyle çalışırken şunu çalıştırın:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 17
```

---

## English

# TZ-4520 · My Orders page takes seconds to load

| Field | Value |
| --- | --- |
| Ticket | TZ-4520 |
| Level | ●●● (3/5) |
| Where it shows | Storefront · My Orders |
| Reporter | Burcu Yalçın, customer success specialist |
| Priority | High |
| Environment | Test environment with large data (about 1 million orders); locally the `medium` data profile, Chrome |
| Status | Open |

### What happened

Customers who have placed many orders are complaining: when they open the "Siparişlerim" (My Orders) page it stays in the "loading" state for a long time and the order list arrives seconds later. Someone on our team tried with a test account with a long order history and saw the same thing. There is no error message; the correct list eventually opens.

The notable thing: for accounts with few orders the page is fast. It is also fast in developer environments that run with small data; nothing looks wrong there. It is clearly slow with large data and for users with many orders. The page was fast last week.

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Generate medium-size data (takes 3-5 minutes): `dotnet run --project backend/tools/Tezgah.DataGen -- --profile medium --reset`
3. Sign in at http://localhost:5173 with `akas.velioglu@posta.test` / `Tezgah123!` (the customer who complained, with many orders; all sample accounts share the same password) and open the **Siparişlerim** page. In the browser Network tab look at how long the page takes to load.

With the `small` profile the difference stays very small; it is clearer with the medium data profile (large data).

### Expected

The order history opens quickly (tens of milliseconds) however many orders there are.

### Actual

For a customer with many orders, on the medium data profile, the page takes seconds.

### Additional information

- The page was fast with this data last week.
- Other pages are not affected; only the customer's My Orders page is slow.

### When you are done

Run this while working with the medium data profile:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 17
```
