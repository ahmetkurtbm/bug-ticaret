# Çözüm · Bölüm 6 (NET-01) · CORS izin listesinde sondaki eğik çizgi

## Kök neden

`backend/src/Tezgah.Api/appsettings.Development.json` içindeki izin verilen adres listesinde satıcı paneli şöyle yazılmış:

```json
"AllowedOrigins": [ "http://localhost:5173", "http://localhost:5174/", "http://localhost:5175" ]
```

`http://localhost:5174/` sonunda eğik çizgi var. Tarayıcı, farklı kaynağa (origin) yaptığı istekte `Origin: http://localhost:5174` başlığı gönderir: şema, sunucu adı ve port; yol ve eğik çizgi yok. ASP.NET Core CORS ara katmanı bu başlığı listedeki değerlerle karşılaştırır. `http://localhost:5174` ile `http://localhost:5174/` eşleşmez. Eşleşme yoksa ara katman hata atmaz, sadece `Access-Control-Allow-Origin` başlığını eklemez. Tarayıcı bu eksiği görünce yanıtı sayfaya vermez ve konsola CORS hatası yazar.

`api/auth/login` gibi istekler JSON gövdeli ve çoğu `Authorization` başlıklı olduğu için tarayıcı önce bir `OPTIONS` ön kontrolü (preflight) yapar. Ön kontrol reddedilince asıl istek hiç gönderilmez. Bu yüzden giriş de yapılamaz.

Değişikliği yapan geliştirici commit mesajına yalnızca şunu yazmış (commit `9dcbfd4`):

> Geliştirme ayarları düzenlendi

Mesaj niyeti açıklamıyor; bu başlı başına bir sorun. Diff yalnızca bu eğik çizgiyi ekliyor. Büyük olasılıkla ayar dosyasını elle düzenlerken ya da adresleri URL gibi "tam" yazmak isterken kazara girdi. Yani kimse bunu bilerek yapmadı ve kimse etkisini denemedi.

## Nasıl bulunur

1. **Belirti:** Satıcı panelinde veri yok, konsolda CORS hatası. Müşteri sitesi (5173) ve yönetim paneli (5175) çalışıyor. Bu fark çok değerli: API'nin genel olarak çalıştığını, sorunun yalnızca bir adrese özgü olduğunu söyler.
2. **Araç: Network sekmesi.** Başarısız isteğin önünde `OPTIONS` isteği var. Yanıt başlıklarında `Access-Control-Allow-Origin` yok. 5173'ten yapılan istekte aynı başlık var.
3. **Araç: curl.** `curl -i -X OPTIONS http://localhost:5080/api/seller/dashboard -H "Origin: http://localhost:5174" -H "Access-Control-Request-Method: GET"` çıktısında `Access-Control-Allow-Origin` yok; Origin'i 5173 yapınca var. `Origin` başlığı olmadan yapılan normal `curl` isteği çalışır, çünkü CORS'u uygulayan sunucu değil tarayıcıdır.
4. **Gözlem:** API terminalinde hata yok. CORS reddi sunucu için hata değildir, bu yüzden log yazılmaz.
5. **Dosya:** `Program.cs` içinde `UseCors("web")` doğru yerde (`UseRouting`'den sonra, `UseAuthentication`'dan önce); politika `Cors:AllowedOrigins` ayarından kuruluyor. Ayar `appsettings.Development.json` içinde: üçüncü satırdaki `5174/`.
6. **Doğrulama:** `git diff main -- backend/src/Tezgah.Api/appsettings.Development.json` tek farkı gösterir.

**Visual Studio:** Çıkış (Output) penceresinde hata aramak yetmez. Hata ayıklarken `Program.cs` içinde `var allowedOrigins = ...` satırından sonra kesme noktası koyup `allowedOrigins` dizisinin içeriğine bakmak sorunu bir saniyede gösterir.

**VS Code:** Aynı kesme noktası "API (monolit)" yapılandırmasıyla. *Variables* panelinde `allowedOrigins` listesini açın.

## Düzeltme

```diff
-    "AllowedOrigins": [ "http://localhost:5173", "http://localhost:5174/", "http://localhost:5175" ]
+    "AllowedOrigins": [ "http://localhost:5173", "http://localhost:5174", "http://localhost:5175" ]
```

Sonra API'yi yeniden başlatın. CORS politikası uygulama açılırken kurulur; yapılandırma dosyasını kaydetmek yeni politikayı devreye sokmaz.

## Neden böyle düzeltiyoruz

- **`AllowAnyOrigin()` ya da `SetIsOriginAllowed(_ => true)`** sorunu siler ama her siteye izin verir. API `AllowCredentials()` kullandığı için `AllowAnyOrigin` zaten birlikte kullanılamaz; `SetIsOriginAllowed(_ => true)` ise güvenlik duvarını kaldırır. Geliştirmede alışkanlık olursa üretime taşınır.
- **Tarayıcıda `mode: 'no-cors'`** çözüm değildir; yanıtı okunamaz hâle getirir.
- **Kodda `TrimEnd('/')` ile temizlemek** olabilir ama yanlış yazılan ayarı sessizce düzeltmek yerine hatalı ayarın açılışta reddedilmesi daha iyidir (aşağıda).

## Bir daha olmaması için

- **Test:** Bu bölümün kontrolü gibi, üç uygulama adresi için preflight sınayan bir entegrasyon testi (`WebApplicationFactory`) CI'da çalışsın.
- **Açılışta doğrulama:** `Cors:AllowedOrigins` içindeki her değer `Uri` olarak çözülsün; `AbsolutePath` `/`'dan farklıysa ya da değer `/` ile bitiyorsa uygulama açılışta anlaşılır bir hatayla durdurulsun (`Payments:BaseUrl` için zaten benzer doğrulama var).
- **Kod inceleme maddesi:** Ayar dosyası değişikliklerini de kod gibi okuyun. "Düzenlendi" gibi içeriksiz commit mesajlarını reddedin; ayar değişikliğinin amacı mesajda yazsın.
- **Hata ayıklama alışkanlığı:** CORS hatasında önce sunucu loguna değil, preflight yanıtının başlıklarına bakın.

## İlgili

HATALAR.md · **NET-01 CORS politikası çalışmıyor**

## English summary

**Root cause:** `appsettings.Development.json` listed the seller panel as `http://localhost:5174/` (trailing slash). Browsers send `Origin: http://localhost:5174` with no slash, so the CORS middleware finds no match and simply omits `Access-Control-Allow-Origin`. The browser then blocks the preflight (`OPTIONS`) response, so even login never gets sent. Nothing is logged on the server because a CORS refusal is not a server error. The commit message "Geliştirme ayarları düzenlendi" gives no reason for the change.

**How to find it:** console CORS error -> Network `OPTIONS` response has no `Access-Control-Allow-Origin` -> `curl -X OPTIONS` with `Origin: 5174` vs `5173` -> only 5174 fails -> `Cors:AllowedOrigins` in `appsettings.Development.json`.

**Fix:** remove the trailing slash and restart the API (the policy is built at startup).

**Prevent:** a preflight integration test for all three origins, startup validation that rejects origins with a path or trailing slash, and treat config changes as code in review.
