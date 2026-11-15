# TZ-4491 · Kampanya geçmişi bir süredir yanlış gösteriliyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4491 |
| Seviye | ●● (2/5) |
| Nerede görülüyor | Yönetim paneli · Kampanyalar |
| Bildiren | Sevgi Tunç, kampanya analisti |
| Öncelik | Orta |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Kampanya geçmişi ekranına her ay kuponlarda kimin ne yaptığını kontrol etmek için bakıyorum. İki hafta kadar önce bu ekran doğruydu: en son yapılan işlem en üstte, sonra geriye doğru gidiyordu. Bugün açtığımda listenin en üstünde aylar öncesinin kayıtları var. Dün pasifleştirdiğim kuponun işlemini listede bulamıyorum.

Hata mesajı yok, ekran normal açılıyor, kayıtlar dolu. Ama sıralama tersine dönmüş gibi: en eski işlemden başlıyor. Kayıt sayısı artınca durum daha da kötü; en yeni işlemler hiç görünmüyor.

Bu ekran hep böyleydi diyenler var, ben öyle olmadığını biliyorum.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. Altyapıyı başlatın ve örnek veriyi üretin: bkz. [Kurulum](../../docs/KURULUM.md).
3. http://localhost:5175 adresinde `admin@tezgah.test` / `Tezgah123!` ile giriş yapın.
4. Kampanyalar sayfasında (http://localhost:5175/kampanyalar) yeni bir kupon oluşturup birkaç kez pasifleştirin ve etkinleştirin.
5. Aşağı inip **Kampanya geçmişi** tablosuna bakın.

## Beklenen

Tablonun en üstünde en son yapılan işlem (az önce pasifleştirdiğim kupon) olur; aşağı indikçe daha eski kayıtlar gelir.

## Gerçekleşen

Tablonun en üstünde en eski kayıt var, aşağı indikçe yeniye gidiyor. Kayıt sayısı çoğalınca en yeni işlemler listede hiç yok.

## Ek bilgi

- İki hafta kadar önce aynı ekran doğruydu.
- Kupon listesi (üstteki tablo) normal.
- Kayıtların kendisi doğru görünüyor: kim, ne zaman, hangi kupon. Yalnızca sıra ve hangilerinin göründüğü yanlış.

## Bitirdiğinizde

API çalışırken şunu çalıştırın:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 16
```

---

## English

# TZ-4491 · Campaign history has been wrong for a while

| Field | Value |
| --- | --- |
| Ticket | TZ-4491 |
| Level | ●● (2/5) |
| Where it shows | Admin panel · Campaigns |
| Reporter | Sevgi Tunç, campaign analyst |
| Priority | Medium |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

I look at the campaign history screen every month to check who did what on coupons. About two weeks ago this screen was correct: the latest action at the top, going back in time. Today when I opened it, the top of the list shows records from months ago. I cannot find the action for the coupon I deactivated yesterday.

There is no error message, the screen opens normally and the records are there. But the order looks reversed: it starts with the oldest action. The more records there are, the worse it gets; the newest actions do not appear at all.

Some people say it was always like this; I know it was not.

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Start the infrastructure and generate the sample data: see [Setup](../../docs/KURULUM.md).
3. Open http://localhost:5175 and sign in with `admin@tezgah.test` / `Tezgah123!`.
4. On the Campaigns page (http://localhost:5175/kampanyalar), create a new coupon and deactivate and activate it a few times.
5. Scroll down to the **Kampanya geçmişi** (campaign history) table.

### Expected

The top of the table shows the latest action (the coupon I just deactivated); older records follow as you scroll down.

### Actual

The top of the table shows the oldest record and it goes toward newer ones. When there are many records, the newest actions are missing from the list entirely.

### Additional information

- About two weeks ago the same screen was correct.
- The coupon list (the table above) is normal.
- The records themselves look right: who, when, which coupon. Only the order and which ones appear are wrong.

### When you are done

Run this while the API is running:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 16
```
