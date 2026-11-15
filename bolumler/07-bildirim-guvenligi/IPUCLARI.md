# İpuçları · Bölüm 7 (NET-02)

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

`curl -i` ile üç isteği sırayla gönderin (anahtarsız, yanlış anahtarlı, doğru anahtarlı) ve durum kodlarını yan yana yazın. Sonra `Tezgah.Notifications/Api/ApiKeyMiddleware.cs` içindeki `InvokeAsync` metoduna kesme noktası koyup servisi hata ayıklayıcıyla başlatın ve anahtarsız isteği gönderin. Metot hiç çağrılıyor mu? Çağrılıyorsa `required` değişkeni ne oluyor?

*EN:* Send three requests with `curl -i` (no key, wrong key, right key) and write the status codes side by side. Then put a breakpoint in the `InvokeAsync` method of `Tezgah.Notifications/Api/ApiKeyMiddleware.cs`, start the service under the debugger and send the request without a key. Is the method called at all? If so, what does the `required` variable become?

## İpucu 2

ASP.NET Core'da istek, `Program.cs` içinde eklenme sırasıyla middleware'lerden geçer. Bu middleware hangi uca anahtar gerektiğini ucun üzerindeki bir işaretten (metadata) okuyor; bunun için isteğin hangi uca gideceğinin önceden belirlenmiş olması lazım. Hangi middleware'in isteği bir uca eşlediğini ve `context.GetEndpoint()` değerinin ondan önce ne olduğunu araştırın. Pipeline'daki sıraya bir de hata yakalayıcı açısından bakın.

*EN:* In ASP.NET Core a request passes through middleware in the order they are added in `Program.cs`. This middleware reads which endpoint needs a key from a marker (metadata) on the endpoint; for that, the endpoint the request will reach must already be decided. Find out which middleware matches a request to an endpoint and what `context.GetEndpoint()` returns before it. Also look at the order from the point of view of the exception handler.

## İpucu 3

`backend/src/Tezgah.Notifications/Program.cs` içinde `app.UseMiddleware<ApiKeyMiddleware>()` satırı `app.UseRouting()` satırından önce duruyor. O noktada `GetEndpoint()` `null` döner, `required` `false` olur ve kontrol sessizce atlanır. Sıranın yeri yanlış; `ApiKeyMiddleware`'in kendisi doğru. `git show HEAD -- backend/src/Tezgah.Notifications/Program.cs` kaldırılmış açıklama yorumunu da gösterir.

*EN:* In `backend/src/Tezgah.Notifications/Program.cs`, the `app.UseMiddleware<ApiKeyMiddleware>()` line sits before `app.UseRouting()`. At that point `GetEndpoint()` returns `null`, `required` becomes `false` and the check is silently skipped. The position is wrong; `ApiKeyMiddleware` itself is correct. `git show HEAD -- backend/src/Tezgah.Notifications/Program.cs` also shows the explanatory comment that was removed.
