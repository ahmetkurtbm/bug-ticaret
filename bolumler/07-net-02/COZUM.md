# Çözüm · Bölüm 7 (NET-02) · Middleware sırası

## Kök neden

`backend/src/Tezgah.Notifications/Program.cs` içinde anahtar kontrolü `UseRouting()` çağrısından önce eklenmiş:

```csharp
app.UseMiddleware<ApiKeyMiddleware>();   // routing henüz çalışmadı
app.UseExceptionHandler();
app.UseRouting();
```

`ApiKeyMiddleware` hangi uca anahtar gerektiğini uç noktanın (endpoint) metadata'sından okur:

```csharp
var required = context.GetEndpoint()?.Metadata.GetMetadata<RequireApiKeyMetadata>() is not null;
```

İsteği bir uç noktaya eşleyen şey `UseRouting()`'dir. Ondan önce `GetEndpoint()` `null` döner, `required` `false` olur ve middleware isteği `next`'e iletir. Yani anahtar kontrolü hiç çalışmaz; korumalı uçlar (`/api/notifications`) anahtarsız açılır. Bu bir güvenlik açığıdır: müşteri e-posta adresleri ve bildirim içerikleri herkese açılır.

Bu kodun "hata vermeden yanlış çalışması" asıl tehlike. Derleyici uyarmaz, ara katman 500 üretmez, hiçbir log satırı yoktur. Kontrol "emin değilsem geç" şeklinde yazılmış: bilinmeyen durum izin demek. Güvenlik kodunda böyle olmaz.

Değişikliği yapan geliştirici şunu düşünmüş (commit `2d14121`):

> Bildirim servisi: anahtar kontrolü pipeline'ın başına alındı

ve kodda şu yorumu bırakmış: "Güvenlik kontrolleri her şeyden önce çalışsın: anahtar kontrolü pipeline'ın en başında." Genel kural olarak mantıklı: yetkisiz isteği olabildiğince erken kes. Ama bu middleware, routing'in sonucuna ihtiyaç duyar. Commit ayrıca önceki sürümdeki, sıranın neden önemli olduğunu anlatan yorum bloğunu da silmiş; bu yorum tam bu hatayı önlüyordu.

## Nasıl bulunur

1. **Belirti:** Anahtarsız ve yanlış anahtarlı `curl` 200 dönüyor, doğru anahtarla da 200. Anahtarın etkisi yok.
2. **Araç: curl.** `curl -i http://localhost:5095/api/notifications` (başlıksız). `/health` ile aynı davranış.
3. **Araç: kesme noktası.** `ApiKeyMiddleware.InvokeAsync` içindeki `required` satırına kesme noktası koyun. Metot çağrılıyor; `context.GetEndpoint()` değeri `null`. Hata ayıklayıcı en hızlı yol: "kontrol çalışmıyor" ile "kontrol çalışıyor ama atlıyor" ayrımını yapar.
4. **Sebep:** `GetEndpoint()` ne zaman dolar? `UseRouting()` çalıştıktan sonra. `Program.cs` içindeki sıraya bakın.
5. **Dosya:** `backend/src/Tezgah.Notifications/Program.cs`. `git log -p -- backend/src/Tezgah.Notifications/Program.cs` sıranın değiştiğini ve yorum bloğunun silindiğini gösterir.
6. **Deneme:** `UseMiddleware<ApiKeyMiddleware>()` satırını `UseRouting()`'in altına alın, yeniden başlatın, kesme noktasında `GetEndpoint()` artık `HTTP: GET /api/notifications` gibi bir uç noktayı gösterir.

**Visual Studio:** Çözümde `Tezgah.Notifications`'ı başlangıç projesi yapıp F5; *Locals* veya *Immediate* penceresine `context.GetEndpoint()` yazın.

**VS Code:** "Bildirim servisi" yapılandırmasıyla başlatın; *Debug Console*'da `context.GetEndpoint()` ifadesini çalıştırın.

## Düzeltme

```diff
-// Güvenlik kontrolleri her şeyden önce çalışsın: anahtar kontrolü pipeline'ın en başında.
-app.UseMiddleware<ApiKeyMiddleware>();
-app.UseExceptionHandler();
-app.UseRouting();
+// Middleware sırası önemlidir:
+//  1. UseExceptionHandler en başta: sonrasındaki her şeyin (anahtar kontrolü dahil) fırlattığı hata ProblemDetails'e çevrilir.
+//  2. UseRouting anahtar kontrolünden ÖNCE: ApiKeyMiddleware hangi uca anahtar gerektiğini endpoint metadata'sından
+//     okur (GetEndpoint). Routing henüz çalışmadıysa endpoint null olur, kontrol atlanır ve korumalı uçlar anahtarsız açılır.
+//  3. Anahtar kontrolü endpoint'lerden ÖNCE: ucun kodu çalıştıktan sonra reddetmek çok geçtir.
+//  /health işaretsizdir, anahtar istemez.
+app.UseExceptionHandler();
+app.UseRouting();
+app.UseMiddleware<ApiKeyMiddleware>();
```

Doğru sıra: hata yakalayıcı → routing → anahtar kontrolü → uç noktalar.

## Neden böyle düzeltiyoruz

- **Hata yakalayıcı en başta olmalı.** `UseExceptionHandler()` kendisinden sonra eklenen her şeyi sarar. Yakalayıcı anahtar kontrolünün gerisinde kalırsa, anahtar kontrolünün fırlattığı hata (ör. `IProblemDetailsService` çözülemez, yapılandırma okunamaz) yakalanmaz: kullanıcı düzgün bir `ProblemDetails` yerine ham bir 500 görür, geliştirme ortamında ise ayrıntılı hata sayfası açılır. En dışa yakın katman her zaman hatayı yakalayan olmalı.
- **Anahtar kontrolü routing'den sonra, uç noktalardan önce olmalı.** Routing olmadan hangi ucun korunduğu bilinmez; uç noktadan sonra reddetmek ise ucun kodunu çalıştırmış olur.
- **Kontrolü routing'den önceye alıp her şeye anahtar istemek** (`/health` dahil) izleme araçlarını ve orkestratör sağlık kontrollerini bozar.
- **Metadata kontrolünü kaldırıp yola göre (`/api/...`) karar vermek** kırılgandır; yeni bir uç eklendiğinde unutulur. İşaret (metadata) ucun yanında durduğu için daha güvenlidir.

## Bir daha olmaması için

- **Test:** Pipeline'ı gerçekten çalıştıran bir entegrasyon testi (`WebApplicationFactory<Program>`): anahtarsız `GET /api/notifications` → 401, yanlış anahtarla → 401, doğru anahtarla → 200, `GET /health` → 200. Mevcut `ApiKeyValidatorTests` yalnızca doğrulayıcıyı sınıyor, sırayı sınamıyor; bu hata onların hepsi yeşilken ortaya çıkar.
- **Kapalı varsayılan (fail closed):** Anahtar gerektirmeyen uçları işaretlemek (`AllowAnonymous` benzeri) ve geri kalan her şeyi korumak, "işaretli olmayan açıktır" mantığından daha güvenlidir. Uzun vadede ASP.NET Core'un kimlik doğrulama/yetkilendirme (`RequireAuthorization`) altyapısına geçmek de iyi bir seçenek.
- **Kod inceleme maddesi:** Güvenlik middleware'inin sırasını değiştiren her PR'da "bu middleware hangi bilgiye bağımlı ve o bilgi bu noktada hazır mı?" sorusunu sorun. Açıklayıcı yorumu silen değişiklik şüpheli sayılsın.
- **Tarama ve izleme:** Güvenlik taraması bu tür anahtarsız erişimi yakaladı; aynı denemeyi CI'a ekleyin. Canlıya çıktıysa: erişim loglarında anahtarsız `/api/notifications` isteklerini arayın, etkilenen verinin kapsamını belirleyin.

## İlgili

HATALAR.md · **NET-02 Middleware sırası (kimlik doğrulama)**

## English summary

**Root cause:** in the Notification service's `Program.cs`, `ApiKeyMiddleware` was added before `UseRouting()`. The middleware reads `RequireApiKeyMetadata` from `context.GetEndpoint()`, which is only set after routing. Before routing it is `null`, so `required` is `false` and the check is skipped: protected endpoints (customer emails) open without a key. The developer (commit "Bildirim servisi: anahtar kontrolü pipeline'ın başına alındı") thought security checks should run first, and also deleted the comment explaining why the order matters.

**How to find it:** `curl` without a key returns 200 -> breakpoint in `ApiKeyMiddleware.InvokeAsync` shows `GetEndpoint()` is `null` -> `Program.cs` order and `git log -p`.

**Fix:** `UseExceptionHandler()` -> `UseRouting()` -> `UseMiddleware<ApiKeyMiddleware>()` -> endpoints. The exception handler must be first so it wraps everything after it, including the key check.

**Prevent:** an integration test through the real pipeline (401 without key, 401 wrong key, 200 right key, 200 `/health`), fail-closed design, a review question about middleware dependencies, and check access logs if it ever shipped.
