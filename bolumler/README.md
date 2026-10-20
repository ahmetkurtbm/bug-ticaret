# Bölümler

Her bölüm, Tezgah'ta gerçekten yaşanabilecek bir hata kaydıdır. Kod `main` dalında doğru çalışır;
hata, bölümün dalında **bir geliştiricinin normal görünen bir commit'iyle** gelmiştir. Sizden
istenen bir şirketteki ilk haftanızda yapacağınızın aynısı:

1. Hata kaydını okuyun (`KAYIT.md`). Kayıt yalnızca kullanıcının gördüğünü anlatır.
2. Hatayı **tekrar edin**. Tekrar edemediğiniz hatayı düzelttiğinizden emin olamazsınız.
3. Belirtiden kaynağa iz sürün: tarayıcı araçları, loglar, kesme noktası, veritabanı, `git log`.
4. Kök nedeni bulun, düzeltin. Belirtiyi saklamayın (ör. `try/catch` ile susturmak çözüm değildir).
5. Kontrol aracıyla doğrulayın, sonra çözüm anlatımıyla (`COZUM.md`) karşılaştırın.

## Araç

Depo kökünden:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- liste          # bölümler
dotnet run --project backend/tools/Tezgah.Bolum -- baslat 8       # calisma/08-net-03 dalını açar
dotnet run --project backend/tools/Tezgah.Bolum -- ipucu 8        # 1. ipucu (ipucu 8 2, ipucu 8 3)
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol        # bulunduğunuz dalın bölümünü kontrol eder
dotnet run --project backend/tools/Tezgah.Bolum -- bitir          # main'e döner
```

`baslat`, bölüm dalından kendi çalışma dalınızı açar. Değişikliklerinizi orada commit'leyin;
bölüm dalı bozulmaz, istediğiniz zaman baştan başlayabilirsiniz:

```bash
git switch main && git branch -D calisma/08-net-03
```

Kontrol, hatanın **davranışını** sınar: kayıttaki senaryo artık doğru çalışıyor mu? Çoğu kontrol
çalışan servislere istek atar (önce servisleri başlatın), frontend bölümleri bir test çalıştırır.
Aynı kontroller `main` dalında her zaman geçer.

## Kurallar ve öneriler

- **`git diff main` yapmayın.** Gerçek hayatta "doğru sürüm" diye bir dal yoktur. Ama `git log`,
  `git blame` ve commit mesajları serbesttir; gerçek ekipler de bunları kullanır.
- Önce kendiniz deneyin. 20-30 dakika ilerleyemezseniz `ipucu` alın. İpuçları gittikçe açılır.
- Düzeltmenizin yanına, hatayı bir daha yakalayacak bir test eklemeyi deneyin.
- Bazı bölümler veriyi de bozar (ör. hatalı kodla kaydedilen kullanıcılar). Gerekirse örnek
  veriyi yeniden üretin: `dotnet run --project backend/tools/Tezgah.DataGen -- --profile small --reset`

## Liste

| No | Kod | Hata kaydı | Seviye | Nerede |
| --- | --- | --- | --- | --- |
| 1 | ORT-01 | [Docker'da ödeme başlamıyor](01-ort-01/KAYIT.md) | ● | Docker Compose · Ödeme |
| 2 | FE-01 | [Ürün sayfası beyaz ekran veriyor](02-fe-01/KAYIT.md) | ● | Müşteri sitesi |
| 3 | FE-12 | [Sayfayı yenileyince 404](03-fe-12/KAYIT.md) | ● | Nginx |
| 4 | API-01 | [Başvuru formu neyin yanlış olduğunu söylemiyor](04-api-01/KAYIT.md) | ● | Satıcı paneli |
| 5 | API-03 | [Askıya alınan satıcı neden giremediğini göremiyor](05-api-03/KAYIT.md) | ● | Satıcı paneli · Yetki |
| 6 | NET-01 | [Satıcı paneli hiçbir veriyi yüklemiyor](06-net-01/KAYIT.md) | ● | API · CORS |
| 7 | NET-02 | [Bildirim API'si anahtarsız açılıyor](07-net-02/KAYIT.md) | ● | Bildirim servisi |
| 8 | NET-03 | [Bazı eski siparişlerin detayı açılmıyor](08-net-03/KAYIT.md) | ● | Sipariş detayı |
| 9 | FE-02 | [Kategori sayfası durmadan istek atıyor](09-fe-02/KAYIT.md) | ● | Müşteri sitesi |
| 10 | API-02 | [Hiçbir kupon kodu kabul edilmiyor](10-api-02/KAYIT.md) | ● | Sepet |
| 11 | FE-03 | [Adet butonuna çok basınca sepet yanlış adet kaydediyor](11-fe-03/KAYIT.md) | ●● | Sepet |
| 12 | FE-14 | [1.249,90 TL'lik ürün 1,25 TL'ye satılıyor](12-fe-14/KAYIT.md) | ●● | Satıcı paneli · Fiyat |
| 13 | NET-09 | [Eski kullanıcılar giriş yapamıyor](13-net-09/KAYIT.md) | ●● | Kimlik doğrulama |
| 14 | NET-11 | [Gece yarısı başlaması gereken kampanya 03:00'te başladı](14-net-11/KAYIT.md) | ●● | Kampanya |
| 15 | EF-01 | [Yönetim sipariş listesi çok yavaş](15-ef-01/KAYIT.md) | ●● | Yönetim paneli |
| 16 | ARAÇ-01 | [Kampanya geçmişi bir süredir yanlış gösteriliyor](16-arac-01/KAYIT.md) | ●● | git bisect |
| 17 | DB-01 | [Siparişlerim sayfası saniyelerce bekliyor](17-db-01/KAYIT.md) | ●●● | Veritabanı |
| 18 | EŞ-01 | [Son ürün iki müşteriye satıldı](18-es-01/KAYIT.md) | ●●● | Stok · Eşzamanlılık |
| 19 | EŞ-02 | [Bazı siparişlerde stok iki kez düşmüş](19-es-02/KAYIT.md) | ●●● | Ödeme webhook'u |

Seviye: ● ilk aylar · ●● ilk yıl · ●●● deneyimli geliştiricinin bile zorlandığı.

Tüm hata türlerinin açıklamalı kataloğu: [HATALAR.md](../HATALAR.md).

---

## English

Each chapter is a bug report from the fictional Tezgah team. `main` is correct; each chapter's
branch adds the bug through an ordinary-looking commit. Read `KAYIT.md` (ticket, Turkish and
English), reproduce, trace, fix, then run `dotnet run --project backend/tools/Tezgah.Bolum -- kontrol`.
Hints: `ipucu <no> [1-3]`. Solutions: `COZUM.md` (Turkish, with an English summary).
Please don't `git diff main` — real projects have no "correct branch"; `git log` and `git blame` are fair game.
