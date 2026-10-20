# Çözüm · Bölüm 13 (NET-09) · Türkçe "i" problemi

## Kök neden

`backend/src/Tezgah.Application/Common/Text/SlugHelper.cs` içindeki `NormalizeEmail` metodu değişti:

```csharp
public static string NormalizeEmail(string email) => email.Trim().ToUpper();
```

`ToUpper()` geçerli kültürü (`CultureInfo.CurrentCulture`) kullanır. API `Program.cs` içinde `CultureInfo.DefaultThreadCurrentCulture` değerini `tr-TR` yapıyor (ayar: `Localization:Culture`, varsayılan `tr-TR`). Türkçede `i` harfinin büyüğü `İ` (noktalı), `ı` harfinin büyüğü `I` olur:

| E-posta | `ToUpperInvariant()` (veritabanındaki eski değer) | `ToUpper()` + `tr-TR` (hatalı kod) |
| --- | --- | --- |
| `musteri@tezgah.test` | `MUSTERI@TEZGAH.TEST` | `MUSTERİ@TEZGAH.TEST` |
| `kemal@tezgah.test` | `KEMAL@TEZGAH.TEST` | `KEMAL@TEZGAH.TEST` (içinde `i` yok) |

Bu yüzden:

- Girişte arama değeri `MUSTERİ@...` olur; veritabanında `MUSTERI@...` yazılıdır. Eşleşme olmaz, `invalid_credentials` ("E-posta veya şifre hatalı.") döner. **Adında `i` olan tüm eski kullanıcılar** etkilenir. `i` içermeyenler etkilenmez, ortak bir desen aramak bu yüzden zordur.
- Yeni kayıt da aynı hatalı fonksiyonu kullanır, `MUSTERİ@...` olarak yazılır ve girişte aynı değer aranır; yeni kullanıcılar sorunsuz girer. Hata yalnızca "eski veri ile yeni kod" birleşiminde görünür.
- Veritabanı yazma ve okuma tarafı tutarlı olsa bile **veri bozulmuştur**: hatalı sürümde kayıt olan her kullanıcının `normalized_email` değeri yanlış biçimdedir.

Değişikliği yapan geliştirici şunu düşünmüş (commit `c27ff3e`):

> E-posta normalleştirmesi sadeleştirildi

`ToUpperInvariant` uzun ve "gereksiz" görünmüş; `ToUpper()` daha kısa. Çoğu .NET geliştiricisi ikisinin aynı sonucu verdiğini varsayar. Verir, ama yalnızca kültür İngilizce ya da değişmez ise. "Bende çalışıyor" cümlesinin nedeni de bu: kültür sizin makinenizin (İngilizce Windows, çoğu container) kültürüdür. `Program.cs` içindeki ayarı çalıştırmayan her süreç (birim testi, veri üretici, bir betik) makine kültürünü kullanır.

## Nasıl bulunur

1. **Belirtiyi daraltın.** Giriş yapabilen ve yapamayan hesapları listeleyin. Yapamayanların hepsi `i` içeriyor, yapabilenlerden bazıları içermiyor. `i` içeren yeni hesap da giriş yapabiliyor; demek ki fark e-postada değil, kayıt zamanında.
2. **Kesme noktası.** `AuthService.LoginAsync` içinde `var normalized = ...` satırından sonra durun. Visual Studio'da *Locals* ya da *Watch* penceresine `normalized` ve `System.Globalization.CultureInfo.CurrentCulture.Name` yazın: değer `MUSTERİ@TEZGAH.TEST`, kültür `tr-TR`. VS Code'da (C# Dev Kit) aynı ifadeleri *Watch* bölümüne ekleyin.
3. **Veritabanıyla karşılaştırın.**

   ```bash
   docker exec -it tezgah-postgres-1 psql -U tezgah -c "select email, normalized_email from users where email like 'musteri%'"
   ```

   Çıktıda `MUSTERI@TEZGAH.TEST` görürsünüz. Ayrıca bozuk kayıtları sorgulayın: `select email, normalized_email from users where strpos(normalized_email, 'İ') > 0`.
4. **Dönüşümü bulun.** `NormalizeEmail` hem `RegisterAsync` hem `LoginAsync` içinde çağrılıyor. `git log -p -- backend/src/Tezgah.Application/Common/Text/SlugHelper.cs` metodun `ToUpperInvariant`'dan `ToUpper`'a değiştiğini gösterir.
5. **Testi çalıştırın.** `backend/tests/Tezgah.UnitTests/Text/SlugHelperTests.cs` içinde `Email_normalization_is_culture_independent` testi geçerli kültürü `tr-TR` yapar ve `MUSTERI@TEZGAH.TEST` bekler. Bu dalda `dotnet test --filter Email_normalization` ile çalıştırın: test kırmızı olmalıdır. Test var, ama hatalı commit'in CI'da kırmızı görünmesi ya da birleştirmeyi durdurması gerekirdi; durdurmadıysa asıl ikinci sorun bu.
6. **"Bende çalışıyor" araştırması.** Aynı kod farklı makinede farklı davranıyorsa önce kültürü loglayın: `CultureInfo.CurrentCulture.Name`, `Localization:Culture` ayarı, `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` ortam değişkeni. İngilizce Windows'ta ve çoğu slim container'da `ToUpper()` ve `ToUpperInvariant()` aynı sonucu verir.

## Düzeltme

```diff
-    public static string NormalizeEmail(string email) => email.Trim().ToUpper();
+    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
```

Bu tek satır **yeni** giriş denemelerini düzeltir. Eski kullanıcılar tekrar girer. Ama hatalı sürümün çalıştığı aralıkta kayıt olan kullanıcılar bu düzeltmeden sonra **giremez**: veritabanlarında `MUSTERİ@...` yazılıdır, kod artık `MUSTERI@...` arar. Bu hesaplar için veri onarımı gerekir.

### Veri onarımı

Önce etkilenen satırları ve çakışmaları görün. Hiçbir şeyi körlemesine güncellemeyin.

```sql
-- 1) Bozuk satırlar (normalized_email içinde 'İ' olanlar)
SELECT id, email, normalized_email, created_at
  FROM users
 WHERE strpos(normalized_email, 'İ') > 0
 ORDER BY created_at;

-- 2) Onarım sonucunda başka bir kayıtla çakışacaklar
--    (aynı kişi "hesabım silindi" diye yeniden kayıt olduysa çıkar)
SELECT a.id AS bozuk_id, b.id AS eski_id, a.email
  FROM users a
  JOIN users b ON b.normalized_email = translate(a.normalized_email, 'İ', 'I') AND b.id <> a.id
 WHERE strpos(a.normalized_email, 'İ') > 0;
```

İkinci sorgu satır döndürmüyorsa onarım güvenlidir:

```sql
BEGIN;
UPDATE users
   SET normalized_email = translate(normalized_email, 'İ', 'I')
 WHERE strpos(normalized_email, 'İ') > 0
   AND email !~ '[^\x01-\x7F]';   -- yalnızca ASCII e-postalar
-- sonucu kontrol edin: ilk sorgu artık yalnızca ASCII dışı e-postaları döndürmeli
COMMIT;
```

Neden `UPDATE users SET normalized_email = upper(email)` değil: Postgres'in `upper()` fonksiyonu veritabanının yerel ayarına (`LC_CTYPE`/collation) bağlıdır. Türkçe yerelli bir veritabanında `upper('i')` yine `İ` verebilir; bugün çalışan komut başka bir sunucuda aynı hatayı üretir. `translate()` ise karakter karakter, yerelden bağımsız bir çevirimdir.

**Bu yaklaşımın sınırları:**

- `translate` yalnızca `i → İ` bozulmasını geri alır. `ı` (noktasız) gibi ASCII dışı harf içeren e-postalarda doğru sonucu garanti etmez: bu harfin değişmez kültürdeki büyük hâli .NET sürümüne ve platforma göre farklılaşabilir ve `normalized_email` içindeki `I` harfinin asıl harfi `I` mı `ı` mı olduğu bilgisi kaybolmuştur. Bu yüzden sorgu `email !~ '[^\x01-\x7F]'` ile ASCII dışını dışarıda bırakır.
- ASCII dışı e-postalar (az sayıda olacaktır) için doğru yol, dönüşümü **uygulamanın düzeltilmiş fonksiyonuyla** yeniden hesaplamaktır: tek seferlik, gözden geçirilmiş bir araç ya da betik, `email` sütunundan `SlugHelper.NormalizeEmail(user.Email)` ile yeni değeri üretir ve yazar. Böylece "kural" tek yerde (kodda) kalır, SQL'de kopyalanmaz.

```csharp
var broken = await db.Users.Where(u => u.NormalizedEmail.Contains("İ")).ToListAsync(ct);
foreach (var user in broken)
    user.NormalizedEmail = SlugHelper.NormalizeEmail(user.Email);
await db.SaveChangesAsync(ct);   // tek işlemde; benzersiz index çakışırsa tamamı geri alınır
```

- Benzersiz index (`ix_users_normalized_email`) çakışmaya izin vermez. İkinci sorgu çakışma gösterirse aynı kişinin iki hesabı vardır (eski hesap ve "silinmiş sandığı için" açtığı yeni hesap). Hangisinin korunacağı, siparişlerin ve sepetlerin ne olacağı bir **iş kararıdır**; SQL ile otomatik çözülmez, destek ekibiyle karar verin.
- Onarımı bir EF migration'ı içine koymayın: migration uygulama açılışında (`Database__MigrateOnStartup`) çalışabilir ve inceleme olmadan canlı veriye dokunur. Ayrı, yedek alındıktan sonra çalıştırılan bir iş olsun.
- Olayın yönetimi: önce hatalı sürümü geri alın (ya da düzeltmeyi yayınlayın), sonra onarımı çalıştırın. Bu arada kayıt sayfasını kapatmayı değerlendirin; her yeni hesap onarılacak satır sayısını artırır.
- Yerel veritabanınızda: `DataGen --profile small --reset` tüm veriyi yeniden üretir (önce `main` ya da düzeltilmiş dalda olduğunuzdan emin olun). Ya da yukarıdaki iki SQL'i çalıştırın.

## Neden böyle düzeltiyoruz

- **`ToUpperInvariant` doğru, çünkü bu değer bir insan metni değil, bir anahtardır.** Kullanıcıya gösterilmez, yalnızca karşılaştırılır; kültürden bağımsız ve her ortamda aynı sonucu vermesi gerekir. Kullanıcıya gösterilen metinde ise doğru kültür kullanılmalıdır (`ToUpper(new CultureInfo("tr-TR"))`).
- **`ToLowerInvariant` da çözerdi**, ama veritabanındaki değerler büyük harfli; değeri değiştirmek tüm tabloyu yeniden yazmak demektir. Geri alma, mevcut veriye uyan yöntemdir.
- **`string.Equals(..., StringComparison.OrdinalIgnoreCase)`**, bellekteki karşılaştırmalar için en doğru araç olurdu. Ama burada eşleşme veritabanında yapılıyor (`WHERE normalized_email = @p`), index kullanabilmesi için sorguya hazır bir değer vermek gerekir.
- **Uygulamanın kültürünü `tr-TR` yapmaktan vazgeçmek.** Tezgah'ın sunucuları Türkçe kültürle çalışıyor (`docs/MIMARI.md`); para ve tarih biçimleri buna bağlı. Kültürü değiştirmek hatayı gizler ama başka bir ortamda (CI, başka bir bölgedeki sunucu) aynı hata geri gelir. Doğru kural: kod kültürden bağımsız doğru çalışmalı.
- **Veri onarımını atlamak.** Düzeltmenin tek başına yeterli olduğunu düşünmek, hatalı sürümde kayıt olan kullanıcıları kilitli bırakır. Her veri biçimi değişikliğinde ya da hatasında "bozuk yazılmış satırlar var mı?" diye sorulmalı.

## Bir daha olmaması için

- **Test:** `Email_normalization_is_culture_independent` testi zaten var; CI'da zorunlu olsun ve birleştirmeyi durdursun. Ek olarak testleri bir işte `tr-TR` kültürüyle (ör. `LANG=tr_TR.UTF-8` ve `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=0`) çalıştırın; "bende çalışıyor" sınıfı hataları bu iş yakalar. Giriş için "büyük harfli e-postayla da giriş yapılır" bütünleştirme testi de ekleyin (bu bölümün kontrolü gibi).
- **Analiz kuralları:** `.editorconfig` içinde `CA1304`, `CA1305` ve `CA1311` kurallarını uyarı ya da hata yapın. Kültür belirtmeyen `ToUpper()`, `ToLower()`, `string.Format` çağrılarını derleme sırasında yakalarlar.
- **Kod inceleme:** "Sadeleştirme" commit'lerinde, davranışı değiştirmediği varsayılan her değişikliğin testi var mı diye bakın. Kimlik doğrulama, para ve tarih kodunda "aynı sonucu verir" cümlesi kanıt ister.
- **İzleme:** Girişte `invalid_credentials` oranını yayın sonrası izleyin; sürümden sonra ani yükseliş alarm üretsin. Kademeli yayında (canary) bu oran ilk dakikalarda hatayı gösterirdi.
- **Veri tutarlılığı:** Normalleştirme kuralını değiştirirken (ya da hatalı değiştirdiğinde) eski veriyle uyumluluk sorusunu açıkça sorun: "Mevcut kayıtlar bu kuralla yazıldı mı?"

## İlgili

HATALAR.md · **NET-09 Türkçe "i" problemi (kültür)**

## English summary

**Root cause:** `SlugHelper.NormalizeEmail` was changed from `ToUpperInvariant()` to `ToUpper()`. The API runs with `tr-TR` (`Program.cs`), where `i` uppercases to `İ`. Existing rows hold `MUSTERI@...` (written culture-independently), so every old user whose email contains an `i` can no longer be found at login. New registrations are written and looked up with the same faulty function (`MUSTERİ@...`), so they work. The commit "E-posta normalleştirmesi sadeleştirildi" assumed both calls are the same; they are only the same under English or invariant culture, which is also why it "works on my machine".

**How to find it:** compare `normalized` in `LoginAsync` (breakpoint, Watch `CultureInfo.CurrentCulture.Name`) with `users.normalized_email` in psql; run the existing `Email_normalization_is_culture_independent` test on the branch; `git log -p` on `SlugHelper.cs`.

**Fix:** use `ToUpperInvariant()` again. Then repair data written by the faulty version: users registered during that window have `İ` in `normalized_email`. Check for conflicts first (the same person may have registered twice), then `UPDATE ... SET normalized_email = translate(normalized_email, 'İ', 'I')` for ASCII-only emails; recompute the rest with the fixed application function in a one-off reviewed tool. Do not use Postgres `upper()`: it depends on the database locale. Do not put the repair in a startup migration.

**Prevent:** make the culture test mandatory in CI, run one CI job under `tr-TR`, enable analyzers CA1304/CA1305/CA1311, and monitor the login failure rate after each release.
