# İpuçları · Bölüm 4 (API-01)

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Formu hatalı bilgiyle gönderin ve tarayıcıda F12 → Network sekmesine bakın. `seller-applications` isteğini seçin; *Payload* sekmesinde gönderdiğiniz JSON'u, *Response* sekmesinde sunucunun cevabını okuyun. Cevabın içindeki `errors` nesnesinin anahtarlarını (alan adlarını) harfi harfine not edin.

*EN:* Submit the form with bad data and open F12 → Network. Select the `seller-applications` request; read the JSON you sent in the *Payload* tab and the server's answer in the *Response* tab. Write down the keys (field names) in the `errors` object of the response, letter for letter.

## İpucu 2

Sunucu hatayı gönderiyor; kayıp, tarayıcıya geldikten sonra oluyor. Yanıtın `errors` anahtarları ile formun bu hataları okuduğu anahtarları karşılaştırın. Arada bir dönüştürme adımı var: `frontend/apps/seller/src/api/client.ts` içindeki `ApiError` sınıfı alan adlarını işliyor. Büyük/küçük harfe dikkat edin.

*EN:* The server does send the errors; the loss happens after they reach the browser. Compare the keys in the response's `errors` with the keys the form reads them from. There is a conversion step in between: the `ApiError` class in `frontend/apps/seller/src/api/client.ts` processes the field names. Pay attention to upper and lower case.

## İpucu 3

`ApiError` sunucunun `Iban`, `TaxNumber` gibi PascalCase anahtarlarını `iban`, `taxNumber` gibi camelCase'e çevirip `fieldErrors` içine koyuyor. `frontend/apps/seller/src/pages/ApplicationPage.tsx` içindeki `ApplicationForm` bileşeni ise alanlara `errors.Iban`, `errors.StoreName` diye PascalCase anahtarla bakıyor; bunlar hiçbir zaman dolu olmaz. `git log -p -- frontend/apps/seller/src/pages/ApplicationPage.tsx` bu satırların yakın zamanda değiştiğini gösterir.

*EN:* `ApiError` converts the server's PascalCase keys such as `Iban` and `TaxNumber` to camelCase (`iban`, `taxNumber`) and stores them in `fieldErrors`. The `ApplicationForm` component in `frontend/apps/seller/src/pages/ApplicationPage.tsx`, however, reads `errors.Iban`, `errors.StoreName` with PascalCase keys, which are never filled. `git log -p -- frontend/apps/seller/src/pages/ApplicationPage.tsx` shows these lines changed recently.
