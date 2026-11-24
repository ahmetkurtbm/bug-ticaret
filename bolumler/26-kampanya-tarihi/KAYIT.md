# TZ-4688 · 1 Aralık'ı seçtim, kupon 30 Kasım'dan başlıyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4688 |
| Seviye | ●● (2/5) |
| Nerede görülüyor | Yönetim paneli · Kampanyalar |
| Bildiren | Merve Kılıç, kampanya uzmanı (pazarlama) |
| Öncelik | Orta |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Aralık kampanyası için yönetim panelinde yeni bir kupon oluşturdum. Başlangıç gününü 1 Aralık, bitiş gününü 31 Aralık seçtim ve kaydettim. Kampanyalar listesinde ise kuponun tarihleri "30 Kasım – 30 Aralık" olarak görünüyor.

Yani kupon seçtiğimden bir gün önce açılıyor ve bir gün önce kapanıyor. Fark etmeseydim kampanyanın son günü müşteriler kuponu kullanamayacaktı; bülten metnini de "1 – 31 Aralık" diye hazırlamıştım.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. http://localhost:5175 adresinde `admin@tezgah.test` / `Tezgah123!` ile giriş yapın.
3. Kampanyalar sayfasında (http://localhost:5175/kampanyalar) **Yeni kupon**'a basın.
4. Başlangıç gününü 1 Aralık, bitiş gününü 31 Aralık seçin; diğer alanları geçerli değerlerle doldurup kaydedin.
5. Kupon listesinde yeni kuponun tarihlerine bakın.

## Beklenen

Kuponun tarihleri listede "1 Aralık – 31 Aralık" olarak görünür.

## Gerçekleşen

Liste "30 Kasım – 30 Aralık" gösteriyor; her iki gün de bir gün geriye kaymış.

## Ek bilgi

- Aynı kaymayı hem başlangıç hem bitiş gününde görüyorum; ayın hangi günü olduğu fark etmiyor.
- Tarihi seçerken formda doğru gün görünüyor; kayma kaydettikten sonra listede ortaya çıkıyor.
- İngiltere'deki tarayıcısıyla aynı kuponu oluşturan bir iş arkadaşımda sorun yok; kendi seçtiği günler listede aynen görünüyor.
- Geçen ay oluşturduğum kuponlarda tarihler doğruydu; sorun son zamanlarda başladı.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 26
```

---

## English

# TZ-4688 · I picked December 1, the coupon starts on November 30

| Field | Value |
| --- | --- |
| Ticket | TZ-4688 |
| Level | ●● (2/5) |
| Where it shows | Admin panel · Campaigns |
| Reporter | Merve Kılıç, campaign specialist (marketing) |
| Priority | Medium |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

I created a new coupon in the admin panel for the December campaign. I picked December 1 as the start day and December 31 as the end day and saved it. In the Campaigns list, however, the coupon's dates show as "30 Kasım – 30 Aralık" ("30 November – 30 December").

So the coupon opens a day before what I picked and closes a day early. If I had not noticed, customers would not have been able to use the coupon on the last day of the campaign; I had also prepared the newsletter text as "1 – 31 December".

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Open http://localhost:5175 and sign in with `admin@tezgah.test` / `Tezgah123!`.
3. On the Campaigns page (http://localhost:5175/kampanyalar) press **Yeni kupon** ("New coupon").
4. Pick December 1 as the start day and December 31 as the end day; fill the other fields with valid values and save.
5. Look at the dates of the new coupon in the coupon list.

### Expected

The coupon's dates appear in the list as "1 Aralık – 31 Aralık" ("1 December – 31 December").

### Actual

The list shows "30 Kasım – 30 Aralık"; both days have moved back by one day.

### Additional information

- I see the same shift on both the start and the end day; it does not depend on which day of the month it is.
- While picking, the form shows the right day; the shift appears in the list after saving.
- A colleague whose browser is in the United Kingdom created the same coupon and has no problem; the days they pick appear in the list unchanged.
- The coupons I created last month had correct dates; the problem started recently.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 26
```
