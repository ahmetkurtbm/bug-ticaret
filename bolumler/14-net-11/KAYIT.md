# TZ-4437 · Gece yarısı başlaması gereken kampanya 03:00'te başladı

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4437 |
| Seviye | ●● (2/5) |
| Modül | Yönetim · Kampanya |
| Bildiren | Aylin Kaya, kampanya yöneticisi (pazarlama) |
| Öncelik | Yüksek |
| Ortam | Yerel kurulum: API + `npm run dev:admin`, yönetim paneli http://localhost:5175 |
| Durum | Açık |

## Ne oldu

Hafta sonu kampanyamız için panelden bir kupon oluşturdum: başlangıç günü Cumartesi, bitiş günü Pazar. Planımız Cumartesi 00:00'da açılıp Pazar 23:59'da kapanmaktı. Cumartesi gece yarısı e-posta bültenini gönderdik, müşteriler kodu hemen denedi.

Sepette şu mesajı aldılar:

> Bu kupon henüz kullanıma açılmadı.

Kupon ancak sabaha karşı 03:00 civarında çalışmaya başladı. Panelde de kuponun durumu o saate kadar "Planlandı" gösterdi. Bültenin ilk üç saatlik tıklamaları boşa gitti.

Bir de bitişte aynı şey var gibi: Pazar gecesi 23:59'dan sonra da bir süre kod çalışmaya devam etti. Kupon listesinde bitiş gününü de bir gün sonrası olarak görüyorum.

## Tekrar etme adımları

1. API'yi başlatın: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
2. Başka bir terminalde: `cd frontend && npm run dev:admin`
3. http://localhost:5175 adresinde `admin@tezgah.test` / `Tezgah123!` ile giriş yapın.
4. Kampanyalar sayfasında (http://localhost:5175/kampanyalar) **Yeni kupon** ile yarın başlayan, iki gün süren bir kupon oluşturun (örneğin kod: `DENEME1`).
5. Kupon listesine bakın. Tarayıcıda Network sekmesinde kupon oluşturma yanıtındaki `startsAt` ve `endsAt` değerlerini de okuyun.

## Beklenen

Kupon, seçilen başlangıç gününün İstanbul saatiyle 00:00'ında başlar ve bitiş gününün sonunda (ertesi gün 00:00) biter. Listede bitiş günü, girdiğim bitiş günüdür.

## Gerçekleşen

Kupon, İstanbul saatiyle 03:00'te başlıyor ve ertesi gün 03:00'te bitiyor. Listede bitiş günü bir gün sonrası görünüyor.

## Ek bilgi

- Müşterilerin mesajı kod doğru yazıldığı hâlde çıktı; kupon kodunda sorun yok.
- Veritabanı sunucusumuz ve uygulama sunucumuz UTC'de çalışıyor, yönetim paneli ise saatleri İstanbul saatiyle gösteriyor.
- Geçen ay kampanyalarda böyle bir şey fark etmemiştik. Geçen haftaki sürümden sonra başladı gibi.
- Önceki kampanyalar için elimizde kayıt yok; yalnızca bu hafta oluşturulanları kontrol ettim.

## Bitirdiğinizde

API çalışırken şunu çalıştırın. Kontrol yedi gün sonra başlayan bir kupon oluşturur, UTC değerlerini İstanbul gün sınırlarıyla karşılaştırır ve kuponu pasifleştirir:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 14
```

---

## English

# TZ-4437 · Midnight campaign started at 3 AM

| Field | Value |
| --- | --- |
| Ticket | TZ-4437 |
| Level | ●● (2/5) |
| Module | Admin · Campaign |
| Reporter | Aylin Kaya, campaign manager (marketing) |
| Priority | High |
| Environment | Local setup: API + `npm run dev:admin`, admin panel at http://localhost:5175 |
| Status | Open |

### What happened

I created a coupon in the panel for our weekend campaign: start day Saturday, end day Sunday. The plan was to open at Saturday 00:00 and close at Sunday 23:59. At midnight on Saturday we sent the email newsletter and customers tried the code right away.

In the cart they got this message (in Turkish):

> Bu kupon henüz kullanıma açılmadı. ("This coupon is not open for use yet.")

The coupon only started working around 03:00 in the morning. The panel also showed the coupon status as "Planlandı" (scheduled) until then. The first three hours of newsletter clicks were wasted.

The end seems to have the same problem: after 23:59 on Sunday the code kept working for a while. In the coupon list I also see the end day as one day later.

### Steps to reproduce

1. Start the API: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
2. In another terminal: `cd frontend && npm run dev:admin`
3. Open http://localhost:5175 and sign in with `admin@tezgah.test` / `Tezgah123!`.
4. On the Campaigns page (http://localhost:5175/kampanyalar), use **Yeni kupon** to create a coupon that starts tomorrow and lasts two days (for example code `DENEME1`).
5. Look at the coupon list. In the browser Network tab also read `startsAt` and `endsAt` in the create response.

### Expected

The coupon starts at 00:00 Istanbul time on the chosen start day and ends at the end of the end day (00:00 the next day). In the list the end day is the end day I entered.

### Actual

The coupon starts at 03:00 Istanbul time and ends at 03:00 the next day. In the list the end day shows one day later.

### Additional information

- The customers' message appeared although the code was typed correctly; there is nothing wrong with the coupon code.
- Our database server and application server run in UTC, while the admin panel shows times in Istanbul time.
- We did not notice anything like this in last month's campaigns. It seems to have started after last week's release.
- We have no record of earlier campaigns; I only checked the ones created this week.

### When you are done

Run this while the API is running. The check creates a coupon that starts in seven days, compares its UTC values with the Istanbul day boundaries, and deactivates the coupon:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 14
```
