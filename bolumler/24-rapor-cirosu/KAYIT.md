# TZ-4655 · Rapordaki ciro muhasebeyle tutmuyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4655 |
| Seviye | ●● (2/5) |
| Nerede görülüyor | Yönetim paneli · Gösterge |
| Bildiren | Nazlı Erdoğan, finans ve muhasebe |
| Öncelik | Yüksek |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Ay sonu mutabakatında yönetim panelindeki Gösterge sayfasının cirosunu muhasebe kayıtlarımızla karşılaştırdım. Aynı tarih aralığı için panelin gösterdiği ciro, bizim kapattığımız tutardan belirgin biçimde yüksek. "Sipariş" kutusundaki adet ise muhasebedeki sipariş sayısıyla birebir aynı. Yani sipariş sayısında fark yok, tutarda var.

"Ortalama sepet" kutusu da gözüme yüksek geldi: bizim hesapladığımız ortalamadan fazla. Geçen ay panel ile muhasebe tutuyordu; bu ay tutmuyor. Rakamlar bu hâliyle yönetime sunulamaz.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. http://localhost:5175 adresinde `admin@tezgah.test` / `Tezgah123!` ile giriş yapın.
3. Gösterge sayfasında "Son 30 gün" aralığını seçin; "Sipariş", "Ciro" ve "Ortalama sepet" kutularındaki değerleri not alın.
4. Aynı aralık için ödenmiş siparişlerin toplam tutarını muhasebe rakamı olarak kabul edip karşılaştırın. `kontrol 24` aracı bu karşılaştırmayı sizin için yapar ve iki rakamı yan yana yazar.

## Beklenen

Panelin cirosu, aynı aralıktaki ödenmiş siparişlerin toplam tutarıyla aynıdır; ortalama sepet de ciro ile sipariş sayısıyla tutarlıdır.

## Gerçekleşen

Sipariş sayısı muhasebeyle aynı, ama ciro ve ortalama sepet muhasebenin rakamından yüksek.

## Ek bilgi

- Sipariş adedi doğru; yalnızca tutar tarafı fazla.
- Fark küçük bir sapma değil; her aralıkta ciroyu aynı yönde büyütüyor.
- Geçen ayın raporu muhasebeyle tutuyordu; bu ay tutmaması geçen haftadan sonra başladı gibi.
- Günlük satış grafiği ve satıcı tablosu ayrı hesaplanıyor gibi; onlarda fark görmedim.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 24
```

---

## English

# TZ-4655 · Report revenue does not match accounting

| Field | Value |
| --- | --- |
| Ticket | TZ-4655 |
| Level | ●● (2/5) |
| Where it shows | Admin panel · Dashboard |
| Reporter | Nazlı Erdoğan, finance and accounting |
| Priority | High |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

In the month-end reconciliation I compared the revenue on the admin panel's Dashboard page with our accounting records. For the same date range, the revenue the panel shows is clearly higher than the amount we closed. The count in the "Sipariş" ("Orders") box is exactly the same as the order count in accounting. So there is no difference in the order count, but there is in the amount.

The "Ortalama sepet" ("Average basket") box also looked high to me: more than the average we calculate. Last month the panel and accounting matched; this month they do not. These numbers cannot be presented to management as they are.

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Open http://localhost:5175 and sign in with `admin@tezgah.test` / `Tezgah123!`.
3. On the Dashboard page choose the "Son 30 gün" ("Last 30 days") range; note the values in the "Sipariş", "Ciro" ("Revenue") and "Ortalama sepet" boxes.
4. Take the total amount of paid orders in the same range as the accounting figure and compare. The `kontrol 24` tool does this comparison for you and prints the two figures side by side.

### Expected

The panel's revenue equals the total amount of paid orders in the same range; the average basket is also consistent with revenue and order count.

### Actual

The order count is the same as accounting, but revenue and average basket are higher than accounting's figures.

### Additional information

- The order count is right; only the amount side is too high.
- The difference is not a small deviation; it enlarges revenue in the same direction in every range.
- Last month's report matched accounting; the mismatch seems to have started after last week.
- The daily sales chart and the seller table seem to be calculated separately; I saw no difference there.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 24
```
