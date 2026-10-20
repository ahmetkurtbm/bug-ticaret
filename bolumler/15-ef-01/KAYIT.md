# TZ-4476 · Yönetim sipariş listesi çok yavaş

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4476 |
| Seviye | ●● (2/5) |
| Modül | Yönetim · Siparişler |
| Bildiren | Hakan Polat, operasyon yöneticisi |
| Öncelik | Yüksek |
| Ortam | Test ortamı (veritabanı ayrı bir sunucuda). Yerelde: API **Development** ortamında (`--launch-profile http`) + `npm run dev:admin` |
| Durum | Açık |

## Ne oldu

Yönetim panelinde Siparişler sayfasını her açışımda sayfa "Yükleniyor" durumunda uzun süre bekliyor. Test ortamında 3-4 saniyeyi buluyor. Geçen haftaya kadar bu sayfa bir saniyeden kısa sürede açılıyordu. Hata mesajı yok, veriler sonunda geliyor ve doğru görünüyor; sadece yavaş.

Durum filtresini ya da sayfayı değiştirdiğimde de her seferinde aynı bekleme oluyor. Operasyon ekibi gün boyu bu sayfada çalıştığı için herkes şikâyetçi.

## Tekrar etme adımları

1. Altyapıyı başlatın ve örnek veriyi üretin (küçük profil yeterli): bkz. [Kurulum](../../docs/KURULUM.md).
2. API'yi Development ortamında başlatın: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
3. Başka bir terminalde: `cd frontend && npm run dev:admin`
4. http://localhost:5175 adresinde `admin@tezgah.test` / `Tezgah123!` ile giriş yapın.
5. Siparişler sayfasını açın (http://localhost:5175/siparisler).
6. Tarayıcı araçlarında Network sekmesinde siparişler isteğini seçin, **Timing** bölümüne bakın (`Server-Timing`).

## Beklenen

Sipariş listesi, sayfadaki sipariş sayısından bağımsız olarak hızlı açılır. Sayfa 5 siparişle de 50 siparişle de yaklaşık aynı sürede gelir.

## Gerçekleşen

Süre sayfadaki sipariş sayısıyla birlikte artıyor. Test ortamında 3-4 saniye.

## Ek bilgi

- Yerel makinemde (veritabanı aynı bilgisayarda) fark küçük kalıyor; asıl yavaşlık veritabanı ayrı sunucudaki ortamda belirgin. Bu yüzden yerelde bir şey görmek için yalnızca süreye bakmak yetmeyebilir. API Development ortamında her yanıta bir `Server-Timing` başlığı ekliyor.
- Sipariş sayısı geçen haftaya göre değişmedi. Sayfa boyutunu büyütünce (ör. 50) bekleme daha da uzuyor.
- Müşteri sitesindeki Siparişlerim sayfası etkilenmiyor.

## Bitirdiğinizde

API (Development ortamında) çalışırken şunu çalıştırın. Kontrol 50 siparişlik bir sayfa ister ve yanıttaki `Server-Timing` başlığından SQL sorgu sayısını okur; sorgu sayısı en fazla 5 olmalıdır:

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
| Module | Admin · Orders |
| Reporter | Hakan Polat, operations manager |
| Priority | High |
| Environment | Test environment (database on a separate server). Locally: API in the **Development** environment (`--launch-profile http`) + `npm run dev:admin` |
| Status | Open |

### What happened

Every time I open the Orders page in the admin panel, it stays in the "loading" state for a long time. In the test environment it takes 3-4 seconds. Until last week this page opened in under a second. There is no error message; the data arrives in the end and looks right; it is just slow.

Changing the status filter or the page is slow in the same way each time. The operations team works on this page all day, so everyone is complaining.

### Steps to reproduce

1. Start the infrastructure and generate the sample data (the small profile is enough): see [Setup](../../docs/KURULUM.md).
2. Start the API in the Development environment: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
3. In another terminal: `cd frontend && npm run dev:admin`
4. Open http://localhost:5175 and sign in with `admin@tezgah.test` / `Tezgah123!`.
5. Open the Orders page (http://localhost:5175/siparisler).
6. In the browser tools, select the orders request in the Network tab and look at the **Timing** section (`Server-Timing`).

### Expected

The order list opens quickly regardless of the number of orders on the page. It arrives in about the same time with 5 orders or 50 orders.

### Actual

The time grows with the number of orders on the page. 3-4 seconds in the test environment.

### Additional information

- On my own machine (database on the same computer) the difference stays small; the slowness is clear mainly where the database is on a separate server. So looking at time alone may not show anything locally. In the Development environment the API adds a `Server-Timing` header to every response.
- The number of orders did not change compared with last week. If I raise the page size (for example 50) the wait gets even longer.
- The My Orders page on the storefront is not affected.

### When you are done

Run this while the API (in the Development environment) is running. The check asks for a page of 50 orders and reads the SQL query count from the `Server-Timing` header; the count must be at most 5:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 15
```
