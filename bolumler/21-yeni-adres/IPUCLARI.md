# İpuçları · Bölüm 21

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

F12 → Network sekmesini açıp *Fetch/XHR* süzgecini seçin, sonra "Adresi kaydet"e basın. Sırayla hangi istekler gidiyor? Kayıt isteğinin (`POST`) durum kodu ve yanıtı ne? Ardından adres listesini getiren bir `GET` isteği gidiyor mu, yoksa hiç yeni istek yok mu? Sayfayı yenilediğinizde listeyi getiren isteğin gelmesiyle bunu karşılaştırın.

*EN:* Open F12 → Network, pick the *Fetch/XHR* filter, and press "Adresi kaydet". Which requests go out, in order? What are the status code and response of the save request (`POST`)? After it, does a `GET` request that fetches the address list go out, or is there no new request at all? Compare this with the request that fetches the list when you refresh the page.

## İpucu 2

Kayıt başarılıysa ve liste isteği gitmiyorsa, ekran eski veriyi tutmaya devam ediyordur. Müşteri sitesi sunucudan gelen veriyi TanStack Query ile tutuyor: her veri bir "sorgu anahtarı"yla saklanıyor ve bir değişiklikten sonra ilgili anahtar "bayat" işaretlenince yeniden çekiliyor. Ödeme sayfasındaki (`pages/CheckoutPage.tsx`) iki parçaya bakın: adres listesini okuyan sorgu hangi anahtarla kayıtlı, adres formu başarıyla bitince hangi anahtarı bayat işaretliyor? İkisini yan yana yazın.

*EN:* If the save succeeds and no list request goes out, the screen keeps holding old data. The storefront holds server data with TanStack Query: each piece of data is stored under a "query key", and after a change the matching key is marked stale so it is fetched again. Look at two parts of the checkout page (`pages/CheckoutPage.tsx`): under which key is the query that reads the address list registered, and which key does the address form mark stale when it finishes successfully? Write the two side by side.

## İpucu 3

`frontend/apps/storefront/src/pages/CheckoutPage.tsx` içinde liste `addressesKey = ['addresses']` anahtarıyla okunuyor; `AddressForm`'un `onSuccess` işleyicisi ise `['me', 'addresses']` anahtarını bayat işaretliyor. TanStack Query anahtarları **baştan** (önek olarak) eşler: `['me', 'addresses']` anahtarı `['addresses']` sorgusunu kapsamaz, o yüzden hiçbir sorgu yeniden çekilmez. Önceki hâli için `git log -p -- frontend/apps/storefront/src/pages/CheckoutPage.tsx`.

*EN:* In `frontend/apps/storefront/src/pages/CheckoutPage.tsx` the list is read with the key `addressesKey = ['addresses']`, while the `onSuccess` handler of `AddressForm` marks `['me', 'addresses']` stale. TanStack Query matches keys **from the start** (as a prefix): the key `['me', 'addresses']` does not cover the `['addresses']` query, so no query is fetched again. For the previous version, see `git log -p -- frontend/apps/storefront/src/pages/CheckoutPage.tsx`.
