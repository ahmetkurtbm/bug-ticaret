# Çözüm · Bölüm 14 (NET-11) · Saat dilimi ve "günün başı"

## Kök neden

`backend/src/Tezgah.Application/Admin/AdminService.cs` içindeki `StartOfDay` fonksiyonu değişti:

```csharp
// Tüm zamanlar veritabanında UTC; gün başı da UTC olarak alınır.
private static DateTimeOffset StartOfDay(DateOnly day) => new(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
```

Fonksiyon, kullanıcının seçtiği takvim gününü (`DateOnly`) bir ana (`DateTimeOffset`) çeviriyor. Ofset olarak `TimeSpan.Zero` verildiği için "18 Kasım" = `2026-11-18T00:00:00Z`. Türkiye `UTC+3` kullanır; bu an İstanbul saatiyle 18 Kasım 03:00'tür. Kupon oluşturulurken hem başlangıç (`StartOfDay(StartsOn)`) hem bitiş (`StartOfDay(EndsOn.AddDays(1))`) aynı fonksiyondan geçtiği için iki uç da 3 saat geç olur:

| | Doğru (İstanbul günü) | Hatalı |
| --- | --- | --- |
| Başlangıç, 18 Kasım | `2026-11-17T21:00:00Z` (18 Kasım 00:00) | `2026-11-18T00:00:00Z` (18 Kasım 03:00) |
| Bitiş günü 19 Kasım (dahil) | `2026-11-19T21:00:00Z` (20 Kasım 00:00) | `2026-11-20T00:00:00Z` (20 Kasım 03:00) |

Kupon doğrulayıcı (`CouponValidator`) "şimdi `StartsAt`'tan önce mi?" diye bakar; 00:00–03:00 arasında "Bu kupon henüz kullanıma açılmadı." cevabı bu yüzden döner.

Yönetim paneli (`frontend/apps/admin/src/lib/dates.ts`) tarihleri `Europe/Istanbul` saat dilimiyle biçimler. Yani panelde `03:00` görünür: panel doğru çalışıyor, **veri** yanlış. Kupon listesinde bitiş günü de bir gün sonrası görünür: panel bitişi "bitiş anından 1 ms önce" olarak biçimler (`formatInclusiveEndDay`), hatalı bitiş anı İstanbul saatiyle ertesi gün 03:00 olduğu için bu ana 1 ms kala hâlâ ertesi gündür.

Değişikliği yapan geliştirici şunu düşünmüş (commit `dbb46e0`):

> Kampanya tarihleri tutarlı biçimde UTC'ye çevriliyor

Kural olarak doğru bir şey düşünmüş: zamanlar veritabanında UTC saklanmalı. Ama karıştırdığı iki ayrı soru var:

1. **Bir an nasıl saklanır?** UTC (`timestamptz`). Doğru ve değişmedi.
2. **"18 Kasım'ın başlangıcı" hangi andır?** Bu soru bir takvime sorulur, UTC'ye değil. Kampanya günlerini İstanbul'daki pazarlama ekibi İstanbul takvimiyle girer (`docs/MIMARI.md`: gün sınırları `Europe/Istanbul` takvimine göredir). Cevap `2026-11-17T21:00:00Z`.

UTC'de saklamak ile gün sınırını UTC'de çizmek aynı şey değildir. Eski kod ikisini ayırıyordu ve bunu açıklayan yorum ("Kampanya günleri İstanbul takvimiyle") commit'te silindi. Dosyadaki `Istanbul` alanı ise artık kullanılmayan bir kalıntı olarak kaldı.

## Nasıl bulunur

1. **Veriye bakın.** Panelden `DENEME1` gibi bir kupon oluşturun. Network sekmesinde yanıtta `startsAt` şöyle görünür: `2026-11-18T00:00:00+00:00`. Veritabanında:

   ```bash
   docker exec -it tezgah-postgres-1 psql -U tezgah -c "select code, starts_at, ends_at from coupons order by id desc limit 3"
   ```

   Aynı değeri İstanbul saatinde görmek için: `select starts_at at time zone 'Europe/Istanbul' from coupons order by id desc limit 1;`. Sonuç `03:00:00`.
2. **Beklenen değeri hesaplayın.** "18 Kasım 00:00 İstanbul" = UTC'de 17 Kasım 21:00. Gerçek ile beklenen arasındaki fark tam 3 saat, yani saat dilimi ofseti. Rastgele bir fark olsaydı başka şüphelenirdiniz.
3. **Katmanı daraltın.** Veri API yanıtında ve veritabanında zaten yanlış; panel yalnızca gösteriyor. Yani hata sunucuda, kupon oluşturma yolunda: `AdminController` → `AdminService.CreateCouponAsync`.
4. **Kesme noktası.** `CreateCouponAsync` içinde `StartsAt = StartOfDay(request.StartsOn)` satırına koyun. Visual Studio'da *Locals* penceresinde `request.StartsOn`, sonra *Step Into* ile `StartOfDay` içine girip dönüş değerinin `Offset`'ine bakın (`+00:00`). VS Code'da C# Dev Kit ile aynı: *Variables* panelinde `Offset`.
5. **Kullanılmayan alan.** Dosyada `private static readonly TimeZoneInfo Istanbul` tanımlı ama hiçbir yerde kullanılmıyor. IDE gri gösterir (VS: IDE0052). `git log -p -- backend/src/Tezgah.Application/Admin/AdminService.cs` ya da `git log -S"Istanbul.GetUtcOffset"` kullanımın silindiği commit'i gösterir.

## Düzeltme

```diff
-            // Bitiş günü dahil (ertesi gün 00:00'a kadar).
+            // Kampanya günleri İstanbul takvimiyle; bitiş günü dahil (ertesi gün 00:00'a kadar).
             StartsAt = StartOfDay(request.StartsOn),
@@
-    // Tüm zamanlar veritabanında UTC; gün başı da UTC olarak alınır.
-    private static DateTimeOffset StartOfDay(DateOnly day) => new(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
+    private static DateTimeOffset StartOfDay(DateOnly day)
+    {
+        var local = day.ToDateTime(TimeOnly.MinValue);
+        return new DateTimeOffset(local, Istanbul.GetUtcOffset(local)).ToUniversalTime();
+    }
```

`Istanbul.GetUtcOffset(local)` o gün için geçerli ofseti verir. Sonuç UTC'ye çevrilip saklanır: saklama kuralı (UTC) korunur, gün sınırı kuralı (İstanbul) geri gelir.

### Veri onarımı

Hatalı sürüm yayındayken oluşturulan kuponların `starts_at`/`ends_at` değerleri 3 saat geç yazılmıştır. Sadece **şimdi geçerli olması gereken ya da ileride başlayacak** kuponlar önemlidir; geçmiş kampanyaların geri alınabilecek bir etkisi yoktur. Önce bakın:

```sql
SELECT id, code, starts_at, ends_at
  FROM coupons
 WHERE (starts_at AT TIME ZONE 'UTC')::time = '00:00'   -- UTC gece yarısı = İstanbul 03:00
   AND ends_at > now();
```

Satırlar gerçekten hatalı sürümden geliyorsa (oluşturma tarihine ve sahibine bakın), değerleri 3 saat geri alın ve `audit_logs`'taki oluşturma kaydı ile (başlangıç/bitiş günlerini düz tarih olarak içerir) karşılaştırın. Düzeltmeyi SQL ile yaparsanız `- interval '3 hours'` kullanmak yerine her satırı kendi `starts`/`ends` günlerinden yeniden hesaplamak daha güvenlidir; çünkü sabit "3 saat" yalnızca bugünkü ofsete bağlıdır.

## Neden böyle düzeltiyoruz

- **UTC saklamak yanlış değil, gün sınırı kuralı yanlış.** Veritabanında `timestamptz` kalır. Gün başını bulmak bir iş kuralıdır ve iş yerinin takvimine (İstanbul) göre verilir. Saat dilimi dönüşümü yalnızca sınırda yapılır: gelen gün → an, gösterilen an → gün.
- **`+3` saati elle eklemek (`TimeSpan.FromHours(3)`).** Bugün çalışır. Ama Türkiye de ofsetini kararla değiştirebilir (2016'ya kadar yaz saati vardı); `TimeZoneInfo` kuralları işletim sistemi/tz veritabanı günceldir, elle yazılan sabit değildir. Başka bir bölge desteklenirse (yaz saatli bir ülke) sabit yanlış çıkar.
- **`DateTime.Now`, `ToLocalTime()` ya da `TimeZoneInfo.Local`.** Sunucunun saat dilimine bağlıdır. Sunucu UTC'de (ya da başka bir bölgede) çalışırsa aynı hatayı verir; "bende çalışıyor" sınıfı bir hata doğurur (HATALAR.md NET-11).
- **Düzeltmeyi panelde yapmak (başlangıca 3 saat çıkarıp göndermek).** API'yi kullanan diğer istemciler yine hatalı veriyi görür, iş kuralı iki yerde yaşar. Doğru yer, kuralın sahibi olan servistir.
- **Kampanya günlerini `DateOnly` olarak saklamak.** Bazı sistemlerde daha doğrudur: `starts_on`/`ends_on` günleri saklanır, "şimdi kampanya içinde mi?" sorusu çalışma anında İstanbul gününe çevrilerek sorulur. Bu tasarım zaman dilimi kurallarının değişmesine karşı en dayanıklısıdır. Ama şema değişikliği ister; bu bölümün düzeltmesi mevcut tasarımı onarır.
- **NodaTime.** Boşluk/çakışan yerel saat (yaz saati geçişi) gibi durumları açıkça yönetir (`LocalDate.AtStartOfDayInZone`). Türkiye'de yaz saati olmadığı için burada gerekmez, ama yaz saatli bölgelerde gece yarısı hiç olmayan ya da iki kez yaşanan bir saat olabilir; `TimeZoneInfo` ile `new DateTimeOffset(local, offset)` böyle durumlarda dikkat ister.

## Bir daha olmaması için

- **Test:** `StartOfDay` mantığını saf bir fonksiyona (ör. `CampaignCalendar.StartOfDay(DateOnly, TimeZoneInfo)`) çıkarın ve gün sınırlarını doğrudan sınayın: `2026-11-18` → `2026-11-17T21:00:00Z`; bitiş için ertesi günün başı. "Şimdi" ile ilgili mantıkta (kupon başlamış mı?) `TimeProvider` kullanın ve testte sahte zaman sağlayıcıyla (ör. `FakeTimeProvider`) 23:59:59 / 00:00:00 / 00:00:01 sınırlarını deneyin. Bu proje `AdminService` içinde `TimeProvider` zaten kullanıyor.
- **Ortam:** Testleri farklı saat dilimleriyle çalıştırın (`TZ=UTC` ve `TZ=Europe/Istanbul`). Hatalı kod makineden bağımsız olduğu için bunu yakalamaz, ama `DateTime.Now` kullanan benzer hataları yakalar.
- **Kod inceleme:** "Tutarlılık" ya da "sadeleştirme" gerekçesiyle kaldırılan saat dilimi dönüşümü şüphelidir. Silinen yorumlara ve kullanılmaz hâle gelen alanlara (`Istanbul`) bakın.
- **İzleme:** Kampanya başlangıçlarında "başlama saatinde ilk 5 dakikadaki `coupon_not_started` hataları" sayacı: gece yarısı beklenen açılışta bu hata yağıyorsa alarm verir.
- **Arayüz:** Kupon formunda oluşturulduktan sonra "İstanbul saatiyle 18 Kasım 00:00 – 19 Kasım 23:59" biçiminde tam başlangıç/bitiş saatini göstermek, bu tür sapmaları kullanıcı gözüyle erken yakalar.

## İlgili

HATALAR.md · **NET-11 DateTime.Now ve saat dilimi**

## English summary

**Root cause:** `AdminService.StartOfDay` was changed to build the day start as `new DateTimeOffset(day 00:00, TimeSpan.Zero)`, i.e. 00:00 UTC, which is 03:00 in Istanbul (UTC+3). Both the start and the end of a coupon go through it, so both ends are 3 hours late. The commit "Kampanya tarihleri tutarlı biçimde UTC'ye çevriliyor" mixed up two questions: storing instants in UTC (correct) and deciding which instant "the start of 18 November" is (a question for the Istanbul calendar). The admin panel formats times in Istanbul time, so it shows 03:00 correctly; the data is wrong. The end day also looks one day late in the list.

**How to find it:** compare `startsAt` in the API response and `starts_at` in the database with the hand-computed expected value (a difference of exactly 3 hours); breakpoint in `StartOfDay` and inspect `Offset`; notice the unused `Istanbul` field and use `git log -S`.

**Fix:** compute the Istanbul midnight with `Istanbul.GetUtcOffset(local)` and convert to UTC, as before. Also repair upcoming coupons created by the faulty version.

**Prevent:** unit tests for day boundaries with a fake `TimeProvider`, tests under several time zones, review of removed time zone code and comments, and a metric for `coupon_not_started` errors at campaign start.
