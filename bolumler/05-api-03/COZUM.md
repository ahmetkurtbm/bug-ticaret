# Çözüm · Bölüm 5 (API-03) · 401 ve 403 karışıklığı

## Kök neden

`backend/src/Tezgah.Api/Infrastructure/DomainExceptionHandler.cs` içinde `ForbiddenException` artık `401 Unauthorized` olarak dönüyor:

```csharp
ForbiddenException e => (StatusCodes.Status401Unauthorized, e.Message, "unauthorized"),
```

Askıdaki mağazanın sahibi için `SellerContext` "Mağazanız aktif değil..." diyerek `ForbiddenException` fırlatır. Kullanıcı kim olduğunu zaten kanıtlamıştır (token geçerli); yetkisi yoktur. Bu `403 Forbidden` durumudur. `401 Unauthorized` ise "seni tanıyamadım, kimliğini göster" demektir.

İstemci (`api/client.ts`) bu ayrıma güvenir: `401` alırsa token'ı yenileyip isteği bir kez tekrarlar, yenileme başarısız olursa oturumu siler ve girişe yönlendirir. `403`'te ise yenilemez; sebebi gösterir. Bu yüzden:

1. Her başarısız istek için gereksiz bir `/api/auth/refresh` çağrısı yapılır. Her yenileme eski yenileme token'ını iptal edip veritabanında yeni bir kayıt açar.
2. Yenilenen token'la aynı istek yine reddedilir, çünkü sorun token değildir.
3. `SellerLayout` içindeki `status === 403` dalı hiç çalışmaz. Askıdaki satıcının görmesi gereken tam sayfa açıklama ve "Çıkış yap" düğmesi çıkmaz; kullanıcı yarım çalışan bir panel görür.
4. Yenileme bir nedenle başarısız olursa (ör. yenileme token'ı süresi dolmuş), istemci oturumu siler ve kullanıcıyı girişe atar. Giriş yaptığında aynı döngü yeniden başlar.

Değişikliği yapan geliştirici şunu düşünmüş (commit `5111552`):

> Yetki hataları tek tip yanıtla dönüyor
> İstemci tarafında yetki hatalarını tek yerde ele almak için.

"Yetki hatası" kelimesini tek bir kod altında toplamak istemiş. Ama istemci tarafında iki farklı durumun iki farklı davranışı var; tek tip yanıt, istemcinin işini kolaylaştırmak yerine yanlış davranışı tetikledi.

## Nasıl bulunur

1. **Belirti:** Askıdaki hesapla panel yarım açılıyor, her bölümde hata kutusu, tam sayfa açıklama yok.
2. **Araç: Network sekmesi.** `seller/me` isteği `401`; gövdede `"title": "Mağazanız aktif değil..."`, `"code": "unauthorized"`. Hemen ardından `POST /api/auth/refresh` `200`, sonra aynı `GET` yine `401`.
3. **Gözlem:** Yenileme başarılı ama istek yine reddediliyor, yani token sorunu değil. Gerçek bir kimlik doğrulama hatasında JWT ara katmanı `WWW-Authenticate: Bearer` başlığı ekler; bu yanıtta yok, yani 401'i uygulamanın kendi kodu üretmiş. `jwt.io` ya da `atob` ile access token'ı çözüp `exp` değerinin gelecekte olduğunu görmek de aynı sonuca götürür.
4. **Araç: arama.** Yanıttaki "Mağazanız aktif değil" metnini depoda arayın: `SellerContext.cs` içinde `ForbiddenException` ile fırlatılıyor.
5. **Dosya:** `ForbiddenException`'ı yanıta çeviren yer: `backend/src/Tezgah.Api/Infrastructure/DomainExceptionHandler.cs`. `git log -p` satırın `403`'ten `401`'e değiştiğini gösterir.
6. **Doğrulama:** `curl -i` ile askıdaki hesabın access token'ıyla `GET /api/seller/dashboard` çağırın: durum kodunu görün.

**Visual Studio:** `DomainExceptionHandler.TryHandleAsync` içine kesme noktası koyun, F5 ile üç servisi başlatıp isteği gönderin; `exception` değişkeninin türü `ForbiddenException`, `status` değeri 401 görünür.

**VS Code:** "API (monolit)" yapılandırmasıyla aynı kesme noktası. İsteği `Tezgah.Api.http` dosyasından gönderebilirsiniz.

## Düzeltme

```diff
-            ForbiddenException e => (StatusCodes.Status401Unauthorized, e.Message, "unauthorized"),
+            ForbiddenException e => (StatusCodes.Status403Forbidden, e.Message, "forbidden"),
```

## Neden böyle düzeltiyoruz

- **İstemcide `401` + `code === "unauthorized"` ayrımı yapmak** yanlış kodu kalıcılaştırır. HTTP istemcileri, ara sunucular, izleme araçları ve ileride yazılacak diğer istemciler (mobil, üçüncü taraf) 401'i "kimlik doğrulama" olarak yorumlar. Sunucunun yanlış kodunu her istemcide ayrı ayrı telafi etmek gerekirdi.
- **İstemcide her hatada oturum silmemek** tek başına yetmez: gereksiz yenileme çağrıları ve eksik tam sayfa açıklama sürerdi. Kök neden sunucunun yanıt kodudur.
- **Tüm yetki hatalarını `400`'e çevirmek** doğrulama hatasıyla karışır ve anlamsızdır.
- **Doğru kod `403`.** Kimlik belli, yetki yok. Yanıt gövdesindeki `title` sebebi taşır ve istemci bunu kullanıcıya gösterir.

## Bir daha olmaması için

- **Test:** `DomainExceptionHandler` için istisna türü → durum kodu eşlemesini sınayan bir birim testi (`ForbiddenException` → 403, `NotFoundException` → 404, `ConflictException` → 409). Ek olarak "askıdaki satıcı `GET /api/seller/dashboard` çağırınca 403" diyen bir entegrasyon testi; bu bölümün kontrolü tam olarak bunu sınar.
- **İstemci politikası belgesi:** `401` → bir kez yenile, olmazsa girişe; `403` → sebebi göster, yenileme yok. Bu kural `docs/` altında yazılı olsun.
- **Kod inceleme maddesi:** Durum kodu eşlemesini değiştiren PR'da "bu kodu hangi istemciler nasıl yorumluyor?" sorusu sorulsun. "Tek tip yanıt" sebebi 401/403 ayrımını silmeyi haklı çıkarmaz.
- **İzleme:** Kısa sürede çok sayıda `/api/auth/refresh` isteği, yenileme token'ı kaydı sayısında ani artış ve 401 oranı için uyarı koyun. Bu hata yenileme kayıtlarını şişirirdi.

## İlgili

HATALAR.md · **API-03 401 ve 403 karışıklığı**

## English summary

**Root cause:** `DomainExceptionHandler.cs` mapped `ForbiddenException` to `401 Unauthorized`. A suspended seller is authenticated but not permitted, which is `403`. The client treats `401` as "token problem": it refreshes the token and retries, and clears the session if refresh fails. So every failed request triggers a useless refresh, the retry fails again, the `403` branch in `SellerLayout` (full-page explanation with a log-out button) never runs, and if refresh ever fails the user is thrown to the login page and the cycle restarts. The developer (commit "Yetki hataları tek tip yanıtla dönüyor") wanted one uniform authorization error for the client.

**How to find it:** Network tab shows `seller/me` -> 401 with the suspension text, then `/api/auth/refresh` -> 200, then the same request -> 401 again (so the token is fine) -> search the message text -> `SellerContext` throws `ForbiddenException` -> `DomainExceptionHandler.cs`.

**Fix:** `ForbiddenException` -> `StatusCodes.Status403Forbidden`, code `"forbidden"`.

**Prevent:** unit-test the exception-to-status mapping, integration-test the suspended seller (403), document the client policy (401 refresh once, 403 show the reason), and question status-code changes in review.
