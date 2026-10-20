# Çözüm · Bölüm 8 (NET-03) · NullReferenceException

## Kök neden

`backend/src/Tezgah.Application/Orders/OrderService.cs` içindeki `GetAsync` metodu teslimat adresini şöyle eşliyor:

```csharp
var a = order.ShippingAddress!;
var address = new OrderAddressDto(a.FullName, a.Phone, a.City, a.District, a.Line, a.PostalCode);
```

Sondaki `!` (null-forgiving operatörü) derleyiciye "bunun `null` olmadığına söz veriyorum" der; çalışma anında hiçbir şey kontrol edilmez. Eski sistemden taşınan siparişlerin adresi yoktur: örnek veride 540 günden eski siparişlerin yaklaşık yüzde 4'ünde teslimat adresi sütunları (`shipping_*`) boştur. EF Core, sahip olunan türün (`OwnsOne`) bütün sütunları `null` ise nesneyi `null` getirir. `a` `null` olur, ikinci satırda `a.FullName` okunurken `NullReferenceException` fırlar, API `500` döner.

Değişikliği yapan geliştirici şunu düşünmüş (commit `7f875b1`):

> Sipariş detayı: adres eşlemesi sadeleştirildi
> Teslimat adresi sipariş oluşturulurken zorunlu, boş olma ihtimali yok.

Yeni siparişler için doğru: sipariş verilirken adres zorunlu. Ama veritabanındaki bütün siparişler yeni değil. "Kodda zorunlu" ile "veride her zaman dolu" aynı şey değildir; geçmiş veri, taşıma ve eski kurallar farklı sonuç üretir. Önceki kod (`order.ShippingAddress is { } a ? new OrderAddressDto(...) : null`) bunu zaten ele alıyordu ve `OrderDetailDto.ShippingAddress` `null` kabul ediyor (`OrderAddressDto?`). Müşteri sitesi de `null` durumunda "Adres bilgisi yok." gösteriyor. "Sadeleştirme" gerçekte bir koruma kaldırdı.

## Nasıl bulunur

1. **Belirti:** Yalnızca bazı eski siparişlerde hata kutusu; mesaj genel. Ağ sekmesinde `GET /api/orders/{no}` `500`.
2. **Araç: API logu.** API'nin terminalinde `Beklenmeyen hata: /api/orders/TZ-2026-...` satırı ve altında örneğin:

   ```text
   System.NullReferenceException: Object reference not set to an instance of an object.
      at Tezgah.Application.Orders.OrderService.GetAsync(Int64 userId, String orderNumber, CancellationToken ct) in .../OrderService.cs:line 256
      at Tezgah.Api.Controllers.OrdersController.Get(...)
   ```

   Yığın izini yukarıdan aşağı okuyun: ilk `Tezgah` satırı hatanın fırlatıldığı yerdir (`OrderService.GetAsync`, `line 256`). Altındakiler "kim çağırdı" bilgisidir. `Microsoft...` satırlarını şimdilik atlayın.
3. **Dosya ve satır:** `OrderService.cs` 256. satır: `a.FullName`. Hangi nesne `null`? `a`. `a` nereden geliyor? Bir önceki satır: `order.ShippingAddress!`.
4. **Veri:** Neden bazı siparişlerde? Etkilenen siparişin satırına bakın: `docker exec -it tezgah-postgres-1 psql -U tezgah` içinde `SELECT order_number, shipping_full_name, shipping_city FROM orders WHERE order_number = '<no>';` sütunlar `NULL`. Genel dağılım için: `SELECT count(*) FILTER (WHERE shipping_full_name IS NULL), count(*) FROM orders;` ve `placed_at` ile eskiliğe göre kıyaslayın.
5. **Bağlam:** `git log -p -- backend/src/Tezgah.Application/Orders/OrderService.cs` son commit'in `is { } a ? ... : null` kalıbını `!` ile değiştirdiğini gösterir. `kontrol 8` aracı da aynı veriyi (`shipping_full_name IS NULL`) bulup sahibini yazdırır.

**Visual Studio:** *Exception Settings* (Ctrl+Alt+E) → "Common Language Runtime Exceptions" işaretli olsun; ya da `OrderService.GetAsync` içine koşullu kesme noktası (`orderNumber == "<no>"`) koyun. Durduğunda *Locals* penceresinde `order.ShippingAddress` `null` görünür.

**VS Code:** "API (monolit)" yapılandırması; *Breakpoints* bölümünde "All Exceptions"ı açın ya da aynı satıra koşullu kesme noktası koyun. *Variables* panelinde `order` altındaki `ShippingAddress`'e bakın.

## Düzeltme

```diff
-        var a = order.ShippingAddress!;
-        var address = new OrderAddressDto(a.FullName, a.Phone, a.City, a.District, a.Line, a.PostalCode);
+        var address = order.ShippingAddress is { } a
+            ? new OrderAddressDto(a.FullName, a.Phone, a.City, a.District, a.Line, a.PostalCode)
+            : null;
```

Yanıt şeması aynı kalır: adres yoksa `shippingAddress: null`.

## Neden böyle düzeltiyoruz

- **`try/catch (NullReferenceException)`** hatayı gizler, bütün metodu ve başka olası `null`'ları kapsar; ayrıca yavaş ve belirsizdir.
- **Boş bir adres uydurmak** (`new OrderAddressDto("", "", ...)`) "adres yok" bilgisini "adres boş string" ile karıştırır; fatura ve kargo süreçleri yanlış veriyle çalışır. `null` dürüst cevaptır.
- **Veritabanı sütununu `NOT NULL` yapmak** eski kayıtlarda uygulanamaz; kayıtlar var ve değiştirilemez ya da değiştirilmemelidir. Veriyi sonradan düzeltmek ayrı bir iştir (eski sistemden adres bulunabilirse) ve kodun `null`'a dayanıklı olmasını gereksiz kılmaz.
- **`!` kullanmaya devam edip veriyi temizlemek** her yeni taşımada aynı riski bırakır.

## Bir daha olmaması için

- **Derleyici uyarıları:** `Nullable` zaten açık (`Tezgah.Application.csproj`). `!` kullanımı derleyici uyarılarını susturur; kod incelemede "bu `!`'in doğru olduğuna dair kanıt nedir?" sorulsun. Gerekirse analizör kuralıyla `!` kullanımını uyarıya çevirin ve nedenini yoruma yazdırın.
- **Test:** `GetAsync` için adresi `null` olan bir siparişle birim/entegrasyon testi. Bu bölümün kontrolü tam olarak bunu gerçek veriyle sınıyor.
- **Gerçekçi test verisi:** Üretimdeki veri dağılımını (eski kayıtlar, taşınan kayıtlar, boş alanlar) yansıtan örnek veri kullanın; DataGen'in %4 eski kayıt üretmesi bu yüzden. Yeni kodun test ortamında yalnızca "iyi" veriyle çalışması yanıltıcıdır.
- **İzleme:** Beklenmeyen `500`'lerde log satırı sipariş numarasını içeriyor; `NullReferenceException` sayısı için uyarı kurun. Bir istisna türü ilk kez ve belli bir uçta artıyorsa bakılmalıdır.
- **Kod inceleme maddesi:** "Sadeleştirme" PR'ında kaldırılan her kontrolün (`is { }`, `?.`, `if`) neden gereksiz olduğu veriyle gösterilsin, yorumla değil.

## İlgili

HATALAR.md · **NET-03 NullReferenceException**

## English summary

**Root cause:** `OrderService.GetAsync` used `var a = order.ShippingAddress!;` and then read `a.FullName`. Orders migrated from the legacy system (about 4% of orders older than 540 days in the sample data) have no address columns, so EF Core materializes `ShippingAddress` as `null` and the next line throws `NullReferenceException`, giving a 500. The developer (commit "Sipariş detayı: adres eşlemesi sadeleştirildi") assumed the address is always present because it is mandatory when an order is created; that is not true for old data. The `!` only silences the compiler.

**How to find it:** the generic error box -> API log: `NullReferenceException` with `OrderService.cs:line 256` as the first `Tezgah` frame -> check the order row in the database (`shipping_*` columns are `NULL`) -> `git log -p`.

**Fix:** restore the null-safe mapping: `order.ShippingAddress is { } a ? new OrderAddressDto(...) : null`. The DTO and the storefront already support `null`.

**Prevent:** question every `!` in review, test with an address-less order, use realistic sample data, and alert on unexpected exception types.
