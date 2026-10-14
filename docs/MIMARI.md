# Mimari

Tezgah, yıllardır tek bir monolitle çalışan ve iki yıl önce mikroservise geçmeye başlayan bir
şirket. Geçiş yarıda: Ödeme ve Bildirim ayrı servislere taşındı, geri kalan her şey hâlâ monolitte.
Gerçek şirketlerin çoğu böyledir; bu yarım hâl birçok hatanın da kaynağıdır.

```
 Tarayıcı
 ├─ Müşteri sitesi  (React, 5173) ─┐
 ├─ Satıcı paneli   (React, 5174) ─┼──► API — modüler monolit (ASP.NET Core, 5080) ──► PostgreSQL "tezgah"
 └─ Yönetim paneli  (React, 5175) ─┘     │  katalog · sepet · kupon · sipariş · stok · satıcı
                                          │  kargo · yönetim · rapor (eski modül, Dapper)
 Ödeme sayfası (3D) ◄── yönlendirme ──────┤
        │                                 │ HTTP + API anahtarı: ödeme oturumu aç
        ▼                                 ▼
 Ödeme servisi (5090) ── HMAC imzalı webhook (outbox, en az bir kez) ──► API
        └─► PostgreSQL "tezgah_payments"
                                          │ outbox → RabbitMQ (topic "tezgah.events")
                                          ▼
                         Bildirim servisi (5095) ──► Mailpit (SMTP 1025)
                               └─► PostgreSQL "tezgah_notifications"
```

## Katmanlar (monolit)

| Proje | Sorumluluk |
| --- | --- |
| `Tezgah.Domain` | Varlıklar ve enum'lar. Bağımlılığı yok. |
| `Tezgah.Application` | İş kuralları: servisler, DTO'lar, fiyat hesabı (`CartPricingCalculator`), arayüzler. |
| `Tezgah.Infrastructure` | EF Core `TezgahDbContext`, migration'lar, JWT, ödeme HTTP istemcisi, RabbitMQ yayıncısı, Dapper raporları. |
| `Tezgah.Api` | Controller'lar, hata → ProblemDetails dönüşümü, kimlik doğrulama, arka plan işleri. |
| `Tezgah.Contracts` | Servisler arası sözleşmeler: ödeme istek/yanıt/webhook, entegrasyon olayları. |

Ayrı servisler: `Tezgah.Payments` (sahte ödeme sağlayıcısı + 3D sayfası), `Tezgah.Notifications`
(RabbitMQ tüketicisi + e-posta). Araçlar: `tools/Tezgah.DataGen` (örnek veri), `tools/Tezgah.Bolum`
(bölüm aracı).

## Sipariş ve ödeme akışı

1. `POST /api/orders` — `Idempotency-Key` başlığıyla. Sepet yeniden fiyatlanır, kupon doğrulanır.
2. Tek işlemde: her kalem için stok **koşullu tek UPDATE** ile ayrılır
   (`quantity - reserved >= adet`), kupon kullanım sayacı koşullu artırılır, sipariş
   `PendingPayment` açılır, sepet boşaltılır.
3. İşlem dışında: ödeme servisinden oturum istenir, müşteri `checkoutUrl`'e yönlendirilir.
4. Müşteri 3D sayfasında onaylar. Ödeme servisi sonucu ve webhook olayını **aynı işlemde** yazar
   (outbox); bir arka plan işi webhook'u HMAC imzasıyla, başarılı olana kadar artan aralıklarla gönderir.
5. API webhook'u imzayı doğrulayarak alır, olay kimliğini `processed_webhook_events` tablosuna yazar
   (ikinci kez gelirse hiçbir şey yapmaz), siparişi koşullu UPDATE ile `Paid` yapar, ayrılmış stoğu
   satışa çevirir ve `order.paid` olayını outbox'a ekler.
6. 30 dakika içinde ödenmeyen sipariş iptal edilir; stok ve kupon kullanımı geri verilir. İptal edilmiş
   siparişe geç gelen ödeme siparişi geri açmaz, iade kaydı oluşturur.

## Olaylar

| Olay | Ne zaman | Tüketen |
| --- | --- | --- |
| `order.paid` | Ödeme onaylanınca | Bildirim: "Siparişiniz alındı" |
| `order.shipped` | Satıcı kargoya verince | Bildirim: "Siparişiniz kargoya verildi" |
| `seller.approved` | Yönetici mağazayı onaylayınca | Bildirim: "Mağazanız onaylandı" |

Monolit olayları `outbox_messages` tablosuna iş değişikliğiyle aynı işlemde yazar; `OutboxPublisherJob`
bunları yayıncı onayı (publisher confirm) ile RabbitMQ'ya gönderir. Teslim en az bir kezdir; tüketici
`event_id` üzerinden tekilleştirir. İşlenemeyen mesajlar `notifications.email.dlq` kuyruğuna düşer.

## Kimlik ve yetki

- JWT erişim token'ı (15 dk) + yenileme token'ı (veritabanında SHA-256 özeti, her kullanımda yenilenir).
- Roller: `customer`, `seller`, `admin`. Satıcı token'ında `seller_id` claim'i vardır; satıcı uçları
  ayrıca mağazanın `Active` olduğunu kontrol eder (değilse 403 ve sebep).
- Servisler arası: ödeme servisine `X-Api-Key`, webhook'a `X-Tezgah-Signature` (HMAC-SHA256).

## Veri

PostgreSQL, `snake_case` adlandırma, para `numeric(18,2)`, zamanlar `timestamptz` (UTC). Gün
sınırları (raporlar, kampanya başlangıç/bitişi) **Europe/Istanbul** takvimine göredir. Sunucular
Türkçe kültürle (`tr-TR`) çalışır; kod büyük/küçük harf ve sayı dönüşümlerinde kültürden bağımsız
yöntemleri kullanır.

Önemli tablolar: `users`, `sellers`, `products`, `product_variants`, `stock_items`,
`stock_movements`, `carts`, `coupons`, `coupon_usages`, `orders`, `order_items`,
`order_status_history`, `payments`, `shipments`, `outbox_messages`, `processed_webhook_events`,
`audit_logs`.

## Bilinçli sadeleştirmeler

- Ödeme sağlayıcısı sahtedir; kart bilgisi alınmaz.
- Kargo firması entegrasyonu yoktur; satıcı takip numarasını elle girer.
- Görseller dış bir yer tutucu servisten gelir (picsum.photos).
- Redis şimdilik kullanılmıyor; önbellek bölümleri için hazır bekliyor.
