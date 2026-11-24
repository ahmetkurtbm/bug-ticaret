> **Cevap anahtarı.** Bölümü kendiniz çözmeden okumayın; önce `ipucu` komutunu deneyin.

# Çözüm · Bölüm 26 (FE-15) · Gün ile an farklı tiplerdir: tarih bir gün kayıyor

## Kök neden

`frontend/apps/admin/src/lib/dates.ts` içine yeni bir yardımcı eklenmiş ve `CampaignsPage.tsx` içindeki kupon formu `startsOn` / `endsOn` alanlarını bu yardımcıdan geçirerek gönderiyor:

```ts
export function toApiDay(value: string) {
  const [year, month, day] = value.includes('.') ? value.split('.').reverse().map(Number) : value.split('-').map(Number)
  return new Date(year, month - 1, day).toISOString().slice(0, 10)
}
```

`new Date(year, month - 1, day)` **tarayıcının yerel saat diliminde** gece yarısı oluşturur. `.toISOString()` ise anı **UTC**'ye çevirir. İstanbul UTC+3 olduğu için 1 Aralık 00:00 (yerel) = 30 Kasım 21:00 (UTC); `slice(0, 10)` de `"2026-11-30"` verir. Bitiş günü için de aynısı olur: 31 Aralık `"2026-12-30"` olur ve liste "30 Kasım – 30 Aralık" gösterir.

İngiltere'deki iş arkadaşında kış aylarında yerel saat UTC ile aynıdır (GMT, UTC+0); gece yarısı iki gösterimde de aynı gündür, kayma görünmez. Yaz saatinde (UTC+1) o da aynı hatayı yaşardı. Yani hata "bazı kullanıcılarda" değil, UTC'den ileride bulunan her tarayıcıda vardır; geliştiricinin ve test eden kişinin bulunduğu yer bunu gizlemiştir.

Değişikliği yapan geliştirici şunu düşünmüş (commit `abc1234`):

> Kupon formu elle yazılan tarihleri de kabul ediyor
> Tarih seçicisi olmayan tarayıcılarda ekip 01.12.2026 yazınca 400 alıyordu. Gün artık tek yardımcıyla API biçimine çevriliyor.

Niyet yerinde: elle yazılan `dd.mm.yyyy` biçimi de kabul edilsin. Ama bunu yapmak için günü bir `Date` nesnesine, yani **ana** çevirmek gerekmiyordu. Takvimdeki bir gün ("1 Aralık") saat ve saat dilimi taşımaz; `Date` ise belirli bir anı gösterir ve onu metne çevirirken bir saat dilimi seçmek zorundadır. Gün ile an farklı tiplerdir; birini diğerine çevirmek bilgi ekler, o bilgi de burada yanlış çıkar.

## Nasıl bulunur

1. **Belirti:** Her iki tarih de tutarlı biçimde bir gün geriye gidiyor ve yalnızca bazı tarayıcılarda. "Bir gün" kayması neredeyse her zaman saat dilimi ile gece yarısı sınırı arasındaki bir etkileşimdir.
2. **Network sekmesi:** `POST /api/admin/coupons` isteğinin *Payload* kısmı `"startsOn": "2026-11-30"` gösterir. Formda seçilen gün ile istek arasındaki fark, sunucuya değil panele işaret eder; sunucu aldığını saklıyor.
3. **Kod:** `CampaignsPage.tsx` içindeki `onSubmit` değerleri `toApiDay` ile dönüştürüyor. `git log -p -- frontend/apps/admin/src/lib/dates.ts` yardımcının ne zaman eklendiğini gösterir.
4. **Konsol denemesi:** `new Date(2026, 11, 1).toISOString()` → `"2026-11-30T21:00:00.000Z"` (İstanbul'da). Aynı dosyadaki `addDays` yardımcısı `T00:00:00Z` ekleyerek günleri **UTC gece yarısı** olarak ele alıyor; karşılaştırınca fark görünür.
5. **Saat dilimini değiştirerek:** Chrome DevTools → Sensors → Location bölümünden saat dilimini `Europe/London` yapıp aynı adımları tekrarlayın: kayma kaybolur. Bu, hatanın tarayıcının saat dilimine bağlı olduğunun kanıtıdır.

**Araç ipucu:** Tarihle ilgili bir hata aradığınızda önce konsolda `Intl.DateTimeFormat().resolvedOptions().timeZone` yazarak bulunduğunuz saat dilimini görün; sonra aynı adımları başka bir saat diliminde tekrarlayın. Sonuç değişiyorsa hata `Date` ile saat dilimi arasındadır.

## Düzeltme

Günü `Date`'e çevirmeden, metin olarak işleyin:

```diff
 export function toApiDay(value: string) {
-  const [year, month, day] = value.includes('.') ? value.split('.').reverse().map(Number) : value.split('-').map(Number)
-  return new Date(year, month - 1, day).toISOString().slice(0, 10)
+  if (DAY_PATTERN.test(value)) return value
+  const [day, month, year] = value.split('.')
+  return `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`
 }
```

`YYYY-MM-DD` gelen değer (tarih seçicisinin ürettiği) olduğu gibi geçer; elle yazılan `dd.mm.yyyy` yalnızca parçalanıp yeniden dizilir. Hiçbir aşamada saat dilimi devreye girmez. (Geçersiz girdiyi reddetmek için `isDayString` ile bir doğrulama eklemek de iyi olur.)

## Neden böyle düzeltiyoruz

- **`Date.UTC(year, month - 1, day)` kullanmak.** Çalışır: `new Date(Date.UTC(year, month - 1, day)).toISOString().slice(0, 10)` her yerde aynı sonucu verir. Ama dolambaçlıdır: günü ana çevirip yeniden güne çevirmek yerine metni doğrudan yeniden dizmek daha basit ve hataya daha az açıktır.
- **Saat farkını elle eklemek** (ör. `getTimezoneOffset()` ile düzeltmek). Yaz saati geçişlerinde ve farklı yerlerde yeniden bozulur.
- **Sunucuda "bir gün ekle" demek.** Belirtiyi maskeler; doğru çalışan istemcilerde artık bir gün fazla kaydeder.
- **Kullanıcıdan İstanbul saatinde bir tarayıcı beklemek.** Hata kodda; çözüm kullanıcıdan beklenmez.

## Bir daha olmaması için

- **Test:** Tarih dönüşümü testleri farklı saat dilimlerinde çalışsın (`TZ=Europe/Istanbul`, `TZ=UTC`, `TZ=America/Los_Angeles`). Bölümün kontrolü arayüz testini İstanbul saatinde çalıştırır; tarihe dokunan her yardımcı için ay başı ve ay sonu günleri (1 ve 31) de test edilsin, çünkü kayma en çok orada fark edilir.
- **Tip ayrımı:** "Gün" (`YYYY-MM-DD` metni) ile "an" (`Date`, ISO zaman damgası) farklı tiplerdir; kodda da öyle adlandırılsın ve birbirine yalnızca bilinçli bir fonksiyonla çevrilsin. Projede zaten `DAY_PATTERN`, `isDayString`, `addDays` gibi gün-metni yardımcıları var; yeni kod onları kullansın.
- **Kod inceleme maddesi:** Saat bileşeni olmayan bir değer için `new Date(...)` ve `toISOString()` görürseniz "hangi saat diliminde?" diye sorun.
- **Geliştirici ortamı:** Ekibin bir kısmı geliştirme makinesinde farklı bir saat dilimiyle çalışsın; bu sınıf hatalar daha erken yakalanır.

## İlgili

HATALAR.md · **FE-15 Tarih bir gün kayıyor**

## English summary

**Root cause:** the new `toApiDay` helper in `frontend/apps/admin/src/lib/dates.ts` builds the day with `new Date(year, month - 1, day)` (local midnight) and then reads `.toISOString().slice(0, 10)` (the UTC day). In Istanbul (UTC+3), December 1 00:00 local is November 30 21:00 UTC, so `"2026-11-30"` is sent; the end day becomes `"2026-12-30"`. A browser in the United Kingdom (GMT in winter) is not shifted, which hid the bug. The developer (commit "Kupon formu elle yazılan tarihleri de kabul ediyor") only wanted to accept the typed `dd.mm.yyyy` form, but a calendar day carries no time or time zone; turning it into an instant (`Date`) and back adds a time zone that nobody intended.

**How to find it:** the request payload already carries the shifted days (so the server is innocent), `onSubmit` in `CampaignsPage.tsx` passes the fields through `toApiDay`, the console shows `new Date(2026, 11, 1).toISOString()` → `2026-11-30T21:00:00.000Z`, and changing the browser's time zone to `Europe/London` makes the shift disappear.

**Fix:** handle the day as a string: return `YYYY-MM-DD` input unchanged and rearrange `dd.mm.yyyy` as text (or use `Date.UTC` if a `Date` is needed).

**Prevent:** run date tests in several time zones and with month-boundary days, keep "day" and "instant" as separate types, review any `new Date(...)`/`toISOString()` applied to a value without a time part.
