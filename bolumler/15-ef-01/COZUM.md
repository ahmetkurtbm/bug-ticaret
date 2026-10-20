# Çözüm · Bölüm 15 (EF-01) · N+1 sorgu

## Kök neden

`backend/src/Tezgah.Application/Admin/AdminOrderService.cs` içindeki `ListAsync` siparişleri çektikten sonra her sipariş için döngüde iki ayrı sorgu çalıştırıyor:

```csharp
foreach (var o in orders)
{
    var customer = await db.Users.AsNoTracking().SingleAsync(u => u.Id == o.UserId, ct);
    var lines = await db.OrderItems.AsNoTracking().Where(i => i.OrderId == o.Id).ToListAsync(ct);
    items.Add(new AdminOrderListItemDto(/* ... */));
}
```

Sayfa başına sorgu sayısı: 1 (toplam sayı, `CountAsync`) + 1 (sipariş listesi) + N × 2 (müşteri ve kalemler). Panelin varsayılan sayfası 20 siparişlik olduğu için 42 sorgu, 50 siparişlik sayfada 102 sorgu çalışır. Buna N+1 sorgu sorunu denir (burada ilişki iki tane olduğu için aslında 2N+1 ek sorgu). Her sorgu veritabanına ayrı bir ağ gidiş-dönüşüdür. Veritabanı aynı makinedeyse gidiş-dönüş mikrosaniyeler sürer ve fark görünmez; ayrı sunucuda her biri milisaniyelerdir ve toplam saniyelere çıkar. Bu yüzden yerelde "bende yavaş değil" demek kolaydır.

Değişikliği yapan geliştirici şunu düşünmüş (commit `7bd1890`):

> Yönetim sipariş listesi okunabilir hale getirildi
> Uzun LINQ projeksiyonu yerine adım adım doldurma; hata ayıklarken satır satır izlenebiliyor.

Kaygısı gerçek: tek ifadelik uzun bir `Select`, hata ayıklayıcıda adım adım izlenemez; EF onu SQL'e çevirir ve değişkenleri araya bakılacak biçimde göstermez. Adım adım yazılmış bir döngü okunur ve izlenebilir. Ama bu okunabilirliğin bedeli, veritabanına sayfa başına yüzlerce gidiş-dönüş oldu. Okunabilirlik başka yollarla da sağlanabilirdi (aşağıya bakın).

## Nasıl bulunur

1. **Sorgu sayısını görün.** API'yi Development ortamında çalıştırın (`--launch-profile http`). Yönetim panelinde Siparişler sayfasını açın, tarayıcıda Network → siparişler isteği → **Timing**. `Server-Timing` altında şunu görürsünüz:

   ```text
   db;dur=84.3;desc="42 sorgu"
   ```

   Bu başlığı `Tezgah.Infrastructure/Diagnostics/QueryCounter.cs` içindeki bir EF yakalayıcısı (interceptor) üretir; `Program.cs` yalnızca Development'ta ekler. Panel API'ye başka bir port üzerinden gittiği için `Timing-Allow-Origin: *` başlığı da gönderilir; bu olmadan tarayıcı başlığı göstermezdi. (Ortam Development değilse başlık hiç gelmez; kontrol aracı da o yüzden "Development ortamında mı çalışıyor?" diye sorar.)
2. **Sayfa boyutuyla ilişkiyi deneyin.** `src/Tezgah.Api/Tezgah.Api.http` içinde `/api/admin/orders?page=1&pageSize=5` isteğini gönderin: 12 sorgu. `pageSize=50`: 102 sorgu. Sorgu sayısı satır sayısıyla doğrusal artıyorsa N+1 vardır.
3. **EF günlüklerini açın.** Varsayılan düzey `Warning`; SQL'ler görünmez. `backend/src/Tezgah.Api/appsettings.Development.json` içine ekleyin:

   ```json
   "Logging": {
     "LogLevel": {
       "Microsoft.EntityFrameworkCore.Database.Command": "Information"
     }
   }
   ```

   Bu değişikliği işiniz bitince geri alın (commit'lemeyin). API'yi yeniden başlatın. Konsolda sayfa başına şu iki sorgunun satır sayısı kadar tekrarlandığını görürsünüz:

   ```text
   SELECT ... FROM users AS u WHERE u.id = @p LIMIT 2
   SELECT ... FROM order_items AS o WHERE o.order_id = @p
   ```

   Parametre değerlerini görmek için `EnableSensitiveDataLogging` gerekir; yalnızca yerelde açın.
4. **Çağıranı bulun.** Konsoldaki sorgunun hemen öncesindeki `SELECT ... FROM orders ... LIMIT @p` sorgusu listeyi veren sorgudur. `AdminController` → `AdminOrderService.ListAsync` içinde `foreach` içindeki `await`'ler görünür. Kesme noktasını döngü içine koyup *Call Stack* ve *Locals* penceresine bakmak (Visual Studio: Debug → Windows; VS Code: *Call Stack* ve *Variables* panelleri) her tekrarın bir ağ çağrısı olduğunu doğrular.
5. **Geçmişe bakın.** `git log -p -- backend/src/Tezgah.Application/Admin/AdminOrderService.cs` "tek sorgu: müşteri ve kalem sayıları projeksiyonda" yorumunun silindiğini gösterir.

## Düzeltme

Müşteri ve kalem toplamlarını liste sorgusunun içinde projeksiyonla isteyin; EF bunları tek SQL'e çevirir:

```diff
         var total = await query.CountAsync(ct);
-        var orders = await query
+        // Tek sorgu: müşteri ve kalem sayıları projeksiyonda; satır başına ek sorgu yok.
+        var items = await query
             .OrderByDescending(o => o.PlacedAt).ThenByDescending(o => o.Id)
             .Skip((page - 1) * pageSize).Take(pageSize)
+            .Select(o => new AdminOrderListItemDto(
+                o.Id, o.OrderNumber, o.User.Email, o.User.FirstName + " " + o.User.LastName, o.PlacedAt, o.Status, o.GrandTotal,
+                o.Items.Sum(i => i.Quantity), o.Items.Select(i => i.SellerId).Distinct().Count(), o.CouponCode))
             .ToListAsync(ct);
-
-        var items = new List<AdminOrderListItemDto>(orders.Count);
-        foreach (var o in orders)
-        {
-            var customer = await db.Users.AsNoTracking().SingleAsync(u => u.Id == o.UserId, ct);
-            var lines = await db.OrderItems.AsNoTracking().Where(i => i.OrderId == o.Id).ToListAsync(ct);
-            items.Add(new AdminOrderListItemDto(
-                o.Id, o.OrderNumber, customer.Email, $"{customer.FirstName} {customer.LastName}", o.PlacedAt, o.Status, o.GrandTotal,
-                lines.Sum(i => i.Quantity), lines.Select(i => i.SellerId).Distinct().Count(), o.CouponCode));
-        }
         return new PagedResult<AdminOrderListItemDto>(items, page, pageSize, total);
```

Sonuç: sayım için 1 sorgu, liste (müşteri birleştirmesi ve alt sorgularla kalem toplamları dahil) için 1 sorgu: toplam 2, satır sayısından bağımsız. Kontrol en fazla 5'e izin verir.

## Neden böyle düzeltiyoruz

- **`Include(o => o.User).Include(o => o.Items)`.** Sorgu sayısını düşürür ama gereğinden fazlasını getirir: tüm kalem satırlarını ve tüm sütunları belleğe alır, sonra bellekte toplar. Burada yalnızca iki sayı (adet toplamı, farklı satıcı sayısı) lazım. Üstelik kalemler de `Include` edilince satırlar çarpılır (kartezyen patlama, bkz. EF-02). `AsSplitQuery()` bunu azaltır ama yine tüm kalemler taşınır.
- **Projeksiyon (`Select`).** Yalnızca gereken sütunları ve toplamları alır, hesaplamayı veritabanında yapar, izleme (change tracking) gerektirmez. Varsayılan çözümdür.
- **Toplu yükleme (`WHERE id IN (...)`).** Önce siparişleri, sonra tüm kullanıcıları ve kalemleri birer sorguyla çekip bellekte birleştirmek 3-4 sorguya indirir. Sorgu satır sayısından bağımsız kalır ve projeksiyon çok karmaşıksa makul bir yoldur. Ama eşleştirmeyi elle yapmak hata alanı açar.
- **Lazy loading'i açmak.** İlişkilere erişimi örtülü sorguya çevirir; N+1 kodda görünmez olur, daha da kötüsü kimse fark etmeden her yerde oluşur. Bu projede lazy loading kapalıdır.
- **Önbellek (cache) koymak ya da sayfa boyutunu küçültmek.** Belirtiyi maskeler. Sorgu sayısı yine satır sayısıyla artar; önbellek boşken aynı yavaşlık döner.
- **Okunabilirlik kaygısı.** Haklı bir kaygı, ama döngüyle değil şöyle çözülür: projeksiyonu adlandırılmış bir `Expression<Func<Order, AdminOrderListItemDto>>` olarak ayırın; hata ayıklarken sorgu metni için `ToQueryString()` (EF) kullanın; ve davranışı sorgu sayısı testiyle koruyun.

## Bir daha olmaması için

- **Test:** Bu bölümün kontrolü gibi bir entegrasyon testi 50 siparişlik sayfa için sorgu sayısını sayar ve `<= 5` ister. Sayım için projedeki `QueryCountingInterceptor` testlerde de kullanılabilir. Küçük veriyle (3 sipariş) yazılan testler N+1'i yakalamaz; test verisi birden çok satır içermeli ve iddia "sorgu sayısı satır sayısına bağlı değil" olmalı.
- **Kod inceleme:** Servis katmanında `foreach` ya da `Select` içinde `await db...` gören herkes "bu satır sayısı kadar çalışır mı?" diye sormalı. "Okunabilir hale getirme" commit'lerinde sorgu sayısı karşılaştırması isteyin.
- **Araç:** Geliştirme ortamında `Server-Timing` sorgu sayısı her zaman görünür; yeni bir liste ekranı yazarken Network → Timing'e bakmayı alışkanlık yapın. Günlük düzeyini `Information` yapmak da bunu gösterir.
- **İzleme:** Üretimde dağıtık izleme (OpenTelemetry EF Core izleyicisi ya da APM) ile "istek başına veritabanı çağrısı" panosu kurun, belirli bir eşiğin üstü için uyarı koyun. Aynı uç noktada sorgu sayısının yayından sonra aniden artması N+1'in imzasıdır.

## İlgili

HATALAR.md · **EF-01 N+1 sorgu**

## English summary

**Root cause:** `AdminOrderService.ListAsync` loaded the page of orders and then, inside a `foreach`, ran one query for the customer and one for the order items of each order: 2 + 2N queries per page (42 for the default 20 rows, 102 for 50 rows). Each query is a separate round trip, which is invisible when the database is on the same machine and costs seconds when it is on another server. The commit "Yönetim sipariş listesi okunabilir hale getirildi" traded a hard-to-step-through projection for a readable loop.

**How to find it:** the `Server-Timing` header (`db;dur=..;desc="N sorgu"`, Development only) in Network → Timing, and how it changes with `pageSize`; EF command logging (`Microsoft.EntityFrameworkCore.Database.Command` at `Information`) showing the repeated `users` and `order_items` queries; the `await`s inside the `foreach`.

**Fix:** a projection (`Select`) that asks for the customer fields and the item totals in the list query itself, so EF produces a single SQL statement (2 queries per request including the count). `Include` over-fetches, lazy loading hides the problem, and batching by ids is a reasonable fallback.

**Prevent:** an integration test that asserts the query count is independent of the number of rows, review of any `await` inside a loop in the service layer, using `Server-Timing` during development, and a per-request database-call metric with an alert in production.
