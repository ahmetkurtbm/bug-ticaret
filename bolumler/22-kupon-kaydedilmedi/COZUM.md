> **Cevap anahtarı.** Bölümü kendiniz çözmeden okumayın; önce `ipucu` komutunu deneyin.

# Çözüm · Bölüm 22 (API-11) · Hata olduğu hâlde 200 dönmek

## Kök neden

`backend/src/Tezgah.Api/Controllers/AdminController.cs` içindeki `CreateCoupon` eylemine bir `try/catch` eklenmiş:

```csharp
[HttpPost("coupons")]
public async Task<ActionResult<AdminCouponDto>> CreateCoupon(CreateCouponRequest request, CancellationToken ct)
{
    try
    {
        return Ok(await admin.CreateCouponAsync(request, ct));
    }
    catch (RequestValidationException ex)
    {
        return Ok(new { error = ex.Message });
    }
}
```

Aynı kodla ikinci kupon oluşturmaya çalışınca servis (`AdminService.CreateCouponAsync`) `RequestValidationException.For("Code", "Bu kod zaten kullanılıyor.")` fırlatır. Normalde bu istisna merkezi hata işleyiciye (`DomainExceptionHandler`) ulaşır; o da `400 Bad Request` ve `ValidationProblemDetails` (`errors.Code`) üretir. Müşteri/yönetim arayüzü bu `errors` alanını formda ilgili alanın altına yazar.

Ama eylem istisnayı kendisi yakalayıp **`200 OK`** ile `{ "error": "..." }` gövdesi döndürüyor. Yönetim paneli (`useMutation`) bir isteğin başarılı olup olmadığına **durum koduna** bakar: `2xx` gelince `onSuccess` çalışır, liste tazelenir, form kapanır. Gövdedeki `error` alanını kimse okumaz. Kullanıcı "kaydedildi" izlenimi alır; hiçbir kupon yoktur.

Değişikliği yapan geliştirici şunu düşünmüş (commit `abc1234`):

> Kupon oluşturmada hata yanıtı tek alandan okunsun
> Panel `error` alanını okusun, durum koduna ve `errors` nesnesine bakmasına gerek kalmasın.

Niyet "istemci işini kolaylaştırmak". Ama **HTTP durum kodu bir sözleşmedir**: araçlar, proxy'ler, izleme, otomatik yeniden deneme ve her HTTP istemcisi `2xx`'in başarı demek olduğunu varsayar. Hatayı `200` ile taşımak bu sözleşmeyi bozar ve başarısızlığı herkesin gözünden gizler.

## Nasıl bulunur

1. **Belirti:** Form uyarı vermeden kapanıyor, liste değişmiyor. Bu, istemcinin işlemi başarılı saydığını gösterir.
2. **Araç: Network sekmesi.** `POST /api/admin/coupons` isteğinin durum kodu `200`. *Response* sekmesi: `{"error":"Bu kod zaten kullanılıyor."}`. Durum kodu "başarılı" derken gövde "başarısız" diyor: çelişki hatanın tam kendisidir.
3. **Sunucu günlüğü:** Merkezi işleyici hiç çalışmadığı için doğrulama hatası günlükte ve izlemede görünmez; `4xx` oranı da artmaz.
4. **Karşılaştırın:** Diğer eylemleri (`Activate`, `Deactivate`) ve başka bir doğrulama hatasını (ör. bitiş günü başlangıçtan önce) tetikleyin: onlar `400` ile `errors.<alan>` döndürür. `CreateCoupon` farklı davranıyor.
5. **Geçmiş:** `git log -p -- backend/src/Tezgah.Api/Controllers/AdminController.cs` eklenen `try/catch`'i gösterir.

**Visual Studio / VS Code:** `catch` bloğuna kesme noktası koyup aynı koda ikinci kez kupon oluşturun; blok çalışır ve istisna merkezi işleyiciye hiç yükselmez.

## Düzeltme

`try/catch`'i kaldırın; istisna merkezi işleyiciye yükselsin:

```diff
     [HttpPost("coupons")]
-    public async Task<ActionResult<AdminCouponDto>> CreateCoupon(CreateCouponRequest request, CancellationToken ct)
-    {
-        try
-        {
-            return Ok(await admin.CreateCouponAsync(request, ct));
-        }
-        catch (RequestValidationException ex)
-        {
-            return Ok(new { error = ex.Message });
-        }
-    }
+    public async Task<ActionResult<AdminCouponDto>> CreateCoupon(CreateCouponRequest request, CancellationToken ct) => Ok(await admin.CreateCouponAsync(request, ct));
```

Artık yanıt `400` ve `{"title": "...", "errors": {"Code": ["Bu kod zaten kullanılıyor."]}}` olur; yönetim paneli `ApiError.fieldErrors` ile uyarıyı kod alanının altında gösterir, form açık kalır. İstemcide değişiklik gerekmez.

## Neden böyle düzeltiyoruz

- **Paneli `error` alanını okuyacak şekilde değiştirmek.** Belirtiyi panelde onarır, ama API hâlâ yalan söyler: başka istemciler (betikler, testler, gelecekteki uygulamalar) aynı tuzağa düşer; izleme araçları hatayı göremez. Sözleşmeyi bozan taraf (API) düzeltilmelidir.
- **Hatayı `4xx` yapıp `try/catch`'i tutmak** (`return BadRequest(...)`). Durum kodu doğru olur ama aynı hata dönüştürme kodu her eyleme kopyalanır, biçim eylemden eyleme farklılaşır. Proje merkezi bir işleyici (`DomainExceptionHandler`) ile bunu tek yerde yapmak üzere kurulu.
- **Yanıtta hem `200` hem de `error` taşımak "geriye uyumluluk" için.** Hata durumunda başarı kodu geriye uyumluluk değildir, hatadır.

## Bir daha olmaması için

- **Test:** Aynı kodla ikinci kupon oluşturan bir API testi: `400` ve `errors.Code` beklenir (`ValidationProblemDetails`). Genel kural olarak "geçersiz istek → `4xx`" testleri her yazma ucunda bulunsun.
- **Kod inceleme maddesi:** Denetleyicide `catch` gören yoruma şu soruyu sorun: "Bu istisna merkezi işleyicide zaten işleniyor mu?" Bir eylemin hata biçimini diğerlerinden farklılaştıran değişiklik şüphelidir. `catch` bloğundan `Ok(...)` dönen kod neredeyse her zaman hatadır.
- **İstemci savunması:** `fetch` sarmalayıcısında yalnızca durum koduna güvenmek doğru, ama kritik işlemlerde yanıtın beklenen şekle uyduğunu (ör. `id` alanı var mı) doğrulamak ek bir güvenlik ağıdır.
- **İzleme:** `4xx`/`5xx` oranı ve doğrulama hatası sayaçları. Hatalar `200` ile saklanırsa bu göstergeler sessiz kalır; "başarılı" oranın hiç hata içermemesi şüphe uyandırmalıdır.

## İlgili

HATALAR.md · **API-11 Hata olduğu hâlde 200 dönmek**

## English summary

**Root cause:** `AdminController.CreateCoupon` was wrapped in a `try/catch` that catches `RequestValidationException` and returns `Ok(new { error = ... })`. A duplicate coupon code makes the service throw a validation exception; normally the central handler (`DomainExceptionHandler`) turns it into a `400` with `ValidationProblemDetails`. Here the response is `200`, so the admin panel's mutation runs `onSuccess`, closes the form and refreshes the list, which has no new coupon. The developer (commit "Kupon oluşturmada hata yanıtı tek alandan okunsun") wanted the panel to read errors from one field, but the HTTP status code is a contract: errors must not travel with a 2xx.

**How to find it:** Network shows `200` with `{"error": ...}` in the body, a contradiction; the central handler never ran and no 4xx is logged; compare with other actions; `git log -p`.

**Fix:** remove the `try/catch` so the exception reaches the central handler; the response becomes `400` with `errors.Code` and the panel shows it under the code field.

**Prevent:** API tests for invalid requests (expect 4xx), review of `catch` blocks that return success, and monitoring of 4xx rates.
