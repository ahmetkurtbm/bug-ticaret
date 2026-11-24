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
dotnet run --project backend/tools/Tezgah.Bolum -- baslat 8       # calisma/08-eski-siparisler dalını açar
dotnet run --project backend/tools/Tezgah.Bolum -- ipucu 8        # 1. ipucu (ipucu 8 2, ipucu 8 3)
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol        # bulunduğunuz dalın bölümünü kontrol eder
dotnet run --project backend/tools/Tezgah.Bolum -- bitir          # main'e döner
```

`baslat`, bölüm dalından kendi çalışma dalınızı açar. Değişikliklerinizi orada commit'leyin;
bölüm dalı bozulmaz, istediğiniz zaman baştan başlayabilirsiniz:

```bash
git switch main && git branch -D calisma/08-eski-siparisler
```

Kontrol, hatanın **davranışını** sınar: kayıttaki senaryo artık doğru çalışıyor mu? Çoğu kontrol
çalışan servislere istek atar (önce servisleri başlatın), frontend bölümleri bir test çalıştırır.
Aynı kontroller `main` dalında her zaman geçer.

> **Codespaces'te çalışıyorsanız:** Bölüm 6 orada tekrar etmez; onu kendi bilgisayarınızda çözün.
> Diğer bölümler aynı şekilde çalışır ([docs/CODESPACES.md](../docs/CODESPACES.md)).

## Kurallar ve öneriler

- **`git diff main` yapmayın.** Gerçek hayatta "doğru sürüm" diye bir dal yoktur. Ama `git log`,
  `git blame` ve commit mesajları serbesttir; gerçek ekipler de bunları kullanır.
- Önce kendiniz deneyin. 20-30 dakika ilerleyemezseniz `ipucu` alın. İpuçları gittikçe açılır.
- Düzeltmenizin yanına, hatayı bir daha yakalayacak bir test eklemeyi deneyin.
- Bazı bölümler veriyi de bozar (ör. hatalı kodla kaydedilen kullanıcılar). Gerekirse örnek
  veriyi yeniden üretin: `dotnet run --project backend/tools/Tezgah.DataGen -- --profile small --reset`

## Liste

| No | Hata kaydı | Seviye | Nerede görülüyor |
| --- | --- | --- | --- |
| 1 | [Docker'da ödeme başlamıyor](01-docker-odeme/KAYIT.md) | ● | Müşteri sitesi · Ödeme |
| 2 | [Ürün sayfası beyaz ekran veriyor](02-urun-sayfasi/KAYIT.md) | ● | Müşteri sitesi · Ürün sayfası |
| 3 | [Sayfayı yenileyince 404](03-sayfa-yenileme/KAYIT.md) | ● | Müşteri sitesi |
| 4 | [Başvuru formu neyin yanlış olduğunu söylemiyor](04-satici-basvurusu/KAYIT.md) | ● | Satıcı paneli · Başvuru |
| 5 | [Askıya alınan satıcı neden giremediğini göremiyor](05-askidaki-satici/KAYIT.md) | ● | Satıcı paneli |
| 6 | [Satıcı paneli hiçbir veriyi yüklemiyor](06-satici-paneli-bos/KAYIT.md) | ● | Satıcı paneli |
| 7 | [Bildirim API'si anahtarsız açılıyor](07-bildirim-guvenligi/KAYIT.md) | ● | Bildirim servisi |
| 8 | [Bazı eski siparişlerin detayı açılmıyor](08-eski-siparisler/KAYIT.md) | ● | Müşteri sitesi · Siparişlerim |
| 9 | [Kategori sayfası durmadan istek atıyor](09-kategori-sayfasi/KAYIT.md) | ● | Müşteri sitesi · Kategori sayfası |
| 10 | [Hiçbir kupon kodu kabul edilmiyor](10-kupon-kodu/KAYIT.md) | ● | Müşteri sitesi · Sepet |
| 11 | [Adet butonuna çok basınca sepet yanlış adet kaydediyor](11-sepet-adedi/KAYIT.md) | ●● | Müşteri sitesi · Sepet |
| 12 | [1.249,90 TL'lik ürün 1,25 TL'ye satılıyor](12-urun-fiyati/KAYIT.md) | ●● | Satıcı paneli · Ürünler |
| 13 | [Eski kullanıcılar giriş yapamıyor](13-giris-yapilamiyor/KAYIT.md) | ●● | Giriş |
| 14 | [Gece yarısı başlaması gereken kampanya 03:00'te başladı](14-kampanya-saati/KAYIT.md) | ●● | Yönetim paneli · Kampanyalar |
| 15 | [Yönetim sipariş listesi çok yavaş](15-yonetim-siparisleri/KAYIT.md) | ●● | Yönetim paneli · Siparişler |
| 16 | [Kampanya geçmişi bir süredir yanlış gösteriliyor](16-kampanya-gecmisi/KAYIT.md) | ●● | Yönetim paneli · Kampanyalar |
| 17 | [Siparişlerim sayfası saniyelerce bekliyor](17-siparislerim-yavas/KAYIT.md) | ●●● | Müşteri sitesi · Siparişlerim |
| 18 | [Son ürün iki müşteriye satıldı](18-son-urun/KAYIT.md) | ●●● | Müşteri sitesi · Sipariş |
| 19 | [Bazı siparişlerde stok iki kez düşmüş](19-stok-dusumu/KAYIT.md) | ●●● | Sipariş · Stok |
| 20 | [Başka bir müşterinin siparişi açılabiliyor](20-baska-musterinin-siparisi/KAYIT.md) | ● | Müşteri sitesi · Siparişlerim |
| 21 | [Yeni eklenen adres listede görünmüyor](21-yeni-adres/KAYIT.md) | ● | Müşteri sitesi · Ödeme |
| 22 | [Kupon "kaydedildi" dedi ama listede yok](22-kupon-kaydedilmedi/KAYIT.md) | ● | Yönetim paneli · Kampanyalar |
| 23 | [Ödemeye geç'e iki kez basınca iki sipariş açıldı](23-cift-siparis/KAYIT.md) | ● | Müşteri sitesi · Ödeme |
| 24 | [Rapordaki ciro muhasebeyle tutmuyor](24-rapor-cirosu/KAYIT.md) | ●● | Yönetim paneli · Gösterge |
| 25 | [Satıcı panelindeki komisyon birkaç kuruş farklı](25-komisyon-farki/KAYIT.md) | ●● | Satıcı paneli · Özet |
| 26 | [1 Aralık'ı seçtim, kupon 30 Kasım'dan başlıyor](26-kampanya-tarihi/KAYIT.md) | ●● | Yönetim paneli · Kampanyalar |
| 27 | ["Kargoya verildi" e-postası müşteriye gitmiyor](27-kargo-epostasi/KAYIT.md) | ●● | Satıcı paneli · Siparişler · E-postalar |
| 28 | [Kupon kabul edilmiyor ama sebebi yazmıyor](28-kupon-sebebi/KAYIT.md) | ● | Müşteri sitesi · Sepet |
| 29 | [API Docker'da açılmıyor, bilgisayarda çalışıyor](29-docker-api/KAYIT.md) | ●● | Docker |
| 30 | [Bir ürün sayfası açılınca tuhaf bir uyarı penceresi çıkıyor](30-urun-aciklamasi/KAYIT.md) | ●● | Müşteri sitesi · Ürün sayfası |
| 31 | [Yönetim göstergesi ara sıra hata veriyor](31-gosterge-hatasi/KAYIT.md) | ●●● | Yönetim paneli · Gösterge |
| 32 | [İki sekme açıkken bir anda oturum kapanıyor](32-iki-sekme/KAYIT.md) | ●●● | Müşteri sitesi · Giriş |

Seviye: ● ilk aylar · ●● ilk yıl · ●●● deneyimli geliştiricinin bile zorlandığı.

Tüm hata türlerinin açıklamalı kataloğu: [HATALAR.md](../docs/HATALAR.md).

---

## English

Each chapter is a bug report from the fictional Tezgah team. `main` is correct; each chapter's
branch adds the bug through an ordinary-looking commit. Read `KAYIT.md` (ticket, Turkish and
English), reproduce, trace, fix, then run `dotnet run --project backend/tools/Tezgah.Bolum -- kontrol`.
Hints: `ipucu <no> [1-3]`. Solutions: `COZUM.md` (Turkish, with an English summary).
Please don't `git diff main` — real projects have no "correct branch"; `git log` and `git blame` are fair game.
