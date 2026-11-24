> **Cevap anahtarı.** Bölümü kendiniz çözmeden okumayın; önce `ipucu` komutunu deneyin.

# Çözüm · Bölüm 27 (EŞ-03) · Yayıncı ile tüketicinin anlaşmazlığı: yönlendirilemeyen mesaj

## Kök neden

Satıcı siparişi kargoya verince API bir "kargolandı" mesajını outbox tablosuna yazar, `OutboxDispatcher` onu RabbitMQ'ya yayınlar, Bildirim servisi de e-postayı gönderir. Mesajın **adı** (routing key) iki taraf arasındaki sözleşmedir.

Hatalı sürümde yayıncı adı değiştirilmiş:

```diff
 // backend/src/Tezgah.Contracts/Events/IntegrationEvents.cs
     public const string OrderShipped = "order.shipped";
+    public const string ShipmentCreated = "order.shipment-created";
```

```diff
 // backend/src/Tezgah.Application/Sellers/SellerOrderService.cs  (ShipAsync)
-        Outbox.Add(db, shippedEvent.EventId, EventTopology.OrderShipped, shippedEvent, now);
+        Outbox.Add(db, shippedEvent.EventId, EventTopology.ShipmentCreated, shippedEvent, now);
```

Bildirim servisi ise kuyruğunu yalnızca bilinen adlara bağlıyor ve gelen adı aynı listeyle çözümlüyor (`NotificationConsumer.cs`, `MessageTypeResolver.cs`):

```csharp
foreach (var key in new[] { EventTopology.OrderPaid, EventTopology.OrderShipped, EventTopology.SellerApproved })
    await channel.QueueBindAsync(Queue, EventTopology.Exchange, key, cancellationToken: ct);
```

`order.shipment-created` bu listede yok. Yayıncı `mandatory: true` ile yayınladığı için (`RabbitMqPublisher.cs`) hiçbir kuyruğa yönlenemeyen mesaj geri döner ve **hata sayılır**; `OutboxDispatcher` mesajı bir süre sonra yeniden dener, her seferinde aynı sonucu alır. Sonuç: API logunda tekrarlayan yayın hataları, `outbox_messages` içinde `published_at` boş kalan kayıtlar, Bildirim servisinde hiç gelmeyen mesaj ve gönderilmeyen e-posta. Satıcı paneli bundan etkilenmez: sipariş durumu aynı işlemde "kargoda" olur, mesaj ise sonradan asenkron yayınlanır.

Değişikliği yapan geliştirici şunu düşünmüş (commit `abc1234`):

> Kargo olayı satıcı bazlı adla yayınlanıyor
> Çok satıcılı siparişte sipariş bütünüyle kargoya verilmeden order.shipped yayınlamak yanıltıcıydı. Olay artık order.shipment-created; gövde aynı (OrderShippedEvent).

Adlandırma gerekçesi makul. Ama mesaj adı iki ayrı servis arasında bir **sözleşmedir**: yalnızca bir tarafı değiştirmek sözleşmeyi bozar. Üstelik iki servis ayrı dağıtılır; aynı anda güncellenseler bile aradaki dağıtım penceresinde mesaj kaybı yaşanır.

## Nasıl bulunur

1. **Belirti:** "Ödendi" e-postası geliyor, "kargoda" gelmiyor. Aynı e-posta altyapısı bir mesajı gönderebiliyorsa sorun e-posta altyapısında değil, bu mesajın kendisindedir.
2. **Bildirim logu:** `docker compose logs notifications` kargodan sonra hiçbir satır göstermez; mesaj servise hiç ulaşmıyor.
3. **API logu:** `docker compose logs api` içinde aynı yayın hatası (geri dönen, yönlendirilemeyen mesaj) tekrar tekrar görünür.
4. **Outbox tablosu:**

   ```sql
   SELECT type, published_at, attempts, last_error FROM outbox_messages ORDER BY occurred_at DESC LIMIT 5;
   ```

   Kargo kaydının `type` değeri `order.shipment-created`, `published_at` boş, `attempts` giderek artıyor ve `last_error` yönlendirilemeyen mesajı anlatıyordur.
5. **RabbitMQ yönetim ekranı** (http://localhost:15680, `tezgah` / `tezgah`): `tezgah.events` aracı kuruluşunun *Bindings* listesinde `order.paid`, `order.shipped`, `seller.approved` var; `order.shipment-created` yok.
6. **Kod:** Yayıncının kullandığı ad ile tüketicinin bağladığı adları yan yana koyun. `git log -p -- backend/src/Tezgah.Application/Sellers/SellerOrderService.cs` değişikliği gösterir.

**Araç ipucu:** RabbitMQ yönetim ekranındaki *Exchanges → tezgah.events → Bindings* bölümü "kim hangi adı dinliyor" sorusunun tek bakışlık cevabıdır; yayıncının adı o listede yoksa mesaj boşa gidiyordur.

## Düzeltme

En basit ve güvenli çözüm: yayıncıda eski adı geri koymak.

```diff
-        Outbox.Add(db, shippedEvent.EventId, EventTopology.ShipmentCreated, shippedEvent, now);
+        Outbox.Add(db, shippedEvent.EventId, EventTopology.OrderShipped, shippedEvent, now);
```

Yeni ad gerçekten isteniyorsa (çok satıcılı siparişte satıcı bazlı anlam taşıyor) geçiş **iki adımda, geriye uyumlu** yapılır:

1. **Önce tüketici iki adı da tanısın** ve dağıtılsın:

   ```diff
   -foreach (var key in new[] { EventTopology.OrderPaid, EventTopology.OrderShipped, EventTopology.SellerApproved })
   +foreach (var key in new[] { EventTopology.OrderPaid, EventTopology.OrderShipped, EventTopology.ShipmentCreated, EventTopology.SellerApproved })
   ```

   ```diff
   // MessageTypeResolver.cs
           EventTopology.OrderShipped => typeof(OrderShippedEvent),
   +       EventTopology.ShipmentCreated => typeof(OrderShippedEvent),
   ```

2. **Sonra yayıncı yeni adı kullansın.** Eski ad, kuyrukta ve outbox'ta bekleyen mesajlar bitene kadar tüketicide kalır; sonra kaldırılır.

İkisi de uygulandıktan sonra outbox'ta bekleyen kayıtlar yeniden denemede yayınlanır ve e-postalar birikmiş olarak gider (yeniden deneme aralığı büyümüş olabilir; gerekirse `next_attempt_at` değerini geriye çekin).

## Neden böyle düzeltiyoruz

- **Yalnızca tüketiciyi değiştirmek.** Çalışır, ama sözleşmeyi bozan yayıncıyı olduğu gibi bırakır; ad değişikliğinin gerekçesi (satıcı bazlı anlam) tüketicide ayrıca karşılanmaz ve diğer olası tüketiciler aynı tuzağa düşer.
- **İkisini aynı anda değiştirmek ve aynı anda dağıtmak.** Servisler ayrı dağıtılır; pencere boyunca mesajlar kaybolur ya da hata verir. Geriye uyumlu iki adımlı geçiş bu pencereyi ortadan kaldırır.
- **`mandatory: true`'yu kaldırmak.** Hatayı susturur, mesajlar sessizce yok olur. Mevcut davranış doğru: yönlendirilemeyen mesaj gürültülü biçimde hata verir; hatanın nedenini bulmak bu yüzden mümkündü.
- **Bildirim servisinde bilinmeyen adları yoksaymak.** Aynı sessiz kayıp; yeni ad bir gün gerçekten yanlış yazılırsa kimse fark etmez.

## Bir daha olmaması için

- **Sözleşme testi:** Yayıncının kullandığı her mesaj adı için "tüketicinin bağlama listesinde ve çözümleyicisinde var mı?" diye bakan bir test (ya da ortak `EventTopology` listesinden hem bağlamayı hem çözümlemeyi üreten tek bir yer). Böylece iki liste ayrı ayrı elle güncellenmez.
- **Uçtan uca test:** Bölümün kontrolü demo mağazanın ödenmiş bir siparişini kargoya verip e-postayı bekler; benzer bir "kargoya ver → e-posta geldi" duman testi CI'da çalışsın.
- **İzleme:** `published_at` boş kalan ve yaşı artan outbox kayıtları için uyarı; yönlendirilemeyen (geri dönen) mesaj sayısı için sayaç.
- **Kod inceleme maddesi:** Ortak sözleşme dosyasına (`Tezgah.Contracts`) dokunan her PR'da "tüketiciler de güncellendi mi, geçiş sırası geriye uyumlu mu?" sorulsun.

## İlgili

HATALAR.md · **EŞ-03 Mesaj kayboldu (outbox yok)**. Bu bölümde outbox var ve mesaj kaybolmuyor; ama sözleşme uyumsuzluğunun yarattığı birikmeyi göstermesi bakımından aynı ailedendir.

## English summary

**Root cause:** the shipping message name was changed on the publisher side only. `SellerOrderService.ShipAsync` now writes the outbox row with the new routing key `order.shipment-created` (`EventTopology.ShipmentCreated`), while the Notifications service binds its queue only to `order.paid`, `order.shipped` and `seller.approved` and resolves message types with the same list. The publisher uses `mandatory: true`, so a message that cannot be routed to any queue is returned and counted as a failure; `OutboxDispatcher` retries forever. Result: repeated publish errors in the API log, outbox rows with an empty `published_at`, no message reaching the Notifications service, and no email, while the seller panel looks fine. The developer (commit "Kargo olayı satıcı bazlı adla yayınlanıyor") renamed the message for a sensible reason, but a message name is a contract between two services.

**How to find it:** the paid email works but the shipped one does not; the Notifications log shows nothing; the API log repeats the publish error; `outbox_messages` has unpublished rows with the new `type`; the RabbitMQ management screen (http://localhost:15680) shows no binding for the new name.

**Fix:** put the old name back in the publisher, or migrate in a backward-compatible way: first let the consumer bind and resolve both names, deploy it, then switch the publisher (and remove the old name once the backlog is gone).

**Prevent:** a contract test that every published name is bound and resolved by its consumer (ideally one shared list), an end-to-end "ship → email arrives" smoke test, alerts on old unpublished outbox rows and on returned messages, and review attention to changes in the shared contracts project.
