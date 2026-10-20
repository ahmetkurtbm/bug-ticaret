# Çözüm · Bölüm 10 (API-02) · JSON alan adı uyuşmazlığı

## Kök neden

`frontend/apps/storefront/src/api/cart.ts` içinde `applyCoupon` gövdeyi `{ code }` olarak gönderiyor:

```ts
applyCoupon: (code: string) => api<Cart>('/api/cart/coupon', { method: 'PUT', body: { code } }),
```

Backend ise şu sınıfı bekliyor (`backend/src/Tezgah.Application/Carts/CartDtos.cs`):

```csharp
public class ApplyCouponRequest
{
    [Required, MaxLength(40)]
    public string CouponCode { get; set; } = default!;
}
```

ASP.NET Core model bağlama JSON alan adlarını özellik adlarıyla eşler (büyük/küçük harfe duyarsız: `couponCode` ↔ `CouponCode`). `code` adında bir özellik yok; alan yok sayılır. `CouponCode` `null` kalır, `[Required]` başarısız olur ve `[ApiController]` otomatik olarak `400` ve `ValidationProblemDetails` döner. Başlığı İngilizce olan varsayılan metindir: `One or more validation errors occurred.` Alan hataları `errors.CouponCode` altındadır. Müşteri sitesi hata metnini (`ApiError.message`) olduğu gibi kutunun altına yazar.

Değişikliği yapan geliştirici şunu düşünmüş (commit `8497842`):

> Sepet API istemcisinde parametre adları kısaltıldı

Fonksiyon parametresini `couponCode`'dan `code`'a kısaltırken, kısayol sözdizimi `{ couponCode }` kullanıldığı için istek gövdesindeki alan adı da otomatik olarak değişti. Önceki kod `body: { couponCode }` idi (özellik adı değişkenden gelir). Yeniden adlandırma bir yerel değişkeni değiştirir gibi göründü, ama gerçekte ağ sözleşmesini değiştirdi.

## Nasıl bulunur

1. **Belirti:** Kutunun altında "One or more validation errors occurred." Bu, ASP.NET Core'un otomatik doğrulama yanıtının varsayılan başlığıdır; yani istek sunucuya ulaşıyor, model doğrulaması başarısız.
2. **Araç: Network sekmesi.** `PUT /api/cart/coupon` → durum `400`. *Payload* sekmesi: `{"code":"KARGOBEDAVA"}`. *Response* sekmesi:

   ```json
   {
     "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
     "title": "One or more validation errors occurred.",
     "status": 400,
     "errors": { "CouponCode": ["The CouponCode field is required."] }
   }
   ```

   Gönderilen `code`, istenen `CouponCode`. Fark gözle görülür.
3. **Backend tarafı:** `CartController.ApplyCoupon(ApplyCouponRequest request, ...)` metoduna kesme noktası koyup isteği gönderirseniz `request.CouponCode == null` görürsünüz. Sunucu "alan boş" diyor; alanı gönderen kodda eksik.
4. **Dosya:** `frontend/apps/storefront/src/api/cart.ts`. `git log -p -- frontend/apps/storefront/src/api/cart.ts` parametre adı değişikliğini gösterir.

**Visual Studio:** `CartController.ApplyCoupon` içine kesme noktası, F5 ile API'yi başlatın; *Locals*'ta `request.CouponCode` `null`.

**VS Code:** "API (monolit)" yapılandırmasıyla aynı kesme noktası. İstemci tarafı için "Müşteri sitesi (Chrome)" ile `cart.ts`'e kesme noktası koyup `body` nesnesine bakabilirsiniz.

## Düzeltme

```diff
-  applyCoupon: (code: string) => api<Cart>('/api/cart/coupon', { method: 'PUT', body: { code } }),
+  applyCoupon: (couponCode: string) => api<Cart>('/api/cart/coupon', { method: 'PUT', body: { couponCode } }),
```

Parametre adı kısa kalabilir; önemli olan gövdedeki anahtar:

```ts
applyCoupon: (code: string) => api<Cart>('/api/cart/coupon', { method: 'PUT', body: { couponCode: code } }),
```

İkisi de doğru. İkincisi, değişken adı ile ağ alanı adının birbirinden bağımsız olduğunu açıkça gösterir.

## Neden böyle düzeltiyoruz

- **Backend'i `code` kabul edecek şekilde değiştirmek** sözleşmeyi bozar: başka istemciler, testler ve belgeler `couponCode` kullanıyor. Doğru olan tarafı (backend) değiştirmek yanlış.
- **Backend'e `[JsonPropertyName("code")]` ya da iki isim kabulü eklemek** gereksiz belirsizlik yaratır; hangi ismin doğru olduğu bilinmez hâle gelir.
- **Sunucuda bilinmeyen alanları reddetmek** bu hatayı daha erken ve anlaşılır yakalatır: `{ code }` gönderildiğinde "bilinmeyen alan: code" mesajı alınırdı. `[ApiController]` varsayılanında bu yok; istenirse özel bir model bağlayıcı ya da `System.Text.Json`'da `UnmappedMemberHandling = Disallow` ile eklenebilir.

## Bir daha olmaması için

- **Test:** Bu bölümün testi gibi, istemci fonksiyonunun gönderdiği gövdeyi sınayan bir test (`{ couponCode: 'KARGOBEDAVA' }`). Sözleşme alanı adı değişirse test kırılır.
- **Tek kaynaktan tür üretimi:** Backend zaten OpenAPI üretiyor (`MapOpenApi`). İstemci tür ve fonksiyonlarını bu şemadan üretirseniz elle yazılan alan adı hatası derleme zamanında yakalanır.
- **Kod inceleme maddesi:** İstek gövdesi nesnesindeki bir anahtarın değişmesi, API sözleşmesinin değişmesidir; PR açıklamasında belirtilsin ve backend sınıfıyla karşılaştırılsın. "Yalnızca yeniden adlandırma" PR'ları gövde nesnelerine dokunuyorsa dikkatle okunmalı. Kısayol sözdizimi (`{ code }`) yeniden adlandırmada alan adını sessizce değiştirir.
- **Hata mesajı:** Kullanıcıya "One or more validation errors occurred." göstermek yerine, `errors` alanındaki ilk mesajı ya da genel Türkçe bir mesajı gösteren ortak bir çevirici kullanın; teknik İngilizce metnin kullanıcıya ulaşması bir izleme sinyalidir.

## İlgili

HATALAR.md · **API-02 400: JSON alan adı uyuşmazlığı (camelCase / PascalCase)** (katalogdaki örnekte alanlar sessizce yok sayılıp kayıt boş kalıyor; burada alan `[Required]` olduğu için sunucu 400 ile bildiriyor. Kök neden aynı: istemci ile sunucu alan adı farkı.)

## English summary

**Root cause:** the storefront's `applyCoupon` sends `{ code }`, while the backend `ApplyCouponRequest` requires `CouponCode` (`[Required]`). Model binding ignores the unknown `code`, `CouponCode` stays `null`, and `[ApiController]` returns 400 "One or more validation errors occurred." The developer (commit "Sepet API istemcisinde parametre adları kısaltıldı") renamed the function parameter from `couponCode` to `code`; because the body used the shorthand `{ couponCode }`, the JSON field name changed with it.

**How to find it:** the English default title means model validation failed on the server -> Network: Payload `{"code":"..."}` vs Response `errors.CouponCode` required -> `CartController.ApplyCoupon` breakpoint shows `CouponCode == null` -> `cart.ts` and `git log -p`.

**Fix:** send `couponCode` in the body: `body: { couponCode }` (or `{ couponCode: code }`).

**Prevent:** a client test asserting the request body, generate client types from the OpenAPI schema, treat body key changes as contract changes in review, and consider rejecting unknown fields on the server.
