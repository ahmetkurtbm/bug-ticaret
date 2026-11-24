# İpuçları · Bölüm 22

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

F12 → Network sekmesini açıp kuponu kaydetmeyi tekrar deneyin. Kupon oluşturan isteği (`coupons`, yöntem POST) seçin. *Headers* sekmesindeki **durum kodu** (Status) ne, *Response* sekmesindeki gövdede ne yazıyor? Panelin "başarılı" saymasıyla bu iki bilgiyi birlikte düşünün: tarayıcı ve istemci kodu bir isteğin başarılı olup olmadığına neye bakarak karar verir?

*EN:* Open F12 → Network and try saving the coupon again. Select the request that creates the coupon (`coupons`, method POST). What is the **status code** in the *Headers* tab, and what does the body in the *Response* tab say? Think about these two things together with the panel treating the call as successful: what do the browser and the client code look at to decide whether a request succeeded?

## İpucu 2

Panel yalnızca durum koduna bakıyor; gövdede hata alanı olsa bile `2xx` gelen isteği başarılı sayıyor. Öyleyse hata sunucuda, yanıtı üreten yerde. `AdminController` içinde kupon oluşturma işlemini yapan eylem (`CreateCoupon`) ile aynı denetleyicideki öteki eylemleri karşılaştırın. Ayrıca projede hataları merkezi bir yerde yanıta çeviren bir sınıf var (`Tezgah.Api/Infrastructure/DomainExceptionHandler.cs`): doğrulama hataları normalde oradan hangi durum koduyla çıkıyor?

*EN:* The panel looks only at the status code; it treats a `2xx` response as successful even if the body carries an error field. So the fault is on the server, where the response is produced. Compare the action that creates the coupon in `AdminController` (`CreateCoupon`) with the other actions of the same controller. The project also has a class that turns errors into responses in one central place (`Tezgah.Api/Infrastructure/DomainExceptionHandler.cs`): with which status code do validation errors normally leave from there?

## İpucu 3

`backend/src/Tezgah.Api/Controllers/AdminController.cs` içindeki `CreateCoupon` eylemi `RequestValidationException`'ı kendisi yakalıyor ve `Ok(new { error = ... })` ile `200` dönüyor. Bu yüzden merkezi hata işleyici hiç devreye girmiyor, panelin `onSuccess` kolu çalışıp formu kapatıyor. Doğrulama hatası `400` ve `ValidationProblemDetails` olarak dönmeli; bunu zaten merkezi işleyici yapıyor. Önceki hâli için `git log -p -- backend/src/Tezgah.Api/Controllers/AdminController.cs`.

*EN:* The `CreateCoupon` action in `backend/src/Tezgah.Api/Controllers/AdminController.cs` catches `RequestValidationException` itself and returns `200` with `Ok(new { error = ... })`. So the central error handler never runs, the panel's `onSuccess` branch runs and closes the form. A validation error should come back as `400` with `ValidationProblemDetails`; the central handler already does this. For the previous version, see `git log -p -- backend/src/Tezgah.Api/Controllers/AdminController.cs`.
