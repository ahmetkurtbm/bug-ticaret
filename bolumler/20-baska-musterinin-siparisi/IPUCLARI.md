# İpuçları · Bölüm 20

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Tek hesapla bakarsanız hata görünmez; iki hesap gerekir. F12 → Network sekmesinde ikinci müşteriyle sipariş detayının isteğini (`/api/orders/...`) seçin ve *Headers* ile *Response* sekmelerini okuyun. İstek hangi kimlikle gidiyor, yanıttaki sipariş kimin? Kendinize şu soruyu sorun: sunucu "bu kişi giriş yapmış mı?" sorusunun yanında "bu sipariş bu kişinin mi?" sorusunu da soruyor mu?

*EN:* Looking with one account will not show the bug; you need two. In F12 → Network, with the second customer, select the order detail request (`/api/orders/...`) and read the *Headers* and *Response* tabs. Which identity does the request carry, and whose order is in the response? Ask yourself: besides "is this person logged in?", does the server also ask "does this order belong to this person?"

## İpucu 2

Kimlik doğrulama ile yetkilendirme ayrı sorulardır: biri "sen kimsin?", öbürü "bunu görmeye hakkın var mı?" der. Sipariş detayını getiren API metodunu bulun (`OrdersController` → `Tezgah.Application/Orders` altındaki servis) ve metodun aldığı parametreleri, veritabanına giden sorgunun koşullarıyla yan yana koyun. Aynı serviste siparişi kullanıcıyla ilişkilendirerek okuyan başka metotlar da var; onlarla karşılaştırın.

*EN:* Authentication and authorization are different questions: one says "who are you?", the other "are you allowed to see this?" Find the API method that returns the order detail (`OrdersController` → the service under `Tezgah.Application/Orders`) and put the parameters the method receives next to the conditions of the query that goes to the database. The same service has other methods that read an order together with its user; compare with them.

## İpucu 3

`backend/src/Tezgah.Application/Orders/OrderService.cs` içindeki `GetAsync` metodu `userId` parametresini alıyor ama sipariş sorgusunun koşulunda yalnızca `o.OrderNumber == orderNumber` var; sipariş sahibine hiç bakılmıyor. Aynı dosyada `RetryPaymentAsync` sorgusunu karşılaştırın. Önceki hâli için `git log -p -- backend/src/Tezgah.Application/Orders/OrderService.cs`.

*EN:* The `GetAsync` method in `backend/src/Tezgah.Application/Orders/OrderService.cs` receives the `userId` parameter, but the condition of the order query contains only `o.OrderNumber == orderNumber`; the owner of the order is never checked. Compare with the query in `RetryPaymentAsync` in the same file. For the previous version, see `git log -p -- backend/src/Tezgah.Application/Orders/OrderService.cs`.
