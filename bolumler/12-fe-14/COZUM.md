# Çözüm · Bölüm 12 (FE-14) · Türkçe ondalık ve binlik ayracı

## Kök neden

`frontend/apps/seller/src/lib/price.ts` içindeki `parsePrice` fonksiyonu fiyat metnini şöyle çeviriyor:

```ts
const value = Number.parseFloat(input.trim().replace(',', '.'))
return Number.isFinite(value) && value > 0 ? Math.round(value * 100) / 100 : null
```

`"1.249,90"` için adımlar:

1. `replace(',', '.')` yalnızca virgülü değiştirir: `"1.249.90"`. Binlik nokta olduğu gibi kalır.
2. `Number.parseFloat("1.249.90")` geçerli sayı bittiği yerde, ikinci noktada durur ve `1.249` döner. Hata vermez.
3. `Math.round(1.249 * 100) / 100` = `1.25`. Sayı sonlu ve sıfırdan büyük olduğu için kontrol geçer.

Sonuç `₺1,25`. Form `null` almadığı için "Fiyatı 1.249,90 biçiminde yazın" hata mesajını hiç göstermez.

Türkçede nokta binlik, virgül ondalık ayracıdır. `parseFloat` ise ayraç kavramını bilmez; "metnin başındaki sayıya benzeyen kısmı" okur ve kalanını yok sayar. Bu iki özellik birleşince hata sessiz olur.

Değişikliği yapan geliştirici şunu düşünmüş (commit `40bc4ef`):

> Fiyat girişi: noktalı ondalık da kabul ediliyor
> Satıcılardan "249.90 yazınca hata veriyor" şikâyeti geldi.

Şikâyet gerçekti ve sezgisi yanlış değildi: satıcılar iki biçimi de yazıyor. Ama `"249.90"` ile `"1.249"` aynı karakterle iki ayrı şey söylüyor. Nokta bir yerde ondalık, başka bir yerde binlik olabiliyor. "İkisini de kabul et" demek, bu belirsizliği çözmeden tahmin etmek demektir. Commit, öncekinde bilinçli olan iki düzenli ifadeyi (regex) ve onların "geçersizse `null` dön" davranışını da kaldırdı.

## Nasıl bulunur

1. **İsteğe bakın.** Satıcı panelinde fiyatı `1.249,90` yazıp kaydedin. Network sekmesinde kaydet isteğinin gövdesinde `price: 1.25` görünür. Sunucu aldığını kaydetmiş; hata istemcide.
2. **Dönüşümü bulun.** Fiyat alanının `onSaveSubmit` içinde `parsePrice(price)` ile sayıya çevrildiğini görürsünüz (`pages/ProductDetailPage.tsx`, `pages/ProductNewPage.tsx`). Fonksiyon `src/lib/price.ts` içinde.
3. **Adım adım deneyin.** Tarayıcı konsolunda:

   ```js
   '1.249,90'.replace(',', '.')              // "1.249.90"
   Number.parseFloat('1.249.90')              // 1.249
   Math.round(1.249 * 100) / 100              // 1.25
   ```

   Üç satır, üç adım; kesilen yer görünür.
4. **Kesme noktası.** VS Code'da *Satıcı paneli (Chrome)* yapılandırmasını başlatın (önce `npm run dev:seller`), `parsePrice` içindeki `const value` satırına kesme noktası koyun. Satırı atlayıp (*Step over*) `value` değerine bakın. Visual Studio frontend için gerekmez.
5. **Testi çalıştırın.** `npm run test:bolum -w seller -- fe14` hangi girdilerin bozulduğunu listeler (`"1.249,90"`: beklenen 1249.9, gelen 1.25).
6. **Geçmişe bakın.** `git log -p -- frontend/apps/seller/src/lib/price.ts` iki regex'in silindiğini ve `parseFloat`'ın geldiğini gösterir.

## Düzeltme

Girdiyi önce biçimine göre doğrula, tanınmayanı reddet:

```diff
 // Türkçe fiyat biçimi: binlik ayracı nokta (isteğe bağlı), ondalık ayracı virgül, en çok 2 ondalık.
+const GROUPED = /^\d{1,3}(\.\d{3})+(,\d{1,2})?$/
+const PLAIN = /^\d+(,\d{1,2})?$/
 
-/** Fiyat girdisini sayıya çevirir. Hem "249,90" hem "249.90" kabul edilir; satıcılar ikisini de yazıyor. */
+/** "1.249,90" -> 1249.9. Geçersiz ya da 2 ondalığı aşan girdide null döner ("249.90" da geçersizdir: nokta binlik ayracıdır). */
 export function parsePrice(input: string): number | null {
-  const value = Number.parseFloat(input.trim().replace(',', '.'))
-  return Number.isFinite(value) && value > 0 ? Math.round(value * 100) / 100 : null
+  const text = input.trim()
+  if (!GROUPED.test(text) && !PLAIN.test(text)) return null
+  const value = Number(text.replace(/\./g, '').replace(',', '.'))
+  return Number.isFinite(value) ? value : null
 }
```

`"249.90"` artık `null` döner ve form "Fiyatı 1.249,90 biçiminde yazın (en çok 2 ondalık)." mesajını gösterir. Satıcının asıl isteği ("249.90 yazınca hata veriyor") böylece anlamlı bir hata mesajı olarak çözülür: hata veriyor, ama neyi düzelteceğini söylüyor.

## Neden böyle düzeltiyoruz

- **"İki biçimi de kabul et, son ayraç ondalıktır" gibi sezgiler.** `"249.90"` için çalışır, `"1.249"` için çalışmaz: bu bin iki yüz kırk dokuz mu, 1,249 mu? Kullanıcı Türkçe yazıyorsa ilkidir. Yanlış tahmin para demektir. Belirsiz girdiyi tahmin etmeyip reddetmek ve nedenini kullanıcıya göstermek her zaman daha ucuzdur.
- **`<input type="number">`.** Tarayıcının diline göre farklı davranır, değeri her zaman noktalı verir, `1e3` gibi girdileri kabul eder, fare tekerleğiyle değer değiştirir ve virgüllü yazımı bazı tarayıcılarda boş döndürür. Para için uygun değildir.
- **`parseFloat`/`Number` ile ayraçları temizleyip bırakmak.** `parseFloat("12abc")` 12 döner; `parseFloat` kısmi ayrıştırma yapar. Kullanıcı girdisini sayıya çevirirken "tamamı sayı mıydı?" kontrolü şarttır.
- **Kütüphane (ör. yerel ayarlı ayrıştırıcılar).** İşe yarar, ama yine "nokta ne demek" sorusunu bir kural olarak yanıtlamak zorundasınız. Bir regex'le net yazmak bu küçük alan için daha okunur.
- **`Math.round(value * 100) / 100` ile 3. ondalığı sessizce yuvarlamak.** `12,345` yazan kişi 12,35 mi 12,34 mü demek istedi? Kaydetmeden önce sormak gerekir; bu yüzden en çok 2 ondalık kuralı reddeder.
- **Kalan risk:** `"1.249"` yazan kişi 1,249 demek istemiş olabilir. Kural bu girdiyi 1249 TL sayar. Bunu azaltmak için ayrıştırılan değeri `formatPriceInput` ile alana geri yazmak (`1.249,00`) ve kaydetmeden önce göstermek yardımcı olur.

## Bir daha olmaması için

- **Test:** `src/__bolum__/fe14-fiyat.test.ts` zaten tablo testi içerir. Gerçek projede buna gidiş-dönüş testi ekleyin: rastgele fiyatlar için `parsePrice(formatPriceInput(x)) === x` (özellik tabanlı test, ör. `fast-check`), ve "geçersiz girdi `null` döner" testleri.
- **Kod inceleme:** Kullanıcı girdisini sayıya çeviren yerde `parseFloat`, `Number`, `+x` gören herkes "kısmi ayrıştırma var mı, geçersizde ne dönüyor?" diye sormalı. "Şikâyeti çözdü" bir gerekçe, ama "başka neyi bozar?" sorusunu atlatmaz.
- **Sunucu tarafı koruma:** Fiyatı sunucuda da doğrulayın (alt/üst sınır). Ek olarak fiyat değişimi büyükse (ör. bir güncellemede %90'dan fazla düşüş) kaydı hemen yayına almayın: satıcıya onay sorun ya da yönetici incelemesine düşürün ve olayı denetim kaydına yazın. Bu, hata hangi katmandan gelirse gelsin 1.249,90 → 1,25 gibi düşüşü kullanıcıdan önce yakalar.
- **İzleme:** Günlük "fiyatı %80'den fazla düşen ürün sayısı" uyarısı. Bu hatadan etkilenmiş kayıtları bulmak için de aynı sorgu kullanılır: hatalı sürümün yayında olduğu aralıkta fiyatı aniden çok düşen varyantları listeleyip satıcılarla doğrulayın.
- **Sözleşme:** API'ye her zaman noktalı sayı ya da kuruş cinsinden tamsayı gönderin; yerel biçim yalnızca ekranda yaşasın.

## İlgili

HATALAR.md · **FE-14 Ondalık sayı ve para biçimi (Türkçe virgül)**

## English summary

**Root cause:** `parsePrice` in `frontend/apps/seller/src/lib/price.ts` did `parseFloat(input.replace(',', '.'))`. For `"1.249,90"` this gives `"1.249.90"`, and `parseFloat` stops at the second dot and returns `1.249`, rounded to `1.25`. The thousands dot is never handled and invalid input is silently truncated instead of rejected, so the form error never shows. The commit "Fiyat girişi: noktalı ondalık da kabul ediliyor" wanted `249.90` to work, but a dot is ambiguous: `249.90` (decimal) versus `1.249` (thousands).

**How to find it:** the Network request already carries `price: 1.25`; step through the conversion in the console or with a breakpoint; the chapter test lists the broken inputs; `git log -p` on `price.ts` shows the removed regexes.

**Fix:** validate the input against the Turkish format (optional dot-grouped thousands, comma decimal, at most 2 decimals) and return `null` for anything else, so the form can tell the user. `249.90` is rejected with a helpful message rather than guessed.

**Prevent:** round-trip and invalid-input tests, review of every user-input-to-number conversion, server-side bounds, and a review or alert step for sudden large price drops (for example more than 90%).
