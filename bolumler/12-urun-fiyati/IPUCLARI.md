# İpuçları · Bölüm 12 (FE-14)

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Fiyatın nerede bozulduğunu bulmak için kaydet isteğini izleyin: tarayıcıda Network sekmesini açın, fiyatı `1.249,90` yazıp kaydedin ve isteğin gövdesindeki `price` değerine bakın. Değer zaten yanlışsa sorun sunucuda değil, formda. Sonra satıcı panelinde (`frontend/apps/seller/src`) formdaki metni sayıya çeviren fonksiyonu bulun.

*EN:* To find where the price breaks, watch the save request: open the Network tab, type `1.249,90`, save, and look at the `price` value in the request body. If it is already wrong, the problem is in the form, not on the server. Then find the function that turns the form's text into a number in the seller panel (`frontend/apps/seller/src`).

## İpucu 2

Sorun bir metinden sayıya dönüşümde. Türkçede nokta binlik ayracı, virgül ondalık ayracıdır; JavaScript'in sayı ayrıştırıcıları ise yalnızca noktayı ondalık sayar. Fonksiyonda her adımdan sonra değerin ne olduğunu yazdırın (tarayıcı konsolunda adımları tek tek deneyebilirsiniz) ve son sayıyı neyin kestiğini bulun. Bir de şunu düşünün: bozuk girdi gelince fonksiyon hata mı veriyor, yoksa sessizce bir sayı mı üretiyor?

*EN:* The problem is in converting text to a number. In Turkish, the dot is the thousands separator and the comma is the decimal separator, while JavaScript's number parsers treat only the dot as decimal. Print the value after each step in the function (you can try the steps one by one in the browser console) and find what cuts the final number. Also think: when the input is malformed, does the function raise an error, or silently produce a number?

## İpucu 3

`frontend/apps/seller/src/lib/price.ts` içindeki `parsePrice` girdiyi `replace(',', '.')` ile değiştirip `Number.parseFloat`'a veriyor. `"1.249,90"` önce `"1.249.90"` oluyor; `parseFloat` ikinci noktada durup `1.249` döndürüyor ve yuvarlama bunu `1,25` yapıyor. Binlik nokta hiç ele alınmıyor ve geçersiz girdi `null` yerine kırpılmış bir sayı üretiyor, bu yüzden formdaki hata mesajı hiç görünmüyor. `"249.90"` yazılabilsin diye bu gevşeklik eklenmişti; ama Türkçe girdide `1.249` nokta ile "bin iki yüz kırk dokuz"dur, "bir virgül iki yüz kırk dokuz" değil. Belirsiz girdiyi tahminle çevirmek yerine ne yapılmalı, düşünün.

*EN:* In `frontend/apps/seller/src/lib/price.ts`, `parsePrice` replaces the comma with `replace(',', '.')` and passes the result to `Number.parseFloat`. `"1.249,90"` first becomes `"1.249.90"`; `parseFloat` stops at the second dot and returns `1.249`, which rounding turns into `1.25`. The thousands dot is never handled, and invalid input yields a truncated number instead of `null`, so the form's error message never appears. This looseness was added so that `"249.90"` could be typed; but in a Turkish input `1.249` with a dot means one thousand two hundred forty-nine, not one point two four nine. Think about what to do with ambiguous input instead of guessing.
