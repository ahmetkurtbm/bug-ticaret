# İpuçları · Bölüm 28

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Ekrandaki mesaj kullanıcıya yöneliktir; nedeni söylemeyebilir. Asıl nedeni API'nin logu söyler. Sepette kuponu reddettirdikten hemen sonra `docker compose logs api` çıktısının sonuna bakın ve "Kupon" geçen satırları arayın. Satırın altındaki istisna (exception) türünü ve mesajını okuyun: sizce müşteriye gösterilen mesajla aynı mı?

*EN:* The message on screen is meant for users and may not say the reason; the API log does. Right after getting a coupon rejected in the cart, look at the end of `docker compose logs api` and search for lines mentioning "Kupon" (coupon). Read the type and message of the exception below the line: is it the same as the message shown to the customer?

## İpucu 2

F12 → Network sekmesinde kuponu uygulayan isteğe bakın (sepete kupon ekleyen `POST`). Yanıt gövdesindeki hata kodu (`code`) ve mesaj ne? Logda yazan hata koduyla karşılaştırın: sunucu içeride kuponun neden reddedildiğini biliyor, ama istemciye bambaşka bir şey söylüyor. Kupon kontrolünü yapan servis (`CouponValidator`) ile onu çağıran `CartService.ApplyCouponAsync` arasında bir şey olmalı.

*EN:* In F12 → Network, look at the request that applies the coupon (the `POST` that adds a coupon to the cart). What are the error `code` and the message in the response body? Compare them with the error code in the log: the server knows internally why the coupon was rejected but tells the client something entirely different. There must be something between the service that checks the coupon (`CouponValidator`) and `CartService.ApplyCouponAsync`, which calls it.

## İpucu 3

`backend/src/Tezgah.Application/Carts/CartService.cs` içindeki `ApplyCouponAsync`, `coupons.ValidateAsync(...)` çağrısını `try/catch (Exception)` içine almış. Asıl hata (`coupon_not_found`, `coupon_expired`, ...) loga yazılıyor, sonra yerine sabit koda ve sabit mesaja sahip yeni bir `DomainException("coupon_rejected", "Kupon uygulanamadı.")` fırlatılıyor. Özgün hata bilgisi bu yüzden istemciye hiç ulaşmıyor. Önceki hâli için `git log -p -- backend/src/Tezgah.Application/Carts/CartService.cs`.

*EN:* `ApplyCouponAsync` in `backend/src/Tezgah.Application/Carts/CartService.cs` wraps the `coupons.ValidateAsync(...)` call in `try/catch (Exception)`. The original error (`coupon_not_found`, `coupon_expired`, ...) is written to the log, and then a new `DomainException("coupon_rejected", "Kupon uygulanamadı.")` with a fixed code and message is thrown in its place. That is why the original error information never reaches the client. For the previous version, see `git log -p -- backend/src/Tezgah.Application/Carts/CartService.cs`.
