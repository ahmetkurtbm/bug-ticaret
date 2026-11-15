# TZ-4127 · Docker'da ödeme başlamıyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4127 |
| Seviye | ● (1/5) |
| Nerede görülüyor | Müşteri sitesi · Ödeme |
| Bildiren | Nilay Öztürk, QA mühendisi |
| Öncelik | Yüksek |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Sipariş verip ödemeye geçmeye çalışıyorum. Ödeme sayfası açılmıyor, ekranda şu yazıyor:

> Ödeme şu anda başlatılamadı. Siparişiniz kaydedildi; birkaç dakika sonra Siparişlerim sayfasından tekrar deneyin.

Sipariş Siparişlerim'de görünüyor ama ödeme adımına bir türlü geçemiyorum. Birkaç dakika bekleyip tekrar denedim, aynı sonuç.

## Tekrar etme adımları

1. Yerelde çalışan servisleri ve `npm run dev` pencerelerini kapatın.
2. Depo kökünde: `docker compose --profile app up -d --build`
3. `docker compose --profile app ps` çıktısında bütün servislerin `running` / `healthy` olmasını bekleyin. Örnek veri hazır olunca http://localhost:5173 açılır.
4. `musteri@tezgah.test` / `Tezgah123!` ile giriş yapın.
5. Herhangi bir ürünü sepete ekleyin, sepetten adres seçip siparişi onaylayın.

## Beklenen

Sipariş oluşur ve ödeme (3D onay) sayfasına yönlendirilirim.

## Gerçekleşen

Sipariş oluşuyor ama ödeme sayfasına yönlendirme yok; yukarıdaki mesaj çıkıyor.

## Ek bilgi

- Geçen hafta aynı komutla bu akış çalışıyordu.
- Aynı sürümü kendi makinemde servisleri tek tek `dotnet run` ile açınca (altyapı Docker'da) sipariş ve ödeme sorunsuz geçiyor.

## Bitirdiğinizde

Sistem container'da çalışırken (aynı `--profile app` ile) şunu çalıştırın:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 1
```

---

## English

# TZ-4127 · Payment fails only in Docker

| Field | Value |
| --- | --- |
| Ticket | TZ-4127 |
| Level | ● (1/5) |
| Where it shows | Storefront · Payment |
| Reporter | Nilay Öztürk, QA engineer |
| Priority | High |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

I place an order and try to go to payment. The payment page does not open. The screen says (in Turkish):

> Ödeme şu anda başlatılamadı. Siparişiniz kaydedildi; birkaç dakika sonra Siparişlerim sayfasından tekrar deneyin.
> ("Payment could not be started right now. Your order was saved; try again from My Orders in a few minutes.")

The order shows up in My Orders, but I cannot get to the payment step. I waited a few minutes and tried again: same result.

### Steps to reproduce

1. Stop locally running services and any `npm run dev` windows.
2. From the repository root: `docker compose --profile app up -d --build`
3. Wait until every service shows `running` / `healthy` in `docker compose --profile app ps`. When the sample data is ready, http://localhost:5173 opens.
4. Log in as `musteri@tezgah.test` / `Tezgah123!`.
5. Add any product to the cart, choose an address in the cart and confirm the order.

### Expected

The order is created and I am redirected to the payment (3D confirmation) page.

### Actual

The order is created, but there is no redirect to payment; the message above appears.

### Additional information

- This flow worked last week with the same command.
- When I run the same version on my machine with the services started one by one using `dotnet run` (infrastructure in Docker), ordering and payment work fine.

### When you are done

With the system running in containers (same `--profile app`), run:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 1
```
