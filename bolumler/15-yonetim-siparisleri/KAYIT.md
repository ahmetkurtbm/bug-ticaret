# TZ-4476 · Yönetim sipariş listesi çok yavaş

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4476 |
| Seviye | ●● (2/5) |
| Nerede görülüyor | Yönetim paneli · Siparişler |
| Bildiren | Hakan Polat, operasyon yöneticisi |
| Öncelik | Yüksek |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Yönetim panelinde Siparişler sayfasını her açışımda sayfa "Yükleniyor" durumunda uzun süre bekliyor. Test ortamında 3-4 saniyeyi buluyor. Geçen haftaya kadar bu sayfa bir saniyeden kısa sürede açılıyordu. Hata mesajı yok, veriler sonunda geliyor ve doğru görünüyor; sadece yavaş.

Durum filtresini ya da sayfayı değiştirdiğimde de her seferinde aynı bekleme oluyor. Operasyon ekibi gün boyu bu sayfada çalıştığı için herkes şikâyetçi.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. Altyapıyı başlatın ve örnek veriyi üretin (küçük profil yeterli): bkz. [Kurulum](../../docs/KURULUM.md).
3. http://localhost:5175 adresinde `admin@tezgah.test` / `Tezgah123!` ile giriş yapın.
4. Siparişler sayfasını açın (http://localhost:5175/siparisler); sayfa başına satır sayısını artırıp bekleme süresini karşılaştırın.

## Beklenen

Sipariş listesi, sayfada kaç sipariş olursa olsun hızlı açılır; geçen haftaki gibi bir saniyenin altında.

## Gerçekleşen

Sayfa başına satır arttıkça bekleme uzuyor. Test ortamında 3-4 saniye.

## Ek bilgi

- Yerel makinemde (veritabanı aynı bilgisayarda) fark küçük kalıyor; asıl yavaşlık veritabanı ayrı sunucudaki ortamda belirgin.
- Sipariş sayısı geçen haftaya göre değişmedi. Sayfa boyutunu büyütünce (ör. 50) bekleme daha da uzuyor.
- Müşteri sitesindeki Siparişlerim sayfası etkilenmiyor.

## Bitirdiğinizde

API (Development ortamında) çalışırken şunu çalıştırın:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 15
```

---

## English

# TZ-4476 · Admin order list is very slow

| Field | Value |
| --- | --- |
| Ticket | TZ-4476 |
| Level | ●● (2/5) |
| Where it shows | Admin panel · Orders |
| Reporter | Hakan Polat, operations manager |
| Priority | High |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

Every time I open the Orders page in the admin panel, it stays in the "loading" state for a long time. In the test environment it takes 3-4 seconds. Until last week this page opened in under a second. There is no error message; the data arrives in the end and looks right; it is just slow.

Changing the status filter or the page is slow in the same way each time. The operations team works on this page all day, so everyone is complaining.

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Start the infrastructure and generate the sample data (the small profile is enough): see [Setup](../../docs/KURULUM.md).
3. Open http://localhost:5175 and sign in with `admin@tezgah.test` / `Tezgah123!`.
4. Open the Orders page (http://localhost:5175/siparisler); increase the rows per page and compare the waiting time.

### Expected

The order list opens quickly however many orders are on the page; under a second, as it did last week.

### Actual

The wait grows as the rows per page increase. 3-4 seconds in the test environment.

### Additional information

- On my own machine (database on the same computer) the difference stays small; the slowness is clear mainly where the database is on a separate server.
- The number of orders did not change compared with last week. If I raise the page size (for example 50) the wait gets even longer.
- The My Orders page on the storefront is not affected.

### When you are done

Run this while the API (in the Development environment) is running:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 15
```
