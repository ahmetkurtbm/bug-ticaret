# Çözüm · Bölüm 4 (API-01) · Doğrulama hataları alanlara ulaşmıyor

## Kök neden

Sunucu, geçersiz başvuruya `400` ve bir `ValidationProblemDetails` gövdesi döner. Alan hataları `errors` nesnesindedir ve anahtarlar C# özellik adlarıyla, yani PascalCase yazılır:

```json
{
  "status": 400,
  "title": "Girilen bilgileri kontrol edin.",
  "errors": { "Iban": ["IBAN geçersiz. TR ile başlayan 26 karakterli IBAN girin."] }
}
```

Satıcı uygulamasındaki `ApiError` (`src/api/client.ts`) bu anahtarları camelCase'e çevirip `fieldErrors` içine koyar: `Iban` → `iban`. Formlar `errors.iban` diye okumalıdır.

Hatalı commit formda `errors.iban` yerine `errors.Iban`, `errors.storeName` yerine `errors.StoreName` yazmış. Bu anahtarlar `fieldErrors` içinde hiç yok, bu yüzden hiçbir alan hata göstermez. Üstelik genel hata kutusu yalnızca `fieldErrors` boşsa çıkar; doluyken (ama alanlar yanlış anahtarla okunuyorken) o da çıkmaz. Sonuç: sessiz form.

Değişikliği yapan geliştirici şunu düşünmüş (commit `4e67a5b`):

> Başvuru formu: alan adları backend DTO'su ile aynı yazıldı

Network sekmesinde `Iban` gördü, formda `iban` gördü ve "backend ile aynı olsun" diye eşitledi. Arada `ApiError`'ın anahtarları camelCase'e çevirdiğini bilmiyordu (ya da unuttu); `values` içindeki alan adları da camelCase olduğu için kendi içinde tutarsız bir sonuç çıktı.

## Nasıl bulunur

1. **Belirti:** Düğmeye basınca hiçbir şey yazmıyor. Önce "istek gitti mi, ne döndü?" diye bakılır.
2. **Araç: Network → isteği seç.** *Payload* gönderilen alanları, *Response* sunucunun cevabını gösterir. Durum 400, `errors` içinde `Iban` var. Yani sunucu doğru çalışıyor ve hatayı veriyor; sorun istemcide.
3. **Araç: konsol / kesme noktası.** `ApplicationForm` içinde `const errors = ...` satırının altına kesme noktası koyun ya da geçici olarak `console.log(errors)` yazın. Çıktı `{ iban: "IBAN geçersiz..." }`: anahtar küçük harfle başlıyor.
4. **Dosya:** `frontend/apps/seller/src/pages/ApplicationPage.tsx`, `ApplicationForm`, `<Field ... error={errors.Iban} />` satırları.
5. **Neden camelCase?** `client.ts` içindeki yorum ve `ApiError` oluşturucusu anahtar dönüşümünü yapıyor. `git log -p -- frontend/apps/seller/src/pages/ApplicationPage.tsx` dönüşümün formdan değil istemciden geldiğini ve son commit'te form anahtarlarının değiştiğini gösterir.

**Visual Studio:** Frontend için tarayıcı araçlarını kullanın.

**VS Code:** "Satıcı paneli (Chrome)" yapılandırmasıyla başlatıp `ApplicationPage.tsx` içinde kesme noktası koyun; *Variables* panelinde `errors` nesnesini açın.

## Düzeltme

```diff
-<Field label="Mağaza adı" name="storeName" ... error={errors.StoreName} />
-<Field label="Ticari unvan / ad soyad" name="legalName" ... error={errors.LegalName} />
-<Field label="Vergi no / T.C. kimlik no" name="taxNumber" ... error={errors.TaxNumber} ... />
-<Field label="Vergi dairesi" name="taxOffice" ... error={errors.TaxOffice} />
-<Field label="IBAN" name="iban" ... error={errors.Iban} ... />
-<Field label="Şehir" name="city" ... error={errors.City} ... />
+<Field label="Mağaza adı" name="storeName" ... error={errors.storeName} />
+<Field label="Ticari unvan / ad soyad" name="legalName" ... error={errors.legalName} />
+<Field label="Vergi no / T.C. kimlik no" name="taxNumber" ... error={errors.taxNumber} ... />
+<Field label="Vergi dairesi" name="taxOffice" ... error={errors.taxOffice} />
+<Field label="IBAN" name="iban" ... error={errors.iban} ... />
+<Field label="Şehir" name="city" ... error={errors.city} ... />
```

(`...` diğer prop'ları gösterir; yalnızca `error` değerleri değişir. Tam hâli `git show main:frontend/apps/seller/src/pages/ApplicationPage.tsx` içindedir.)

## Neden böyle düzeltiyoruz

- **`ApiError` içindeki camelCase dönüşümünü kaldırmak** üç uygulamadaki (müşteri, satıcı, yönetim) bütün formları etkiler; sözleşme tek yerde ve belgelenmiş durumda. Bir formdaki hatayı ortak kodu bozarak çözmek yanlış.
- **Backend'i camelCase anahtar üretecek şekilde değiştirmek** `errors` sözleşmesini değiştirir, başka istemcileri (ve testleri) kırar. ASP.NET Core'un standart davranışı özellik adıdır.
- **Formda hem `errors.iban ?? errors.Iban` okumak** sorunu örter. Hangisinin doğru olduğu bilinmeden yazılan kod bir sonraki kişiyi aynı belirsizlikle bırakır.
- **Formu "backend ile aynı" yazmak** iyi niyetli ama yanlış hedef: form, backend'in ham cevabıyla değil `ApiError`'ın sunduğu arayüzle konuşur.

## Bir daha olmaması için

- **Test:** Bu bölümün testi gibi, sunucunun gerçek yanıt şeklini (`errors: { Iban: [...] }`) `ApiError`'dan geçirip formun alt satırında göstermesini bekleyen bir bileşen testi bulunsun.
- **Tip güvenliği:** `fieldErrors` tipi `Record<string, string>` olduğu için `errors.Iban` derleme hatası vermiyor. Form alanlarının anahtarlarına bağlı bir tip (`Partial<Record<keyof FormValues, string>>`) kullanılırsa yanlış anahtar derlemede yakalanır.
- **Beklenmeyen hata görünürlüğü:** Sunucu alan hatası gönderdi ama hiçbir alan onu göstermediyse (anahtar eşleşmedi), genel hata kutusu yine de açılsın. Sessizlik en kötü sonuçtur.
- **Kod inceleme maddesi:** Sözleşme (API) ile ilgili bir isim değişikliğinde "bu adın istemciye gelene kadar hangi katmanlardan geçtiğini" sorun; Network'te gördüğünüz ham isim, kodda okuduğunuz isim olmayabilir.

## İlgili

HATALAR.md · **API-01 400 Bad Request: model doğrulama**

## English summary

**Root cause:** the server returns 400 with `errors` keyed in PascalCase (`Iban`). The seller app's `ApiError` converts them to camelCase (`fieldErrors.iban`). The commit "Başvuru formu: alan adları backend DTO'su ile aynı yazıldı" changed the form to read `errors.Iban`, `errors.StoreName`, etc., which never exist, so no field shows an error and the general alert (shown only when `fieldErrors` is empty) stays hidden too.

**How to find it:** Network tab (Payload vs Response: 400 with `errors.Iban`) -> breakpoint or `console.log(errors)` in `ApplicationForm` shows `{ iban: ... }` -> `ApplicationPage.tsx` reads `errors.Iban`.

**Fix:** read the camelCase keys the client produces: `errors.storeName`, `errors.legalName`, `errors.taxNumber`, `errors.taxOffice`, `errors.iban`, `errors.city`.

**Prevent:** component test with the real response shape, type the field-error keys against the form values, show a general error when server errors match no field, and ask in review which layers a renamed contract field passes through.
