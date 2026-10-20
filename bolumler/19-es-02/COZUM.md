# Çözüm · Bölüm 19 (EŞ-02) · Ödeme bildirimi iki kez geliyor (idempotency)

## Kök neden

Ödeme servisi webhook olaylarını **en az bir kez** gönderir (`docs/MIMARI.md`): ağ yavaşlarsa ya da cevap kaybolursa aynı olay, aynı `EventId` ile tekrar gelir. API'nin görevi bir olayı kaç kez alırsa alsın etkisini bir kez uygulamaktır (idempotent tüketici).

`backend/src/Tezgah.Application/Payments/PaymentWebhookService.cs` içindeki `HandleAsync` tekrarı artık şöyle anlıyor:

```csharp
var order   = await db.Orders.Include(o => o.Items).SingleOrDefaultAsync(o => o.Id == evt.OrderId, ct);
var payment = await db.Payments.SingleOrDefaultAsync(p => p.ProviderPaymentId == providerId, ct);
...
// Ödeme daha önce sonuçlandıysa aynı olayın tekrarıdır.
if (payment is { Status: PaymentStatus.Succeeded or PaymentStatus.Failed })
    return WebhookResult.Duplicate;
...
if (order.Status == OrderStatus.PendingPayment && order.GrandTotal == evt.Amount)   // bellekteki değer
{
    order.Status = OrderStatus.Paid;
    order.PaidAt = evt.OccurredAt;
    db.OrderStatusHistory.Add(...);
    await OrderStockTransitions.CommitSaleAsync(db, order, now, ct);   // stoğu düşer
    ... Outbox.Add(db, paidEvent.EventId, EventTopology.OrderPaid, ...);  // "Siparişiniz alındı" e-postası
}
```

Bu kod **okuyup karar veriyor**: "ödeme sonuçlandı mı?" ve "sipariş hâlâ bekliyor mu?" sorularının cevabı işlemin başında veritabanından okunuyor. Tek bir istek için doğrudur. Beş kopya birkaç milisaniye içinde gelirse:

| Zaman | R1 | R2 … R5 | Veritabanı |
| --- | --- | --- | --- |
| t1 | işlem açar; ödemeyi ve siparişi okur: `Pending`, `PendingPayment` | aynı şekilde okur: `Pending`, `PendingPayment` (R1 henüz commit etmedi) | ödeme `Pending` |
| t2 | "tekrar değil" der, siparişi bellekte `Paid` yapar | hepsi "tekrar değil" der, hepsi siparişi bellekte `Paid` yapar | |
| t3 | stok `UPDATE`: `quantity-2, reserved-2`; hareket, geçmiş, outbox ekler; commit | her biri sırayla aynı stok satırını kilitler, aynı düşümü yapar; hareket, geçmiş, outbox ekler; commit | stok 5 kez düştü |
| t4 | e-posta olayı | 4 e-posta olayı daha | 5 `order.paid` |

Sonuç: sipariş numarası başına 5 stok hareketi, 5 "Ödendi" geçmiş kaydı ve 5 e-posta. Stok satırı beş kez (`quantity - 2`) düştü. `reserved` adedi de 5 kez düştü, oysa bu siparişin rezervasyonu yalnızca 2 adetti: fazladan düşen 4 × 2 = 8 adet, aynı satırda başka müşterilerin ayırdığı adetten gitti. Bu müşteriler ödeme yaptığında ya da siparişleri süre dolumuyla iptal edildiğinde sistem rezervasyonlarını yine düşmeye çalışır ve hesap tutmaz. Başkasının ayırdığı stok "çalınmış" olur.

Sıralı (art arda, aralarında zaman olan) bir tekrarda kod doğru çalışır: ikinci olay geldiğinde ödeme zaten `Succeeded` olarak commit edilmiştir, `Duplicate` döner. Bu yüzden elle ya da sıralı test geçer; yalnızca **aynı anda** gelen kopyalar bozar.

**Kısıt neden bazen kurtarır, bazen kurtarmaz?** Veritabanında `ck_stock_items_reserved_valid` (`reserved >= 0 AND reserved <= quantity`) kısıtı var. Aynı stok satırında *başka rezervasyon yoksa* ikinci düşüm `reserved`'ı sıfırın altına indirir ve veritabanı bunu reddeder: istek 500 döner, işlem geri alınır, ödeme servisi olayı yeniden gönderir ve bu kez ödeme `Succeeded` olduğu için `Duplicate` (200) alır. Sonuç doğru görünür; hata, şans eseri sakladı. Aynı satırda başka müşterilerin rezervasyonu varsa (kontrolün eklediği 10 adet gibi) `reserved` sıfırın altına inmez ve kısıt sessiz kalır; kopyalar başkalarının rezervasyonunu düşer. Kısıt **son savunma hattıdır**, tekilleştirme yöntemi değil. Bölümün kontrolü bu yüzden 10 sahte rezervasyon ekler.

Değişikliği yapan geliştirici şunu düşünmüş (commit `044f151`):

> Webhook tekrar kontrolü ödeme durumuna taşındı
> Ayrı processed_webhook_events tablosuna gerek yok: ödemenin durumu zaten olayın işlenip
> işlenmediğini söylüyor. Bir INSERT ve bir ham SQL azaldı.

Mantık sezgisel olarak doğru görünüyor: "ödeme zaten sonuçlandıysa olay işlenmiştir". Eksik olan şu: bir olayın işlendiğini **kaydetmek** ve bunu işin kendisiyle aynı işlemde, atomik yapmak gerekir. Durumu okumak "şu an işlenmiş mi?" sorusunu yanıtlar, "başkası şu anda işliyor mu?" sorusunu yanıtlamaz. Ayrıca sipariş geçişini koşullu `UPDATE` ile yapan eski kod (`claimed == 1`) da aynı commit'te bellekteki değere bakan bir `if`'e çevrilmiş. İki savunma hattı birden kalktı.

## Nasıl bulunur

1. **Sonuçları sayın.** `kontrol 19` çıktısı "5 stok hareketi ve 5 'ödendi' kaydı oluştu" benzeri satırlar verir. Kontrol yalnızca stoğu eski hâline getirir; siparişin kayıtları yerinde kalır:

   ```sql
   -- sipariş numarasından id bulun
   SELECT id FROM orders WHERE order_number = 'TZ-2026-...';

   SELECT count(*) FROM stock_movements     WHERE reference_type = 'order' AND reference_id = 123;
   SELECT count(*) FROM order_status_history WHERE order_id = 123 AND to_status = 2;     -- 2 = Paid
   SELECT count(*) FROM outbox_messages      WHERE type = 'order.paid' AND payload::text LIKE '%TZ-2026-...%';
   SELECT * FROM processed_webhook_events    ORDER BY processed_at DESC LIMIT 5;        -- bu olay için satır YOK
   ```

   (`type` değeri olay adıdır; farklıysa `select distinct type from outbox_messages` ile bakın.) Mailpit'te (http://localhost:8025) aynı sipariş için birden fazla e-posta görünür.
2. **Günlüğü okuyun.** API günlüğünde `Ödeme olayı işlendi {EventId} {OrderNumber} Succeeded` satırı **aynı `EventId` ile 5 kez**, birkaç milisaniye farkla geçer. `Tekrarlanan ödeme olayı yok sayıldı` satırı hiç yoktur. Doğru çalışan sistemde bir "işlendi", dört "yok sayıldı" satırı olurdu. Aynı olay kimliğiyle tekrar eden "işlendi" günlüğü, tekilleştirmenin çalışmadığının en hızlı kanıtıdır.
3. **Zaman çizelgesi çizin.** "Aynı anda gelen iki kopya hangi satırları okur, ne yazar?" sorusunu yukarıdaki tablo gibi çizin. `payment.Status` ve `order.Status` okumaları işlemin başında yapılıyor ve kimse henüz commit etmediği için hepsi bekliyor görüyor.
4. **Durmadan günlük yazan kesme noktası.** Tekrar denetimi satırına bir logpoint / tracepoint koyun (`{evt.EventId} payment={payment?.Status} order={order.Status}`; Visual Studio: *Actions* → *Log a message*; VS Code: *Add Logpoint*). `kontrol 19` beş satırı da `Pending / PendingPayment` olarak yazar. Normal kesme noktası tüm süreci durdurup yarışı bozar. Visual Studio'da *Debug → Windows → Threads* ve *Parallel Stacks* pencereleri aynı satırda bekleyen birden çok isteği gösterebilir.
5. **Geçmiş.** `git log -p -- backend/src/Tezgah.Application/Payments/PaymentWebhookService.cs` `INSERT ... ON CONFLICT` bloğunun ve `claimed == 1` kontrolünün silindiğini gösterir.

## Düzeltme

Tekilleştirmeyi olay kimliği üzerinde, işlemin ilk adımı olarak, veritabanına yaptırın; sipariş geçişini de koşullu `UPDATE` ile yapın:

```diff
         await using var tx = await db.Database.BeginTransactionAsync(ct);
 
+        // Önce olay kimliğini yaz: aynı anda gelen iki kopyadan sadece biri bu satırı ekleyebilir.
+        var inserted = await db.Database.ExecuteSqlInterpolatedAsync(
+            $"INSERT INTO processed_webhook_events (event_id, source, processed_at) VALUES ({evt.EventId}, 'payments', {now}) ON CONFLICT (event_id) DO NOTHING", ct);
+        if (inserted == 0)
+        {
+            logger.LogInformation("Tekrarlanan ödeme olayı yok sayıldı {EventId}", evt.EventId);
+            return WebhookResult.Duplicate;
+        }
+
         var order = await db.Orders.Include(o => o.Items).SingleOrDefaultAsync(o => o.Id == evt.OrderId, ct);
@@
-        // Ödeme daha önce sonuçlandıysa aynı olayın tekrarıdır.
-        if (payment is { Status: PaymentStatus.Succeeded or PaymentStatus.Failed })
-        {
-            logger.LogInformation("Tekrarlanan ödeme olayı yok sayıldı {EventId}", evt.EventId);
-            await tx.CommitAsync(ct);
-            return WebhookResult.Duplicate;
-        }
-
@@
-            if (order.Status == OrderStatus.PendingPayment && order.GrandTotal == evt.Amount)
+            // Koşullu geçiş: bu arada süre dolumu siparişi iptal ettiyse ya da başka bir ödeme
+            // olayı onu zaten "ödendi" yaptıysa 0 satır etkilenir ve stok ikinci kez düşülmez.
+            var claimed = await db.Orders
+                .Where(o => o.Id == order.Id && o.Status == OrderStatus.PendingPayment && o.GrandTotal == evt.Amount)
+                .ExecuteUpdateAsync(s => s
+                    .SetProperty(o => o.Status, OrderStatus.Paid)
+                    .SetProperty(o => o.PaidAt, evt.OccurredAt)
+                    .SetProperty(o => o.UpdatedAt, now), ct);
+            if (claimed == 1)
             {
-                order.Status = OrderStatus.Paid;
-                order.PaidAt = evt.OccurredAt;
                 db.OrderStatusHistory.Add(...);
```

İki katman var ve ikisi de gerekli:

1. **Olay tekilleştirme (`INSERT ... ON CONFLICT DO NOTHING`).** `processed_webhook_events.event_id` üzerinde benzersiz anahtar (PK) olduğu için aynı `EventId` ile ikinci `INSERT`, ilkinin işlemi bitene kadar **bekler**. İlk işlem commit ederse ikinci `DO NOTHING` ile 0 satır etkiler ve `Duplicate` döner. İlk işlem hata verip geri alınırsa kayıt da geri alınır ve ikincisi devam eder: olay kaybolmaz. Kayıt ve etkiler *aynı işlemde* olduğu için "işlendi" kaydı varsa etkiler de vardır (hepsi ya da hiçbiri).
2. **Koşullu durum geçişi (`UPDATE ... WHERE status = PendingPayment`).** Farklı iki olay (farklı `EventId`) aynı siparişi ödenmiş yapmaya çalışabilir ya da süre dolumu işi aynı anda siparişi iptal ediyor olabilir. Koşullu `UPDATE` bir "karşılaştır ve ata" işlemidir; yalnızca durum hâlâ beklemedeyse geçiş yapar ve etkilenen satır sayısı (`claimed`) kazananı söyler. Kazanamayan (0 satır) stoğa dokunmaz; yetim ödeme ise iade için işaretlenir.

Tekrarlanan olaya API **200** döner (`Duplicate`); gönderen böylece yeniden denemeyi bırakır. 500 dönmek "bir şey ters gitti, tekrar dene" demektir ve gönderen aynı olayı yeniden gönderir.

## Neden böyle düzeltiyoruz

- **Durumu okuyarak tekilleştirmek (hatalı sürüm).** Okuma ile yazma arasında boşluk var; eşzamanlı kopyalar boşluğa girer. Bir de olay kimliği (mesajın kimliği) ile ödeme durumu (iş verisinin durumu) farklı şeylerdir: durumla tekilleştirmek, aynı ödemeye ait *farklı* bir olayı (örneğin sonradan gelen başarısız/başarılı sırası bozuk olay) da "tekrar" sanıp düşürebilir.
- **Satırı kilitleyerek okumak (`SELECT ... FOR UPDATE` sipariş üzerinde).** Çalışır: ikinci istek kilidi bekler, kilit açılınca siparişi `Paid` görür. Ama olay bazlı kayıt tutmaz (denetim ve hata ayıklama için "bu olay işlendi mi?" cevabı olmaz), ham SQL ister ve kilidi işlem boyunca tutar. Yardımcı bir katman olarak mantıklıdır, tek başına yetmez.
- **İyimser eşzamanlılık (`xmin` belirteci).** Çatışmada istisna fırlatır, sonuç 500 olur, gönderen yeniden dener ve bir sonraki deneme `Paid` görür. Doğru sonuca varır ama gürültülüdür ve her tekrar 500 ile başlar.
- **Serializable yalıtım.** İşlemleri tek tek sıraya dizmiş gibi davranır; çatışan işlemler iptal edilir (`40001`) ve yeniden denenir. Genel maliyeti ve yeniden deneme gereksinimi yüksektir; bu probleme ağır bir çözümdür.
- **Veritabanı kısıtlarına güvenmek.** `ck_stock_items_reserved_valid` kimi zaman ikinci düşümü reddeder (yukarıda anlatıldığı gibi) ama başkalarının rezervasyonu varsa susar. Ek savunma olarak stok hareketlerine ya da "ödendi" geçmişine benzersizlik kısıtı koymak (ör. `order_status_history` için `to_status = 2` kısmi benzersiz index'i) işe yarar; ihlal 500 yerine `Duplicate` olarak ele alınmalı. Ama kısıt, tekilleştirmenin yerine geçmez.
- **Gönderenin tekrar göndermesini engellemek.** "Tam bir kez" (exactly-once) teslim dağıtık sistemlerde pratikte yoktur. Ağ yanıtı kaybolursa gönderen yeniden dener, bu doğru ve istenen davranıştır. Güvenilirliği, tüketicinin idempotent olması sağlar.
- **Tekilleştirmeyi bildirim tarafına bırakmak.** Bildirim servisi `event_id` ile tekilleştirir ama burada beş `order.paid` olayı *farklı* kimliklerle (`Guid.CreateVersion7`) üretildi; yani tekrar bildirim servisine ulaşmadan önce, ilk yan etkinin oluştuğu yerde kesilmeli.

## Bir daha olmaması için

- **Test:** Bu bölümün kontrolü gibi bir entegrasyon testi: gerçek PostgreSQL ile aynı olayı 5-20 kez **paralel** gönderin; yanıtlar 200, stok bir kez düşmüş, bir hareket, bir "Ödendi" kaydı, bir `order.paid`, başkalarının rezervasyonu yerinde. Ayrıca sıralı tekrar testi ve "olay k kez uygulanırsa bir kez uygulanmışla aynıdır" özelliği. Zamanlamaya bağlı hatalar için testi birçok tur koşturun. Başkalarının rezervasyonunu içeren senaryo şart: onsuz kısıt hatayı maskeler.
- **Kod inceleme kuralı:** Tekilleştirme kodunda "okuyup karar vermek" yerine "yazarak sahiplenmek" (benzersiz anahtar ya da koşullu güncelleme) ara. "Bir tablo ve bir INSERT az" gerekçesiyle yapılan sadeleştirmelerde bunun neden orada olduğunu sorun; kaldırılan yorumları okuyun.
- **Üç ilke:** (1) en az bir kez teslim kabul edilir, (2) tüketici idempotent olur (olay kimliğini etkilerle aynı işlemde kaydeder), (3) durum geçişleri koşulludur (karşılaştır ve ata).
- **İzleme:** Duplicate oranı (kaç olay `Duplicate` ile döndü), `payment.needs_refund` kayıtları, aynı sipariş için birden fazla "Ödendi" geçmişi (periyodik sorgu), gün sonu stok mutabakatı (stok hareketlerinin toplamı = stok değişimi). Aynı `EventId` ile tekrar eden "işlendi" günlüğü için uyarı.
- **Olay sonrası veri onarımı:** Etkilenen siparişleri `order_status_history` içinde `to_status = 2` sayısı 1'den büyük olanlardan bulun. Fazla düşülen stok için **ters hareket** (düzeltme kaydı) ekleyin ve stok satırını buna göre düzeltin; satırları silmeyin, izi koruyun. Başkalarının eksilen rezervasyonu, bekleyen siparişlerden yeniden hesaplanabilir. Gönderilmiş fazla e-postalar geri alınamaz; gerekirse müşteriye özür bildirimi gönderin.

## İlgili

HATALAR.md · **EŞ-02 Ödeme bildirimi iki kez geliyor (idempotency)**. Bkz. ayrıca EŞ-01 (aynı yarış kalıbı) ve EŞ-03 (outbox).

## English summary

**Root cause:** the payment service delivers webhooks at least once. `PaymentWebhookService.HandleAsync` was changed to detect a repeat by reading "is the payment already Succeeded/Failed?" and to transition the order using the in-memory `order.Status == PendingPayment`, instead of writing the event id to `processed_webhook_events` (`INSERT ... ON CONFLICT DO NOTHING`) and using a conditional `UPDATE ... WHERE status = PendingPayment`. When copies of the same event arrive at the same time, each one sees "pending", and each one deducts stock, adds a stock movement and a "Paid" history row, and publishes an `order.paid` event (several emails). Other customers' reservations on the same stock row are also deducted. If no other reservation exists on that row, the `ck_stock_items_reserved_valid` check constraint rejects the second deduction (a 500, the payment service retries and then gets Duplicate): a last line of defence, not a dedupe mechanism. The commit "Webhook tekrar kontrolü ödeme durumuna taşındı" removed one table and one raw SQL statement and with them both protections.

**How to find it:** count stock movements, "Paid" history rows and `order.paid` outbox rows for the order; the API log shows "Ödeme olayı işlendi" for the same `EventId` several times and never "Tekrarlanan ödeme olayı yok sayıldı"; a logpoint shows all copies reading Pending; `git log -p`.

**Fix:** first statement in the transaction: `INSERT INTO processed_webhook_events ... ON CONFLICT (event_id) DO NOTHING` and return `Duplicate` (HTTP 200) when 0 rows are inserted; then a conditional `ExecuteUpdate` on the order (`WHERE status = PendingPayment`) and continue only when 1 row is affected. Alternatives: `SELECT FOR UPDATE` on the order, optimistic concurrency, Serializable isolation, unique constraints as extra defence.

**Prevent:** a parallel duplicate-event test on a real database including other customers' reservations, review of read-then-write dedupe, the three principles (at-least-once delivery, idempotent consumer, conditional state transition), and monitoring for duplicate rates and daily stock reconciliation. Repair affected data with compensating stock movements rather than by deleting rows.
