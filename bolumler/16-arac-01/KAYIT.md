# TZ-4491 · Kampanya geçmişi bir süredir yanlış gösteriliyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4491 |
| Seviye | ●● (2/5) |
| Modül | Yönetim · Kampanya geçmişi |
| Bildiren | Sevgi Tunç, kampanya analisti |
| Öncelik | Orta |
| Ortam | Test ortamı. Yerelde: API + `npm run dev:admin`, yönetim paneli http://localhost:5175 |
| Durum | Açık |

## Ne oldu

Kampanya geçmişi ekranına her ay kuponlarda kimin ne yaptığını kontrol etmek için bakıyorum. İki hafta önce bu ekran doğruydu: en son yapılan işlem en üstte, sonra geriye doğru gidiyordu. Bugün açtığımda listenin en üstünde aylar öncesinin kayıtları var. Dün pasifleştirdiğim kuponun işlemini listede bulamıyorum.

Hata mesajı yok, ekran normal açılıyor, kayıtlar dolu. Ama sıralama tersine dönmüş gibi: en eski işlemden başlıyor. Kayıt sayısı artınca durum daha da kötü; en yeni işlemler hiç görünmüyor.

Bu ekran hep böyleydi diyenler var, ben öyle olmadığını biliyorum. Bu arada son iki haftada depoya çok küçük commit'ler girmiş; hangisinin neye sebep olduğunu bilen yok.

## Tekrar etme adımları

1. Altyapıyı başlatın ve örnek veriyi üretin: bkz. [Kurulum](../../docs/KURULUM.md).
2. `git log --oneline -25` ile bu dalın son değişikliklerine bakın.
3. API'yi başlatın: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
4. Başka bir terminalde: `cd frontend && npm run dev:admin`
5. http://localhost:5175 adresinde `admin@tezgah.test` / `Tezgah123!` ile giriş yapın.
6. Kampanyalar sayfasında (http://localhost:5175/kampanyalar) yeni bir kupon oluşturup birkaç kez pasifleştirin ve etkinleştirin.
7. Aşağı inip **Kampanya geçmişi** tablosuna bakın.

Hatanın hangi değişiklikle geldiğini bulmak da bu bölümün işidir. Bölüm dalında 12 commit var ve `main` dalı doğru çalışıyor. İki nokta arasını elle taramak yerine `git bisect` kullanmayı deneyin.

## Beklenen

Tablonun en üstünde en son yapılan işlem (az önce pasifleştirdiğim kupon) olur; aşağı indikçe daha eski kayıtlar gelir. En fazla istenen sayıda en yeni kayıt döner.

## Gerçekleşen

Tablonun en üstünde en eski kayıt var, aşağı indikçe yeniye gidiyor. Kayıt sayısı istenen sınırı geçince en yeni işlemler listede hiç yok.

## Ek bilgi

- İki hafta önce aynı ekran doğruydu. O zamandan beri kampanya modülüne dokunan birkaç küçük commit olmuş (açıklama satırları, kod biçimi, sıralama düzenlemeleri); hangisinin sebep olduğu belli değil.
- Kupon listesi (üstteki tablo) normal.
- Kayıtların kendisi doğru görünüyor: kim, ne zaman, hangi kupon. Yalnızca sıra ve hangilerinin geldiği yanlış.

## Bitirdiğinizde

API çalışırken şunu çalıştırın. Kontrol dört işlem yapar (oluşturma, pasifleştirme, etkinleştirme, pasifleştirme), geçmişi okur ve en yeni işlemin başta olduğunu sınar:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 16
```

`git bisect run` için yardımcı betikler bu klasörde: `bisect-kontrol.sh` (Linux, macOS, Git Bash) ve `bisect-kontrol.ps1` (Windows PowerShell). Kullanımları betiklerin başındaki yorumda yazıyor.

---

## English

# TZ-4491 · Campaign history has been wrong for a while

| Field | Value |
| --- | --- |
| Ticket | TZ-4491 |
| Level | ●● (2/5) |
| Module | Admin · Campaign history |
| Reporter | Sevgi Tunç, campaign analyst |
| Priority | Medium |
| Environment | Test environment. Locally: API + `npm run dev:admin`, admin panel at http://localhost:5175 |
| Status | Open |

### What happened

I look at the campaign history screen every month to check who did what on coupons. Two weeks ago this screen was correct: the latest action at the top, going back in time. Today when I opened it, the top of the list shows records from months ago. I cannot find the action for the coupon I deactivated yesterday.

There is no error message, the screen opens normally and the records are there. But the order looks reversed: it starts with the oldest action. The more records there are, the worse it gets; the newest actions do not appear at all.

Some people say it was always like this; I know it was not. Meanwhile, many very small commits went into the repository in the last two weeks; nobody knows which one caused what.

### Steps to reproduce

1. Start the infrastructure and generate the sample data: see [Setup](../../docs/KURULUM.md).
2. Look at the recent changes on this branch with `git log --oneline -25`.
3. Start the API: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
4. In another terminal: `cd frontend && npm run dev:admin`
5. Open http://localhost:5175 and sign in with `admin@tezgah.test` / `Tezgah123!`.
6. On the Campaigns page (http://localhost:5175/kampanyalar), create a new coupon and deactivate and activate it a few times.
7. Scroll down to the **Kampanya geçmişi** (campaign history) table.

Finding which change introduced the bug is also part of this chapter. The chapter branch has 12 commits and `main` works correctly. Instead of scanning between the two by hand, try `git bisect`.

### Expected

The top of the table shows the latest action (the coupon I just deactivated); older records follow as you scroll down. At most the requested number of the newest records is returned.

### Actual

The top of the table shows the oldest record and it goes toward newer ones. When the number of records exceeds the requested limit, the newest actions are missing from the list entirely.

### Additional information

- Two weeks ago the same screen was correct. Since then a few small commits touched the campaign module (comment lines, code formatting, ordering tweaks); it is not clear which one is responsible.
- The coupon list (the table above) is normal.
- The records themselves look right: who, when, which coupon. Only the order and which ones are returned are wrong.

### When you are done

Run this while the API is running. The check performs several actions (create, deactivate, activate, deactivate), reads the history and tests that the newest action comes first:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 16
```

Helper scripts for `git bisect run` are in this folder: `bisect-kontrol.sh` (Linux, macOS, Git Bash) and `bisect-kontrol.ps1` (Windows PowerShell). Their usage is described in the comment at the top of each script.
