# İpuçları · Bölüm 5 (API-03)

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Askıdaki hesapla panele girin ve Network sekmesinde `seller/me` ya da `seller/dashboard` isteğine bakın. Üç şeyi okuyun: durum kodu, yanıt gövdesindeki `title` ve `code`, ve bu isteğin hemen arkasından gelen `/api/auth/refresh` isteğinin durum kodu. İstek token yenilendikten sonra tekrar edilmiş ama yine aynı sonucu almış mı?

*EN:* Log in with the suspended account and look at the `seller/me` or `seller/dashboard` request in the Network tab. Read three things: the status code, the `title` and `code` in the response body, and the status code of the `/api/auth/refresh` request that follows it. Was the request repeated after the token was renewed and did it get the same result again?

## İpucu 2

Yenileme isteği başarılı (200) ama asıl istek hâlâ reddediliyor. Demek ki token geçersiz değil; sunucu kimliği tanıyor ama işleme izin vermiyor. Bu iki durum için HTTP'de iki ayrı durum kodu vardır. Sunucunun hangisini döndürmesi gerektiğini ve istemcinin (`frontend/apps/seller/src/api/client.ts`) hangi kodda oturum yenilemeyi denediğini karşılaştırın. Hangi katmanın durum kodunu belirlediğini bulmak için backend'de "askıya alındı" mesajının nereden fırlatıldığına bakın.

*EN:* The refresh request succeeds (200), yet the original request is still refused. So the token is not invalid: the server recognizes the identity but does not allow the operation. HTTP has two separate status codes for these two situations. Compare which one the server should return with the status code on which the client (`frontend/apps/seller/src/api/client.ts`) tries to renew the session. To find which layer decides the status code, look in the backend at where the "suspended" message is thrown.

## İpucu 3

`SellerContext` askıdaki mağaza için `ForbiddenException` fırlatıyor. Bu istisnayı HTTP yanıtına `backend/src/Tezgah.Api/Infrastructure/DomainExceptionHandler.cs` çeviriyor. Dosyadaki `ForbiddenException` satırı `Status401Unauthorized` ve `"unauthorized"` döndürüyor; adı "forbidden" olan bir istisna için bu yanlış. `git log -p -- backend/src/Tezgah.Api/Infrastructure/DomainExceptionHandler.cs` değişikliği gösterir.

*EN:* `SellerContext` throws `ForbiddenException` for a suspended store. `backend/src/Tezgah.Api/Infrastructure/DomainExceptionHandler.cs` translates that exception into an HTTP response. The `ForbiddenException` line in that file returns `Status401Unauthorized` and `"unauthorized"`, which is wrong for an exception named "forbidden". `git log -p -- backend/src/Tezgah.Api/Infrastructure/DomainExceptionHandler.cs` shows the change.
