# TZ-4588 · Bazı siparişlerde stok iki kez düşmüş

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4588 |
| Seviye | ●●● (3/5) |
| Modül | Ödeme webhook'u |
| Bildiren | Gizem Arslan, finans operasyonları ve mutabakat uzmanı |
| Öncelik | Kritik |
| Ortam | Test ortamı; yerelde API (`--launch-profile http`) + Ödeme servisi (`--launch-profile http`) + Bildirim servisi, e-postalar http://localhost:8025 (Mailpit) |
| Durum | Açık |

## Ne oldu

Günlük mutabakatta stok hareketlerini ödenen siparişlerle karşılaştırırken şunu gördüm: Bazı siparişler için stok, sipariş edilen adetin birkaç katı kadar düşmüş. Aynı siparişin sipariş geçmişinde "Ödendi" durumuna geçiş kaydı birden fazla kez var. Aynı sipariş numarası için stok hareketi satırı da birden fazla.

Bunun yanında destek hattına bir müşteri yazdı: Aynı siparişi için kendisine arka arkaya birkaç kez "Siparişiniz alındı" e-postası gelmiş.

Başka bir şey daha var: Aynı ürünü sepetinde bekleyen, henüz ödeme yapmamış müşterilerin siparişleri için ayrılmış stok da sanki azalmış. Bu müşteriler ödeme yaptıktan sonra stok tarafında tutarsızlık çıkacak diye endişeliyiz.

Ödeme sağlayıcı tarafında bir sorun görünmüyor; her sipariş için ödeme tek sefer yapılmış.

## Tekrar etme adımları

1. Altyapıyı başlatın ve örnek veriyi üretin: bkz. [Kurulum](../../docs/KURULUM.md).
2. API'yi başlatın: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
3. Ödeme servisini başlatın: `dotnet run --project backend/src/Tezgah.Payments --launch-profile http`
4. Bildirim servisini de başlatırsanız e-postaları görebilirsiniz: `dotnet run --project backend/src/Tezgah.Notifications --launch-profile http`
5. Kontrol aracını çalıştırın. Araç `musteri@tezgah.test` için 2 adetlik bir sipariş verir, aynı ürünü bekleyen başka müşterileri taklit etmek için 10 adet rezervasyon ekler ve ödeme servisinin gönderdiği aynı ödeme olayını (aynı olay kimliği) aynı anda 5 kez API'ye gönderir:

   ```bash
   dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 19
   ```
6. Çıktıda siparişin durumuna, stok düşüşüne, stok hareketi ve "ödendi" kaydı sayılarına ve diğer müşterilerin rezervasyonuna bakın. Araç yalnızca stoğu eski hâline getirir; siparişin kayıtları (stok hareketleri, geçmiş) ve gönderilen e-postalar yerinde kalır. Bunları veritabanında ve Mailpit'te inceleyebilirsiniz.

## Beklenen

Aynı ödeme olayı kaç kez gelirse gelsin, sipariş bir kez "Ödendi" olur. Stok yalnızca bir kez (sipariş edilen adet kadar) düşer. Tek bir stok hareketi, tek bir "Ödendi" geçmiş kaydı ve tek bir e-posta oluşur. Başka müşterilerin ayırdığı stok yerinde kalır. Tekrarlanan olaylara API 200 döner (gönderen yeniden denemeyi bırakır).

## Gerçekleşen

Stok birden fazla kez düşüyor, birden fazla stok hareketi ve "Ödendi" kaydı oluşuyor, birden fazla e-posta çıkıyor. Başka müşterilerin ayırdığı stok azalıyor.

## Ek bilgi

- Ödeme ekibi, ağ yavaşladığında aynı bildirimi kısa aralıklarla birkaç kez gönderebildiklerini söylüyor ("en az bir kez" teslim). Bu normal; tekrarın sorun olmaması gerekiyor.
- Sorun bildirimler aralarında zaman varken geldiğinde değil, birbirine çok yakın (aynı saniye içinde) geldiğinde ortaya çıkıyor gibi. Ödeme ekibi, sakin zamanlarda aynı bildirimi yeniden gönderip denediklerinde sorun görmediklerini söylüyor.
- Bazı durumlarda API 500 döndürüyor ve ödeme servisi aynı bildirimi yeniden gönderiyor; bu durumda sonunda tek kayıt kalıyor gibi görünüyor. Ne zaman olduğunu çıkaramadım.
- Mutabakat raporunda toplam tahsilat doğru; yalnızca stok ve bildirim tarafı yanlış.

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
| Module | Payment webhook |
| Reporter | Gizem Arslan, finance operations and reconciliation specialist |
| Priority | Critical |
| Environment | Test environment; locally the API (`--launch-profile http`) + Payments service (`--launch-profile http`) + Notifications service, emails at http://localhost:8025 (Mailpit) |
| Status | Open |

### What happened

While comparing stock movements with paid orders in the daily reconciliation I saw this: for some orders, stock went down by several times the ordered quantity. The order's status history has more than one transition to "Paid". There is also more than one stock movement row for the same order number.

In addition, a customer wrote to the support line: for the same order they received several "Siparişiniz alındı" ("Your order was received") emails one after another.

There is one more thing: the stock reserved for orders of customers who have the same product waiting for payment and have not paid yet also seems to have gone down. We are worried that stock will be inconsistent when those customers pay.

There seems to be no problem on the payment provider side; for every order the payment was made once.

### Steps to reproduce

1. Start the infrastructure and generate the sample data: see [Setup](../../docs/KURULUM.md).
2. Start the API: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
3. Start the Payments service: `dotnet run --project backend/src/Tezgah.Payments --launch-profile http`
4. If you also start the Notifications service you can see the emails: `dotnet run --project backend/src/Tezgah.Notifications --launch-profile http`
5. Run the check tool. It places an order of 2 units for `musteri@tezgah.test`, adds 10 units of reservation to imitate other customers waiting on the same product, and sends the same payment event the payment service would send (the same event id) to the API 5 times at the same moment:

   ```bash
   dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 19
   ```
6. In the output look at the order status, the stock decrease, the number of stock movements and "paid" records, and the other customers' reservation. The tool only restores the stock; the order's records (stock movements, history) and the emails that were sent stay in place. You can inspect them in the database and in Mailpit.

### Expected

However many times the same payment event arrives, the order becomes "Paid" once. Stock is deducted only once (by the ordered quantity). There is one stock movement, one "Paid" history record and one email. The stock reserved by other customers stays in place. For repeated events the API returns 200 (the sender stops retrying).

### Actual

Stock is deducted more than once, more than one stock movement and "Paid" record are created, more than one email goes out. The stock reserved by other customers goes down.

### Additional information

- The payments team says that when the network slows down they may send the same notification several times at short intervals ("at least once" delivery). This is normal; a repeat should not be a problem.
- The problem seems to appear not when notifications arrive with time between them, but when they arrive very close to each other (within the same second). The payments team says that when they resend the same notification in quiet periods they see no problem.
- In some cases the API returns 500 and the payment service sends the same notification again; in that case a single record seems to remain in the end. I could not work out when this happens.
- In the reconciliation report the total collected amount is right; only the stock and notification sides are wrong.

### When you are done

Run this while the API and the Payments service are running:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 19
```
