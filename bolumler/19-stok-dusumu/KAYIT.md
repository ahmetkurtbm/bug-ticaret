# TZ-4588 · Bazı siparişlerde stok iki kez düşmüş

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4588 |
| Seviye | ●●● (3/5) |
| Nerede görülüyor | Sipariş · Stok |
| Bildiren | Gizem Arslan, finans operasyonları ve mutabakat uzmanı |
| Öncelik | Kritik |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Günlük mutabakatta stok hareketlerini ödenen siparişlerle karşılaştırırken şunu gördüm: Bazı siparişler için stok, sipariş edilen adetin birkaç katı kadar düşmüş. Aynı siparişin sipariş geçmişinde "Ödendi" durumuna geçiş kaydı birden fazla kez var. Aynı sipariş numarası için stok hareketi satırı da birden fazla.

Bunun yanında destek hattına bir müşteri yazdı: Aynı siparişi için kendisine arka arkaya birkaç kez "Siparişiniz alındı" e-postası gelmiş.

Başka bir şey daha var: Aynı ürünü sepetinde bekleyen, henüz ödeme yapmamış müşterilerin siparişleri için ayrılmış stok da sanki azalmış. Bu müşteriler ödeme yaptıktan sonra stok tarafında tutarsızlık çıkacak diye endişeliyiz.

Mutabakat raporunda toplam tahsilat doğru; her sipariş için ödeme tek sefer yapılmış.

## Tekrar etme adımları

Sorun yoğun saatlerde görülüyor.

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. Altyapıyı başlatın ve örnek veriyi üretin: bkz. [Kurulum](../../docs/KURULUM.md).
3. O anki trafiği canlandırmak için kontrol aracını bir kez çalıştırın. Araç `musteri@tezgah.test` hesabı için 2 adetlik bir sipariş açar:

   ```bash
   dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 19
   ```
6. Siparişin durumuna, stok düşüşüne, stok hareketi ve "Ödendi" geçmiş kayıtlarının sayısına, diğer müşterilerin ayırdığı stoğa ve Mailpit'e (http://localhost:8025) düşen e-postalara bakın. Araç yalnızca stoğu eski hâline getirir; siparişin kayıtları ve gönderilen e-postalar yerinde kalır.

## Beklenen

Sipariş bir kez "Ödendi" olur. Stok yalnızca bir kez (sipariş edilen adet kadar) düşer. Tek bir stok hareketi, tek bir "Ödendi" geçmiş kaydı ve tek bir e-posta oluşur. Başka müşterilerin ayırdığı stok yerinde kalır.

## Gerçekleşen

Stok birden fazla kez düşüyor, birden fazla stok hareketi ve "Ödendi" kaydı oluşuyor, birden fazla e-posta çıkıyor. Başka müşterilerin ayırdığı stok azalıyor.

## Ek bilgi

- Sorun sakin zamanlarda değil, yoğun anlarda ortaya çıkıyor gibi.
- Mutabakat raporunda toplam tahsilat doğru; yalnızca stok ve e-posta tarafı yanlış.

## Bitirdiğinizde

API ve Ödeme servisi çalışırken şunu çalıştırın:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 19
```

---

## English

# TZ-4588 · Stock was deducted twice for some orders

| Field | Value |
| --- | --- |
| Ticket | TZ-4588 |
| Level | ●●● (3/5) |
| Where it shows | Order · Stock |
| Reporter | Gizem Arslan, finance operations and reconciliation specialist |
| Priority | Critical |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

While comparing stock movements with paid orders in the daily reconciliation I saw this: for some orders, stock went down by several times the ordered quantity. The order's status history has more than one transition to "Paid". There is also more than one stock movement row for the same order number.

In addition, a customer wrote to the support line: for the same order they received several "Siparişiniz alındı" ("Your order was received") emails one after another.

There is one more thing: the stock reserved for orders of customers who have the same product waiting for payment and have not paid yet also seems to have gone down. We are worried that stock will be inconsistent when those customers pay.

In the reconciliation report the total collected amount is right; the payment was made once for every order.

### Steps to reproduce

The problem shows at busy times.

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Start the infrastructure and generate the sample data: see [Setup](../../docs/KURULUM.md).
3. To recreate the traffic of that moment, run the check tool once. It opens an order of 2 units for the `musteri@tezgah.test` account:

   ```bash
   dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 19
   ```
6. Look at the order status, the stock decrease, the number of stock movement rows and "Paid" history records, the stock reserved by other customers, and the emails that arrive in Mailpit (http://localhost:8025). The tool only restores the stock; the order's records and the emails that were sent stay in place.

### Expected

The order becomes "Paid" once. Stock is deducted only once (by the ordered quantity). There is one stock movement, one "Paid" history record and one email. The stock reserved by other customers stays in place.

### Actual

Stock is deducted more than once, more than one stock movement and "Paid" record are created, more than one email goes out. The stock reserved by other customers goes down.

### Additional information

- The problem seems to appear at busy moments, not in quiet periods.
- In the reconciliation report the total collected amount is right; only the stock and email sides are wrong.

### When you are done

Run this while the API and the Payments service are running:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 19
```
