> **Cevap anahtarı.** Bölümü kendiniz çözmeden okumayın; önce `ipucu` komutunu deneyin.

# Çözüm · Bölüm 28 (API-10) · Asıl hatayı yutmak: her şeyi tek mesaja çevirmek

## Kök neden

`backend/src/Tezgah.Application/Carts/CartService.cs` içindeki `ApplyCouponAsync`, kupon doğrulamasını geniş bir `try/catch` ile sarmış:

```csharp
Coupon coupon;
try
{
    coupon = await coupons.ValidateAsync(couponCode, owner.UserId, ct);
}
catch (Exception ex)
{
    // Sebep loga yazılır; müşteriye tek mesaj döner (geçerli kodları tahmin etmeyi zorlaştırmak için).
    logger.LogWarning(ex, "Kupon uygulanamadı: {CouponCode}", couponCode);
    throw new DomainException("coupon_rejected", "Kupon uygulanamadı.");
}
```

`CouponValidator` her durumda anlamlı bir `DomainException` fırlatıyor: `coupon_not_found` ("Kupon kodu geçersiz."), `coupon_not_started`, `coupon_expired` ("Bu kuponun süresi doldu."), `coupon_exhausted`, `coupon_requires_login`, `coupon_already_used`. Hatayı yakalayıp **yerine yeni bir istisna** atmak bu kodları ve mesajları siler: istemci her durumda aynı `coupon_rejected` / "Kupon uygulanamadı." cevabını alır. Sebep yalnızca loga gider; müşteri ise çağrı merkezini arar.

Değişikliği yapan geliştirici şunu düşünmüş (commit `abc1234`):

> Kupon hataları loglanıyor, müşteriye tek mesaj dönüyor
> Güvenlik notu: "kod yok" ile "süresi dolmuş" ayrı mesajlar geçerli kupon kodlarını tahmin etmeyi kolaylaştırıyordu. Ayrıntı artık logda.

Kaygı tamamen yersiz değil ama çözüm orantısız: kupon kodunu tahmin etmek için saldırganın kodları tek tek denemesi gerekir ve "kod yok" ile "süresi dolmuş" arasındaki fark bu denemeyi kolaylaştıran bir kaynak değildir; kod zaten denenerek bulunuyor. Bir saldırganın deneme sayısını sınırlayan doğru önlem **hız sınırlamadır (rate limiting)** ve yanlış denemeleri izlemektir; mesajı bulanıklaştırmak dürüst kullanıcıyı cezalandırır. Ayrıca `catch (Exception)` yalnızca doğrulama hatalarını değil **her şeyi** yakalar: veritabanı hatası, iptal, null başvurusu da aynı "Kupon uygulanamadı." mesajına dönüşür ve gerçek bir arıza kullanıcıya "kupon sorunu" gibi görünür.

## Nasıl bulunur

1. **Belirti:** Farklı nedenlerle reddedilen kuponların hepsi aynı mesajı veriyor. Bir hatanın "tek tip" olması, nedenin bir yerde kaybolduğunu gösterir.
2. **Sunucu logu:** `docker compose logs api` içinde `Kupon uygulanamadı: YOKBOYLEKUPON` satırı ve altında asıl `DomainException` (`coupon_not_found`, "Kupon kodu geçersiz.") vardır. Süresi geçmiş kodda `coupon_expired`. Neden logda var, yanıtta yok.
3. **Network sekmesi:** İstek yanıtında `code: "coupon_rejected"`, `title: "Kupon uygulanamadı."`; logdaki kodla uyuşmuyor.
4. **Kod:** `ApplyCouponAsync` içindeki `catch (Exception ex)` bloğu ve içindeki `throw new DomainException(...)`. `git log -p -- backend/src/Tezgah.Application/Carts/CartService.cs` eklenen bloğu gösterir.

**Visual Studio / VS Code:** `catch` bloğuna kesme noktası koyup var olmayan bir kuponu uygulayın; `ex` değişkeninde özgün `DomainException`'ı ve kodunu görürsünüz.

## Düzeltme

`try/catch`'i kaldırın; istisna merkezi hata işleyiciye yükselsin ve özgün kod/mesaj istemciye ulaşsın:

```diff
-        Coupon coupon;
-        try
-        {
-            coupon = await coupons.ValidateAsync(couponCode, owner.UserId, ct);
-        }
-        catch (Exception ex)
-        {
-            // Sebep loga yazılır; müşteriye tek mesaj döner (geçerli kodları tahmin etmeyi zorlaştırmak için).
-            logger.LogWarning(ex, "Kupon uygulanamadı: {CouponCode}", couponCode);
-            throw new DomainException("coupon_rejected", "Kupon uygulanamadı.");
-        }
+        var coupon = await coupons.ValidateAsync(couponCode, owner.UserId, ct);
```

(Yalnızca bu bölüm için eklenen `ILogger` bağımlılığı ve `using Microsoft.Extensions.Logging;` satırı da kaldırılabilir.) Geliştirici illa log istiyorsa, **dar** yakalayıp özgün hatayı yeniden fırlatın:

```csharp
catch (DomainException ex) when (ex.Code.StartsWith("coupon_"))
{
    logger.LogInformation("Kupon reddedildi: {Code} ({CouponCode})", ex.Code, couponCode);
    throw;   // yeni bir istisna değil, aynı istisna
}
```

## Neden böyle düzeltiyoruz

- **Hata mesajını sabit tutup ayrıntıyı logda bırakmak.** Kullanıcı için işe yarayan bilgi kullanıcıdan saklanmış olur; destek maliyeti artar. Güvenlik gerekçesi geçerli kupon kodlarının sızması idiyse mesajı bulanıklaştırmak yerine **deneme sayısı** sınırlanır.
- **`catch (Exception)`'ı korumak.** Her şeyi aynı mesaja çevirir: gerçek bir sistem hatası bile "kupon sorunu" sanılır ve arızalar fark edilmez. Yakalamanız gerekiyorsa türü ve koşulu daraltın (`when` filtresi).
- **Yeni istisna fırlatırken eskisini `InnerException` olarak taşımak.** Logda iz kalır ama istemciye giden kod/mesaj yine silinir; sorunun asıl kısmı çözülmez.
- **İstemcide mesajı tahmin etmek.** İstemci sebebi bilmiyor; doğru bilgi sunucudan gelmeli.

## Bir daha olmaması için

- **Test:** `ApplyCouponAsync` için her kupon reddi senaryosunda beklenen **hata kodunu** doğrulayan testler (`coupon_not_found`, `coupon_expired`, ...). Bölümün kontrolü de var olmayan ve süresi geçmiş kodlarla farklı yanıtlar bekler.
- **Hız sınırlaması:** Kupon uygulama ucuna kullanıcı/IP başına dakikalık deneme sınırı ve art arda başarısız denemeler için bekleme süresi; asıl kod tahmini riski bununla azalır.
- **Kod inceleme maddesi:** `catch (Exception)` + yeni istisna ya da `catch` içinde sabit bir mesaj gören yoruma sorun: "Özgün hata bilgisi kayboluyor mu? Daha dar yakalanabilir mi? Yeniden fırlatmak (`throw;`) yeterli mi?"
- **İzleme:** Reddedilen kuponları koda göre sayın (`coupon_not_found` ani artışı tahmin saldırısının işaretidir); tek tip mesajda bu sayım yapılamaz.

## İlgili

HATALAR.md · **API-10 500 ama asıl hata yutulmuş**. Bu bölümde yanıt 500 değil ama mekanizma aynı: asıl hata yakalanıp başka bir şeye çevriliyor.

## English summary

**Root cause:** `CartService.ApplyCouponAsync` wraps `coupons.ValidateAsync(...)` in `try/catch (Exception)`, logs the original error and throws a new `DomainException("coupon_rejected", "Kupon uygulanamadı.")`. `CouponValidator` throws meaningful errors (`coupon_not_found`, `coupon_expired`, `coupon_exhausted`, ...), but the replacement exception erases their codes and messages, so every rejection looks the same to the customer. The developer (commit "Kupon hataları loglanıyor, müşteriye tek mesaj dönüyor") wanted to make guessing valid coupon codes harder, but guessing needs one try per code anyway; the right measure is rate limiting, not hiding the reason. `catch (Exception)` also turns unrelated failures (database errors, cancellation) into a "coupon problem".

**How to find it:** the API log shows the real exception (`coupon_not_found` / `coupon_expired`) while the response says `coupon_rejected`; the Network tab and the code disagree; the `catch` block in `ApplyCouponAsync` and `git log -p`.

**Fix:** remove the `try/catch` so the original `DomainException` reaches the central handler; if logging is needed, catch narrowly (`when` filter) and rethrow with `throw;`.

**Prevent:** tests that assert the specific error code per rejection, rate limiting on the coupon endpoint, review attention to broad `catch` blocks that replace exceptions, and monitoring of rejected coupons by code.
