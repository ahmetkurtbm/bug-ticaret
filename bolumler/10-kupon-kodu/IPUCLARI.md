# İpuçları · Bölüm 10 (API-02)

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Network sekmesinde kupon isteğini (`coupon`, yöntem PUT) seçin. *Payload* sekmesinde tarayıcının gönderdiği JSON'u, *Response* sekmesinde sunucunun cevabını okuyun. Cevaptaki `errors` nesnesinde hangi alan adı geçiyor, gönderdiğiniz JSON'da hangi alan adı var? İkisini yan yana yazın.

*EN:* In the Network tab, select the coupon request (`coupon`, method PUT). Read the JSON the browser sent in the *Payload* tab and the server's answer in the *Response* tab. Which field name appears in the `errors` object of the answer, and which field name is in the JSON you sent? Write the two side by side.

## İpucu 2

Sunucu istek gövdesini bir C# sınıfına bağlar (model bağlama, model binding) ve `[Required]` gibi kurallarla doğrular. JSON'daki alan adı sınıftaki özellik adıyla eşleşmezse (büyük/küçük harf fark etmez, ama harfler aynı olmalı) alan boş kalır ve doğrulama başarısız olur. Kupon ucunun hangi sınıfı beklediğini backend'de (`CartController`, `Carts` klasöründeki DTO'lar) bulun ve istemcinin gönderdiği alanla karşılaştırın.

*EN:* The server binds the request body to a C# class (model binding) and validates it with rules such as `[Required]`. If a field name in the JSON does not match the property name in the class (case does not matter, but the letters must be the same), the property stays empty and validation fails. Find which class the coupon endpoint expects in the backend (`CartController`, the DTOs in the `Carts` folder) and compare it with the field the client sends.

## İpucu 3

Backend'de `ApplyCouponRequest` sınıfı `[Required, MaxLength(40)] public string CouponCode` bekliyor. `frontend/apps/storefront/src/api/cart.ts` içindeki `applyCoupon` ise gövdeyi `{ code }` olarak gönderiyor. Alan adı uyuşmadığı için `CouponCode` boş kalıyor ve `[Required]` 400 üretiyor. Hata istemcide; backend sözleşmesi doğru. Önceki hâli için `git log -p -- frontend/apps/storefront/src/api/cart.ts`.

*EN:* In the backend, the `ApplyCouponRequest` class expects `[Required, MaxLength(40)] public string CouponCode`. `applyCoupon` in `frontend/apps/storefront/src/api/cart.ts` sends the body as `{ code }`. Because the field name does not match, `CouponCode` stays empty and `[Required]` produces the 400. The fault is in the client; the backend contract is correct. For the previous version, see `git log -p -- frontend/apps/storefront/src/api/cart.ts`.
