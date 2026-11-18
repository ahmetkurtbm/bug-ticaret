# Codespaces'te çalışmak

Bilgisayarınıza hiçbir şey kurmadan, tarayıcıdan çalışıyorsunuz. Bu ekran (VS Code) GitHub'ın bir
sunucusunda çalışıyor; Docker, .NET ve Node orada kurulu.

## 1. İlk açılışta kurulumu bekleyin (bir kez, 5-10 dakika)

Sistem derlenir ve örnek veri üretilir. Alttaki terminalde `✔ Hazır` yazısını görünce hazırdır.
(Terminal görünmüyorsa: sol alttaki **Codespaces** yazısına tıklayın → **View Creation Log**.)

Sonraki açılışlarda bu adım yoktur; Codespace açılınca sistem kendiliğinden ayağa kalkar.

## 2. Backend'i başlatın: F5

Sol menüde **Run and Debug** (Ctrl+Shift+D) → üstteki listede **Tüm backend** → **F5**.

API, Ödeme ve Bildirim servisleri hata ayıklayıcıyla açılır. Alttaki panelde her biri için ayrı bir
terminal açılır ve **loglar orada canlı akar**; hata olunca orada görürsünüz. İstediğiniz satıra
tıklayarak durdurma noktası koyabilirsiniz; istek gelince VS Code orada durur.

> Veritabanı, kuyruk ve üç site Docker'da kendiliğinden çalışır. Backend ise VS Code'da çalışır ki
> durdurma noktası koymak için hiçbir şeyi kapatmanız gerekmesin.

## 3. Siteleri açın

Alttaki panelde **Ports** (Bağlantı Noktaları) sekmesi. Satırın üstüne gelip **küre simgesine**
(Open in Browser) tıklayın:

| Ports'ta adı | Tarayıcı sekmesinin adı | Ne | Hangi hesapla |
| --- | --- | --- | --- |
| Müşteri sitesi (5173) | **Tezgah** | Alışveriş sitesi | `musteri@tezgah.test` |
| Satıcı paneli (5174) | **Tezgah Satıcı Paneli** | Mağazaların paneli | `satici@tezgah.test` |
| Yönetim paneli (5175) | **Tezgah Yönetim** | Şirket çalışanlarının paneli | `admin@tezgah.test` |
| E-postalar (8025) | Mailpit | Sistemin gönderdiği e-postalar | — |
| API (5080) | Tezgah API · gelen istekler | API'ye gelen son istekler: adres, 200/401/500, süre, hata | — |
| Ödeme servisi (5090) | Tezgah Ödeme | Sahte bankanın paneli: son ödemeler | — |

Şifre her hesapta `Tezgah123!`. Ödeme sayfasında SMS kodu `123456`.

- Müşteri sitesine satıcı ya da yönetici hesabıyla da girilebilir; ama satıcının ve yöneticinin
  kendi ekranları ayrı sitelerdir. Satıcı panelinde müşteri hesabıyla giriş yaparsanız "mağaza
  başvurusu" sayfası çıkar; bu normaldir.
- Satıcı paneli yalnızca o mağazanın ürünlerinin siparişlerini gösterir. Demo satıcının ürünleri
  için müşteri sitesinin ana sayfasında **Tezgah Demo Mağaza** çipine tıklayın.
- Ödeme sayfası ilk açıldığında GitHub *"You are about to access a development port served by
  someone's codespace"* der. Bu sizin Codespace'iniz; **Continue** deyin. Her oturumda bir kez çıkar.

Önce bir tur atın: müşteri olarak demo mağazadan bir ürün alın, satıcı olarak kargoya verin,
e-postaları görün. Sistem normalde nasıl çalışıyor bilmezseniz, bozulduğunda farkı göremezsiniz.

## 4. Bir bölüm çözün

Alttaki **Terminal** sekmesine yazın:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- liste
dotnet run --project backend/tools/Tezgah.Bolum -- baslat 8
```

Bölüm başlayınca kod değişir. Değişikliğin çalışan sisteme geçmesi için:

| Ne değişti | Ne yapın |
| --- | --- |
| Backend (C#) | VS Code'da backend'i yeniden başlatın: **Ctrl+Shift+F5** |
| Siteler (React) | Terminalde: `bash .devcontainer/yenile.sh` |
| Emin değilim | İkisini de yapın |

Bölüm başlatınca ve kodu her düzelttiğinizde aynı kural geçerli.

Sonra sol taraftaki dosya listesinden `bolumler/08-eski-siparisler/KAYIT.md` dosyasını açıp hata
kaydını okuyun. Hata ayıklama sırası (ayrıntı: [Başlarken → 6. adım](BASLARKEN.md#6-hata-ayıklayın-üç-soru-üç-araç)):

1. **Tarayıcıda F12 → Network / Console:** sorun tarayıcıda mı, sunucuda mı? Ya da API'nin
   "gelen istekler" ekranı (Ports → API → küre).
2. **Log:** backend terminallerinde akan satırlar; hata hangi dosya, hangi satır.
3. **Durdurma noktası:** o satıra tıklayın, hatayı tekrar edin, değerlere bakın.

İpucu, kontrol ve bitiş:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- ipucu 8
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol
dotnet run --project backend/tools/Tezgah.Bolum -- bitir
```

Kontrol geçince bölüm "çözüldü" olarak kaydedilir; `liste` çözdüklerinizi ✔ ile gösterir.

## 5. İşiniz bitince Codespace'i durdurun

Sol alttaki **Codespaces** yazısı → **Stop Current Codespace**. (Unutursanız GitHub 30 dakika sonra
kendisi durdurur.) Silmeniz gerekmez: ertesi gün github.com/codespaces adresinden aynı Codespace'i
açın, kaldığınız yerden devam edin. Yalnızca projeyle işiniz tamamen bitince silin.

## Sık sorunlar

| Belirti | Çözüm |
| --- | --- |
| Siteler açılmıyor, "502" ya da boş sayfa | Codespace yeni açıldıysa 1 dakika bekleyin. Hâlâ yoksa terminalde: `bash .devcontainer/baslat.sh` |
| Site açılıyor ama veri gelmiyor, konsolda CORS ya da 401 | Backend çalışıyor mu? F5 → Tüm backend. Sonra sitede çıkış yapıp tekrar girin. |
| API ya da Ödeme satırı GitHub giriş sayfası açıyor | Ports'ta o satıra sağ tıklayın → Port Visibility → **Public**. (F5'ten 1 dakika sonra kendiliğinden yapılır.) |
| API'yi (5080) ya da Ödeme'yi (5090) açınca 404 | Backend çalışmıyor (F5) ya da port Public değil (yukarıdaki satır). |
| F5'te "address already in use" | Backend zaten çalışıyor (alttaki terminallere bakın) ya da Docker'da açık kalmış: `docker compose stop api payments notifications` |
| Ports sekmesinden bir satırı kapattım | Bu yalnızca tarayıcı bağlantısını keser, servisi durdurmaz. Geri açmak için **Add Port** → port numarası. |

## Bilinen farklar

- **Bölüm 1** API'nin Docker'da çalışmasını ister: o bölümde F5 yerine
  `docker compose --profile app up -d --build` kullanın.
- **Bölüm 6** Codespaces'te tekrar etmez (hata, sitelerin yerel adreslerine bağlı). Bu bölümü
  kendi bilgisayarınızda çözün: [Başlarken](BASLARKEN.md).
- API ve ödeme servisi portları herkese açıktır (siteler onlara tarayıcıdan ulaşır). Sistemde
  yalnızca kurgusal örnek veri vardır; yine de bağlantıyı başkasıyla paylaşmayın.
