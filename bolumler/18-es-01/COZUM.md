# Çözüm · Bölüm 18 (EŞ-01) · Son stoğu iki kişi alıyor (overselling)

## Kök neden

`backend/src/Tezgah.Application/Orders/OrderService.cs` içindeki `ReserveStockAsync` "oku, kontrol et, yaz" biçiminde çalışıyor:

```csharp
var stock = await db.StockItems
    .Where(s => s.VariantId == variantId)
    .OrderByDescending(s => s.Quantity - s.Reserved)
    .FirstOrDefaultAsync(ct);                       // 1) oku (izlenen varlık)

if (stock is null || stock.Quantity - stock.Reserved < quantity)   // 2) kontrol et (C#'ta)
    throw new ConflictException("insufficient_stock", "...");

stock.Reserved += quantity;                         // 3) bellekte değiştir
stock.UpdatedAt = clock.GetUtcNow();
return stock.Id;                                    // yazma: SaveChanges'ta (siparişin sonunda)
```

`SaveChanges` EF'in izlediği değişikliği şu SQL'e çevirir:

```sql
UPDATE stock_items SET reserved = @p0, updated_at = @p1 WHERE id = @p2;
```

`@p0`, **uygulamanın hesapladığı sabit bir sayıdır** (okunan `reserved` + adet). "Veritabanındaki güncel değere ekle" demiyor. Sekiz istek aynı anda başladığında:

| Zaman | İstek A | İstek B (… H) | Veritabanı `reserved` (quantity=5) |
| --- | --- | --- | --- |
| t1 | `reserved=4` okur | `reserved=4` okur | 4 |
| t2 | "5-4=1 ≥ 1, yeterli" | "5-4=1 ≥ 1, yeterli" | 4 |
| t3 | `Reserved = 5` yazar, commit | `Reserved = 5` yazar, commit | 5 |
| t4 | sipariş oluştu | sipariş oluştu (hepsi) | **5** |

Sekiz sipariş, bir adetlik yer için "yeterli" kararı verdi; veritabanında `reserved` yalnızca bir kez arttı (kayıp güncelleme, *lost update*). Tek adet stok sekiz müşteriye satıldı (*oversell*), stoktaki sayı ise tutarlı görünüyor. Bu yüzden `ck_stock_items_reserved_valid` (`reserved <= quantity`) kısıtı da hata vermedi: her istek **geçerli** bir değer yazdı, yalnızca yanlış olan karardı.

Pencere, okuma ile `SaveChanges` arasındadır; siparişin tamamı (fiyatlandırma, kupon, kalemler) bu süredir ve onlarca milisaniyedir. İsteklerin bu aralıkta çakışması için yoğun anlar yeter (flaş indirim).

Değişikliği yapan geliştirici şunu düşünmüş (commit `74eb340`):

> Stok ayırma EF ile yazıldı
> Ham ExecuteUpdate yerine izlenen varlık; değişiklik siparişle aynı SaveChanges'ta gidiyor,
> kod daha okunur ve birim testi yazılabilir hale geldi.

Gerekçeler makul: `ExecuteUpdate` EF Core'un bellek içi (in-memory) sağlayıcısında desteklenmez, bu yüzden birim testi yazılamaz; izlenen varlık daha tanıdık görünür. "Siparişle aynı `SaveChanges`" ise zaten gereksizdir; sipariş işlemi (`BeginTransactionAsync`) stok güncellemesini siparişle aynı işleme alıyor. Ama okunabilirlik kazancı, atomikliği silmenin bedeliyle ödendi. Eski metodun açıklaması ("'Yeterli mi?' kontrolü ile düşüm aynı komutta olduğu için aynı anda gelen iki sipariş son ürünü birlikte alamaz") commit'te silindi. `OrderService`'teki "Stok satırları her zaman aynı sırayla kilitlenir" yorumu ise kaldı; artık doğru değil, çünkü okuma kilit almıyor.

**Neden tek kullanıcıyla test edince görünmedi?** Hata zamanlamaya bağlıdır: iki isteğin okuma-yazma aralığının çakışması gerekir. Tek kullanıcı ya da sıralı istekler hiç çakışmaz ve her zaman doğru çalışır. Birim testi bellek içi sağlayıcıyla tek iş parçacığında koşar; gerçek bir veritabanı kilidi, işlem yalıtımı ya da eşzamanlılık yoktur. Kod incelemede de kod "doğru" okunur: kontrol var, hata fırlatma var. Eksik olan, kontrolle yazma arasındaki boşluktur.

## Nasıl bulunur

1. **Belirtiyi yakalayın.** `kontrol 18` çıktısında "Tek kalan ürün N müşteriye satıldı" görürsünüz. Kontrol temizlik yaptığı için sonradan kanıt kalmaz; durumu canlı yakalayın.
2. **Durmadan günlük yazan kesme noktası.** `ReserveStockAsync` içinde `FirstOrDefaultAsync`'ten sonraki satıra bir izleme noktası (tracepoint/logpoint) koyun ve `stock.Id`, `stock.Reserved` yazdırın. Visual Studio: kesme noktası → *Actions* → *Log a message* (`{stock.Id} okunan reserved={stock.Reserved}`), *Continue execution* işaretli kalsın. VS Code: satır numarasına sağ tık → *Add Logpoint…* (`{stock.Id} okunan reserved={stock.Reserved}`). `kontrol 18`'i çalıştırın: sekiz satırın hepsi aynı `reserved` değerini yazar. Sekiz istek aynı eski değeri okumuş demektir. (Normal bir kesme noktası ise tüm süreci durdurur ve yarışı bozar; bu yüzden izleme noktası.)
3. **SQL günlüğü.** `Microsoft.EntityFrameworkCore.Database.Command` düzeyini `Information` yapın. Stok için sekiz `SELECT` ve ardından sekiz `UPDATE stock_items SET reserved = @p0 ...` görürsünüz. `reserved = reserved + ...` yok, sabit parametre var. Parametrenin sekizinde de aynı olduğuna dikkat edin (hassas veri günlüğü açıksa değerler görünür).
4. **Değişmez kuralı (invariant) sorgulayın.** "Ayrılan stok = ödeme bekleyen siparişlerin kalemleri toplamı" olmalıdır:

   ```sql
   SELECT s.id, s.quantity, s.reserved, p.bekleyen
     FROM stock_items s
    CROSS JOIN LATERAL (
          SELECT COALESCE(SUM(oi.quantity), 0) AS bekleyen
            FROM order_items oi
            JOIN orders o ON o.id = oi.order_id
           WHERE oi.stock_item_id = s.id AND o.status = 1      -- 1 = PendingPayment
         ) p
    WHERE s.variant_id = 123                                    -- incelediğiniz ürün
      AND s.reserved <> p.bekleyen;
   ```

   Satır dönerse bir yerde kayıp güncelleme olmuştur. Sorguyu tek ürüne bağlayın: örnek veri her ürün için bu eşitliği kurmamış olabilir ve tüm tabloyu taramak yavaştır. Kontrol aracı temizliği yapmadan önce bakamazsınız; kanıt için kontrol dışı bir yük aracıyla (ör. k6, bombardier) yarışı kurup hemen sorgulayın.
5. **Geçmişe bakın.** `git log -p -- backend/src/Tezgah.Application/Orders/OrderService.cs`: `ExecuteUpdateAsync` ile koşullu UPDATE'in silindiğini ve yerine izlenen varlık güncellemesinin geldiğini gösterir. Yorumdaki "kilitlenir" ifadesi ile kod arasındaki uyumsuzluk da bir ipucudur.
6. **Genel yöntem.** Eşzamanlılık hatasında "iki istek aynı anda olursa ne olur?" sorusunu zaman çizelgesi (yukarıdaki tablo) çizerek yanıtlayın. Bir değeri okuyup karar veren ve sonra yazan her kod parçası şüphelidir.

## Düzeltme

Kontrolü ve artırmayı veritabanında tek, koşullu bir UPDATE'e taşıyın; etkilenen satır sayısı kararı verir:

```diff
-    /// <summary>Stoğu en çok ürünü olan depodan ayırır. Değişiklik siparişle birlikte kaydedilir.</summary>
+    /// <summary>
+    /// Stoğu tek bir koşullu UPDATE ile ayırır. "Yeterli mi?" kontrolü ile düşüm aynı komutta olduğu için
+    /// aynı anda gelen iki sipariş son ürünü birlikte alamaz.
+    /// </summary>
     private async Task<long> ReserveStockAsync(long variantId, int quantity, CancellationToken ct)
     {
-        var stock = await db.StockItems
-            .Where(s => s.VariantId == variantId)
-            .OrderByDescending(s => s.Quantity - s.Reserved)
-            .FirstOrDefaultAsync(ct);
+        var candidates = await db.StockItems.AsNoTracking()
+            .Where(s => s.VariantId == variantId && s.Quantity - s.Reserved >= quantity)
+            .OrderByDescending(s => s.Quantity - s.Reserved)
+            .Select(s => s.Id)
+            .ToListAsync(ct);
 
-        if (stock is null || stock.Quantity - stock.Reserved < quantity)
-            throw new ConflictException("insufficient_stock", "Sepetinizdeki bir ürünün stoğu tükendi. Sepetinizi kontrol edin.");
-
-        stock.Reserved += quantity;
-        stock.UpdatedAt = clock.GetUtcNow();
-        return stock.Id;
+        foreach (var stockItemId in candidates)
+        {
+            var updated = await db.StockItems
+                .Where(s => s.Id == stockItemId && s.Quantity - s.Reserved >= quantity)
+                .ExecuteUpdateAsync(s => s
+                    .SetProperty(x => x.Reserved, x => x.Reserved + quantity)
+                    .SetProperty(x => x.UpdatedAt, clock.GetUtcNow()), ct);
+            if (updated == 1) return stockItemId;
+        }
+        throw new ConflictException("insufficient_stock", "Sepetinizdeki bir ürünün stoğu tükendi. Sepetinizi kontrol edin.");
     }
```

Üretilen SQL:

```sql
UPDATE stock_items
   SET reserved = reserved + @quantity, updated_at = @now
 WHERE id = @id AND quantity - reserved >= @quantity;
```

Neden çalışıyor: PostgreSQL varsayılan yalıtım düzeyinde (`READ COMMITTED`) bir UPDATE satırı kilitler. İkinci UPDATE aynı satıra geldiğinde ilkinin işlemi bitene kadar bekler, sonra `WHERE` koşulunu satırın **güncel** sürümüne göre yeniden değerlendirir. Koşul artık sağlanmıyorsa 0 satır etkilenir, kod bir sonraki depoyu dener, hiçbiri yoksa 409 döner. Kilit sipariş işlemi bitene dek tutulur; işlem geri alınırsa ayırma da geri alınır. Aynı üründe istekler sırayla geçer, farklı ürünler birbirini beklemez. (Sipariş oluşturma kodu kalemleri `VariantId` sırasıyla işler; ters sıralı iki sepetin birbirini kilitlemesini (deadlock) bu önler.)

## Neden böyle düzeltiyoruz

Eşzamanlılık düzeltmelerinin dört klasik yolu vardır; hepsi çalışır, farkları maliyettedir:

| Yol | Nasıl | Artı | Eksi |
| --- | --- | --- | --- |
| **Koşullu tek UPDATE** (seçilen) | `SET reserved = reserved + q WHERE quantity - reserved >= q`, sonuç satır sayısı | Tek gidiş-dönüş, uygulama kodunda kilit yok, sağlam, kısa kod | Karar tek SQL ifadesine sığmalı; EF'in izleyicisini atlar (birim test için gerçek DB gerekir) |
| **`SELECT ... FOR UPDATE`** (kötümser kilit) | Satırı kilitleyerek oku, C#'ta karar ver, yaz | Karmaşık iş kuralları kodda yazılabilir | İki gidiş-dönüş; kilit işlem boyunca tutulur; kilit sırasına dikkat (deadlock); EF LINQ ile desteklemez, ham SQL gerekir |
| **İyimser eşzamanlılık** (`xmin` eşzamanlılık belirteci) | Satırın sürümünü (`xmin`) oku, `UPDATE ... WHERE id = @id AND xmin = @x`; 0 satır → `DbUpdateConcurrencyException` | Kilit tutmaz, çatışma az ise ucuz | Yoğun çekişmede (tek ürüne flaş indirim) çoğu deneme başarısız olur, yeniden deneme fırtınası; çatışmayı `SaveChanges`'ta, sipariş sonunda öğrenirsiniz; yeniden deneme mantığı gerekir; satırın başka sütunlarının değişmesi de çatışma sayılır |
| **Serializable yalıtım** | İşlemi `IsolationLevel.Serializable` ile başlat | Kod aynı kalır, PostgreSQL çakışmayı bulup birini `40001` ile iptal eder | Tüm işlem yeniden denenmeli; yanlış pozitif iptaller; yük altında maliyet; sipariş akışının tamamını etkiler |

Diğer seçenekler:

- **Yeniden deneme eklemek** tek başına çözüm değildir; kontrol ve yazma hâlâ ayrıdır.
- **Uygulama içi kilit (`lock`, `SemaphoreSlim`)** yalnızca tek örnekte (tek sunucuda) çalışır. İki API örneği çalışır çalışmaz bozulur.
- **Dağıtık kilit (Redis vb.)**: yeni bir bağımlılık ve hata kaynağı; veritabanı zaten bu işi atomik yapabiliyor.
- **Sadece CHECK kısıtına güvenmek.** `reserved <= quantity` kısıtı var, ama yukarıda görüldüğü gibi bu hatada her istek geçerli bir değer yazar; kısıt yakalamaz. `SET reserved = reserved + q` ile birlikte kullanılırsa kısıt son savunma hattı olur (ihlalde istisna fırlar), ama bunu 409'a çevirmek koşullu UPDATE'ten daha zahmetlidir.
- **Okunabilirlik/test kaygısı.** Haklıdır; çözüm UPDATE'i atomik bırakıp testi gerçek PostgreSQL ile (ör. Testcontainers) yazmaktır, bellek içi sağlayıcıyla değil. Atomik yöntem küçük bir metottur ve niyeti yorum cümlesiyle açıklanır.

## Bir daha olmaması için

- **Eşzamanlılık testi:** Kontrol 18 gibi bir entegrasyon testi (gerçek PostgreSQL, 8-50 eşzamanlı istek, beklenen sonuç: tam olarak 1 başarı) CI'da çalışsın. Zamanlamaya bağlı hata tek çalıştırmada görünmeyebilir; testi yoğunlaştırın (tur sayısını artırın, bir bariyerle istekleri aynı anda bırakın).
- **Kod inceleme kuralı:** "Bir değeri oku → karara bağla → yaz" kalıbı gören herkes "iki istek aynı anda gelirse?" diye sormalı: sayaç, bakiye, stok, kota, kupon limiti. Böyle yerlerde `x.Alan += n` doğrudan şüphelidir; koşullu UPDATE, kilit ya da eşzamanlılık belirteci olmalı. "Okunabilir hale getirme" değişikliğinde yorumun anlattığı güvenceyi (atomiklik) kaldırmayın.
- **Veri tutarlılığı kontrolü:** Yukarıdaki değişmez kural sorgusunu (ayrılan stok = bekleyen kalemler toplamı) periyodik bir işle çalıştırıp fark çıkarsa uyarı verin. Aşırı satış büyük olaya dönmeden fark edilir.
- **Savunma hattı:** `CHECK (reserved >= 0 AND reserved <= quantity)` kısıtı kalsın; kod hatalı olsa bile veritabanı bozuk duruma izin vermez (çoğu zaman).
- **Yük testi:** Kampanya öncesi tek ürüne eşzamanlı yük (k6, bombardier) deneyin.
- **İzleme:** 409 `insufficient_stock` oranı, stok bitişinde satılan sipariş sayısı ile stok farkı, iptal/iade sayıları.
- **Olay sonrası veri:** Zaten fazla satılmış siparişler için iş kararı gerekir (geç gelen siparişleri iptal etmek, beklemeye almak, tedarik etmek). `reserved` değerini sipariş toplamına çekmek `reserved <= quantity` kısıtına takılabilir; önce hangi siparişlerin iptal edileceğine karar verin.

## İlgili

HATALAR.md · **EŞ-01 Son stoğu iki kişi alıyor (overselling)**

## English summary

**Root cause:** `ReserveStockAsync` loaded the stock row as a tracked entity, checked `Quantity - Reserved < quantity` in C#, and then did `stock.Reserved += quantity`; EF sent `UPDATE stock_items SET reserved = @newValue`. Concurrent orders all read the same `reserved`, all passed the check, and all wrote the same new value: a lost update and overselling. The `reserved <= quantity` check constraint did not help because each request wrote a valid value. The commit "Stok ayırma EF ile yazıldı" removed the atomic conditional `ExecuteUpdate` for readability and testability. It never showed with one user because the bug needs overlapping requests; in-memory unit tests have no concurrency.

**How to find it:** run `kontrol 18`; put a non-stopping tracepoint/logpoint after the read and see eight requests reading the same `reserved`; EF SQL log showing `SET reserved = @p0`; the invariant query (reserved = sum of pending order items); `git log -p`.

**Fix:** a single conditional UPDATE: `SET reserved = reserved + @q WHERE id = @id AND quantity - reserved >= @q`, and check the affected row count (try the next stock row, or throw 409). Alternatives: `SELECT ... FOR UPDATE` (pessimistic), optimistic concurrency with `xmin` (retry storms on hot rows), Serializable isolation (retries, overhead).

**Prevent:** a real-database concurrency test, review of read-decide-write patterns, a periodic invariant check, keeping the check constraint as a last line of defence, load tests before campaigns.
