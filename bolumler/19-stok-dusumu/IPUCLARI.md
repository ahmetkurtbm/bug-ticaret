# İpuçları · Bölüm 19 (EŞ-02)

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

`kontrol 19` bittikten sonra sipariş kayıtları yerinde kalır; onlara bakın. Veritabanında o siparişin `stock_movements` (`reference_type = 'order'`), `order_status_history` (`to_status = 2`) ve `outbox_messages` satırlarını sayın, http://localhost:8025 adresinde (Mailpit) kaç e-posta geldiğine bakın. API günlüğünde `Ödeme olayı işlendi` satırını arayın: aynı `EventId` kaç kez geçiyor ve zaman damgaları birbirine ne kadar yakın?

*EN:* After `kontrol 19` finishes, the order's records stay in place; look at them. In the database count that order's rows in `stock_movements` (`reference_type = 'order'`), `order_status_history` (`to_status = 2`) and `outbox_messages`, and see how many emails arrived at http://localhost:8025 (Mailpit). In the API log search for the line `Ödeme olayı işlendi`: how many times does the same `EventId` appear, and how close are the timestamps?

## İpucu 2

Ödeme servisi olayları "en az bir kez" gönderir: aynı olay birden çok kez gelebilir ve API bunu tek seferlik işlemeye eşdeğer yapmalıdır (idempotent). Kodda tekrarın nasıl anlaşıldığına bakın: olayın kendisi mi kaydediliyor, yoksa bir şey okunup karar mı veriliyor? Aynı olayın beş kopyası aynı anda gelirse, kopyaların her biri "ödeme sonuçlandı mı?" ve "sipariş hâlâ bekliyor mu?" sorularını sorduğunda hangi yanıtı alır? Yarış (race) mantığıyla, bir önceki bölümde gördüğünüz oku-kontrol et-yaz kalıbını düşünün.

*EN:* The payment service sends events "at least once": the same event can arrive more than once, and the API must make that equivalent to processing it once (idempotent). Look at how the code recognizes a repeat: is the event itself recorded, or is something read and a decision made? If five copies of the same event arrive at the same moment, what answer does each copy get when it asks "has the payment been settled?" and "is the order still waiting?" Think in terms of a race, and about the read-check-write pattern from the previous chapter.

## İpucu 3

`backend/src/Tezgah.Application/Payments/PaymentWebhookService.cs` içindeki `HandleAsync`, tekrarı "ödeme zaten `Succeeded`/`Failed` mi?" okumasıyla anlıyor ve sipariş geçişini bellekteki `order.Status == PendingPayment` değerine bakarak yapıyor. Beş kopya da işlem başında ödemeyi ve siparişi bekliyor görür, hepsi ilerler: her biri stoğu düşer, stok hareketi, geçmiş kaydı ve `order.paid` olayı ekler. Olay kimliğinin `processed_webhook_events` tablosuna `INSERT ... ON CONFLICT DO NOTHING` ile, işlemin ilk adımı olarak yazılması ve sipariş durumunun `WHERE status = PendingPayment` koşullu bir `ExecuteUpdate` ile değiştirilip etkilenen satır sayısına bakılması gerekir.

*EN:* `HandleAsync` in `backend/src/Tezgah.Application/Payments/PaymentWebhookService.cs` recognizes a repeat by reading "is the payment already `Succeeded`/`Failed`?" and makes the order transition based on the in-memory `order.Status == PendingPayment`. All five copies see the payment and order as waiting at the start of their transaction and all proceed: each one deducts the stock and adds a stock movement, a history record and an `order.paid` event. The event id must be written to the `processed_webhook_events` table with `INSERT ... ON CONFLICT DO NOTHING` as the first step of the transaction, and the order status must be changed with a conditional `ExecuteUpdate` (`WHERE status = PendingPayment`) while checking the affected row count.
