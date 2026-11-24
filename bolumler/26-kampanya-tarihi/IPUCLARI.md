# İpuçları · Bölüm 26

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Kaymanın nerede doğduğunu daraltın: panel mi yanlış gönderiyor, sunucu mu yanlış kaydediyor? F12 → Network sekmesini açıp kuponu tekrar kaydedin. Kuponu oluşturan isteği (`coupons`, yöntem POST) seçin ve *Payload* sekmesindeki başlangıç ve bitiş değerlerini, formda seçtiğiniz günlerle yan yana koyun.

*EN:* Narrow down where the shift arises: does the panel send the wrong value, or does the server save it wrongly? Open F12 → Network and save the coupon again. Select the request that creates the coupon (`coupons`, method POST) and put the start and end values in the *Payload* tab next to the days you picked in the form.

## İpucu 2

İstekte günler zaten bir gün geride gidiyor; sunucu gelen değeri olduğu gibi saklıyor. Demek ki kayma yönetim panelinde, form değerinin isteğe çevrildiği yerde. `frontend/apps/admin/src/pages/CampaignsPage.tsx` içindeki `CouponForm` bileşeninde formun gönderildiği işlevi (`onSubmit`) bulun ve `startsOn` / `endsOn` alanlarının hangi yardımcıdan geçtiğine bakın. Bir gün bilgisini önce `Date` nesnesine çevirip sonra yeniden metne dökmek, hangi ek bilgilerin işin içine girmesine yol açar?

*EN:* In the request the days already go one day back; the server stores the value it receives as is. So the shift is in the admin panel, where the form value is turned into the request. In the `CouponForm` component in `frontend/apps/admin/src/pages/CampaignsPage.tsx`, find the function that submits the form (`onSubmit`) and see which helper the `startsOn` / `endsOn` fields go through. What extra information gets involved when a day is first turned into a `Date` object and then written back out as text?

## İpucu 3

`frontend/apps/admin/src/lib/dates.ts` içindeki `toApiDay` formdaki günü `new Date(year, month - 1, day)` ile oluşturuyor: bu, **tarayıcının bulunduğu yerin** gece yarısıdır. Ardından `.toISOString()` onu UTC'ye çevirip ilk 10 karakterini alıyor. Tarayıcınız UTC'nin önündeyse (İstanbul UTC+3) yerel gece yarısı, UTC'de bir önceki günün akşamına denk gelir. Konsolda `new Date(2026, 11, 1).toISOString()` yazıp deneyin; İngiltere'deki iş arkadaşınızın neden etkilenmediğini de aynı mantık açıklıyor. Önceki hâli için `git log -p -- frontend/apps/admin/src/lib/dates.ts`.

*EN:* `toApiDay` in `frontend/apps/admin/src/lib/dates.ts` builds the day from the form with `new Date(year, month - 1, day)`: this is midnight **where the browser is**. Then `.toISOString()` converts it to UTC and takes the first 10 characters. If your browser is ahead of UTC (Istanbul is UTC+3), local midnight falls on the evening of the previous day in UTC. Try `new Date(2026, 11, 1).toISOString()` in the console; the same logic explains why your colleague in the United Kingdom is not affected. For the previous version, see `git log -p -- frontend/apps/admin/src/lib/dates.ts`.
