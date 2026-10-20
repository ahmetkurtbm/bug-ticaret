# İpuçları · Bölüm 13 (NET-09)

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Giriş isteği 401 dönüyor; nedeni yanıt söylemez. Giriş işlemini yapan `AuthService.LoginAsync` içine kesme noktası koyun ve aramada kullanılan e-posta değerini (`normalized`) okuyun. Sonra aynı kullanıcının veritabanındaki karşılığına bakın: `docker exec -it tezgah-postgres-1 psql -U tezgah -c "select email, normalized_email from users where email like 'musteri%'"`. İki değeri yan yana, harf harf karşılaştırın.

*EN:* The login request returns 401 and the response does not say why. Put a breakpoint in `AuthService.LoginAsync` and read the email value used in the lookup (`normalized`). Then look at the same user's row in the database: `docker exec -it tezgah-postgres-1 psql -U tezgah -c "select email, normalized_email from users where email like 'musteri%'"`. Compare the two values side by side, letter by letter.

## İpucu 2

İki değer aynı görünüyor ama bir harf farklı. Hangisi? E-postadaki küçük "i" harfinin büyüğü İngilizcede `I`, Türkçede `İ` (noktalı). Aramada kullanılan değeri üreten fonksiyonu bulun ve hangi kültürle çalıştığını düşünün: API'nin kültür ayarı `Program.cs`'in başında. Yeni kayıtların neden giriş yapabildiğini de açıklayın: kayıt ve giriş aynı fonksiyonu kullanıyor mu?

*EN:* The two values look the same but one letter differs. Which one? The uppercase of a lowercase "i" is `I` in English and a dotted `İ` in Turkish. Find the function that produces the lookup value and think about which culture it runs under: the API's culture setting is at the top of `Program.cs`. Also explain why new registrations can log in: do registration and login use the same function?

## İpucu 3

`backend/src/Tezgah.Application/Common/Text/SlugHelper.cs` içindeki `NormalizeEmail` metodu `ToUpper()` çağırıyor; bu çağrı geçerli kültürü kullanır ve API `tr-TR` ile çalıştığı için `i` harfi `İ` olur. Veritabanındaki eski kayıtlar kültürden bağımsız dönüşümle (`I`) yazılmış, yeni kayıtlar ise `İ` ile yazılıyor; bu yüzden eski kullanıcılar girmiyor, yeni kullanıcılar giriyor. Depodaki birim testlerine de bakın: bu davranışı sınayan bir test var mı, bu dalda çalıştırırsanız ne olur? Düzeltme tek satır, ama hatalı sürümde açılan hesaplar için ayrıca bir veri onarımı gerekir.

*EN:* The `NormalizeEmail` method in `backend/src/Tezgah.Application/Common/Text/SlugHelper.cs` calls `ToUpper()`, which uses the current culture; the API runs with `tr-TR`, so `i` becomes `İ`. Old rows in the database were written with a culture-independent conversion (`I`), while new rows are written with `İ`; that is why old users cannot log in and new users can. Also look at the unit tests in the repository: is there a test for this behavior, and what happens when you run it on this branch? The code fix is one line, but accounts created while the faulty version was live need a separate data repair.
