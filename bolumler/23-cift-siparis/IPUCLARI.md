# İpuçları · Bölüm 23

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

F12 → Network sekmesinde ağ hızını "Slow 3G" yapıp "Ödemeye geç"e art arda basın. Sipariş oluşturan istekten (`POST /api/orders`) kaç tane gidiyor? Her birinin *Headers* sekmesindeki istek başlıklarına bakın: aralarında sunucunun "bu istek bir öncekinin tekrarı mı?" diye anlamasını sağlayan bir başlık var mı, varsa değerleri aynı mı farklı mı? Ayrıca düğmeye ilk basışın ardından ekranda düğmenin durumuna (tıklanabilir mi, soluk mu) bakın.

*EN:* In F12 → Network, set the speed to "Slow 3G" and press "Ödemeye geç" repeatedly. How many order-creating requests (`POST /api/orders`) go out? Look at the request headers in the *Headers* tab of each: is there a header that lets the server tell "is this request a repeat of the previous one?", and if so, are the values the same or different? Also watch the state of the button after the first press (is it clickable, is it dimmed).

## İpucu 2

İki ayrı koruma olmalı ve ikisine de bakın: (1) istemci aynı niyet için aynı anahtarı göndermeli, (2) istek sürerken düğme yeniden basılamamalı. Ödeme sayfasındaki (`pages/CheckoutPage.tsx`) sipariş verme mutasyonunu, anahtarın nerede üretildiğini ve düğmenin `disabled` koşulunu ile form gönderme işleyicisini okuyun. Anahtar ne zaman ve kaç kez üretiliyor: sayfa açıldığında bir kez mi, her tıklamada mı?

*EN:* There should be two separate protections, and you should look at both: (1) the client must send the same key for the same intent, (2) the button must not be pressable again while a request is in flight. In the checkout page (`pages/CheckoutPage.tsx`) read the order-placing mutation, where the key is generated, the `disabled` condition of the button and the form submit handler. When and how often is the key generated: once when the page opens, or on every click?

## İpucu 3

`frontend/apps/storefront/src/pages/CheckoutPage.tsx` içinde `idempotencyKey` artık `useRef` ile sabit tutulmuyor; `mutationFn` içinde her çağrıda yeni bir `crypto.randomUUID()` üretiliyor. Backend `Idempotency-Key` başlığı aynı olan ikinci isteği tekrar işlemez, ama her istek farklı anahtar taşıyınca koruma işe yaramaz. Üstüne düğmenin `disabled` koşulundan `place.isPending || place.isSuccess` ve `onSubmit`'teki `!place.isPending` denetimi de kaldırılmış; yani ikinci basış engellenmiyor. Önceki hâli için `git log -p -- frontend/apps/storefront/src/pages/CheckoutPage.tsx`.

*EN:* In `frontend/apps/storefront/src/pages/CheckoutPage.tsx` the `idempotencyKey` is no longer held steady with `useRef`; a new `crypto.randomUUID()` is generated inside `mutationFn` on every call. The backend does not process a second request with the same `Idempotency-Key` header again, but the protection does not work when every request carries a different key. On top of that, `place.isPending || place.isSuccess` was removed from the `disabled` condition of the button and the `!place.isPending` check from `onSubmit`; so a second press is not blocked. For the previous version, see `git log -p -- frontend/apps/storefront/src/pages/CheckoutPage.tsx`.
