# Codespaces'te çalışmak

Bilgisayarınıza hiçbir şey kurmadan, tarayıcıdan çalışıyorsunuz. Bu ekran (VS Code) GitHub'ın bir
sunucusunda çalışıyor; Docker, .NET ve Node orada kurulu.

## 1. Sistemin hazır olmasını bekleyin

İlk açılışta sistem derlenir ve örnek veri üretilir: **5-10 dakika** sürer. Alttaki terminalde
`✔ Hazır` yazısını görünce hazırdır. (Terminal kapalıysa: üst menü → Terminal → New Terminal.
Kurulumun ilerleyişi için sol altta "Codespaces" yazısına tıklayıp "View Creation Log" seçin.)

## 2. Siteleri açın

Alttaki panelde **Ports** (Bağlantı noktaları) sekmesine geçin. Her satırda bir site var:

| Ad | Ne |
| --- | --- |
| Müşteri sitesi | Alışveriş sitesi |
| Satıcı paneli | Mağazaların paneli |
| Yönetim paneli | Şirket çalışanlarının paneli |
| E-postalar (Mailpit) | Sistemin gönderdiği e-postalar |

Satırın üstüne gelip **küre simgesine** (Open in Browser) tıklayın; site yeni sekmede açılır.

Ödeme sayfası ilk açıldığında GitHub *"You are about to access a development port served by someone's
codespace"* diye bir uyarı gösterir. Bu sizin kendi Codespace'iniz; **Continue** deyin. Uyarı her
Codespace oturumunda bir kez çıkar.

Hesaplar (şifre hepsinde `Tezgah123!`): `musteri@tezgah.test`, `satici@tezgah.test`,
`admin@tezgah.test`. Ödeme sayfasında SMS kodu `123456`.

Önce bir tur atın: müşteri olarak sipariş verin, satıcı olarak kargoya verin, yönetici olarak
siparişlere bakın. Sistem normalde nasıl çalışıyor bilmezseniz, bozulduğunda farkı göremezsiniz.

## 3. Bir bölüm çözün

Terminale yazın:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- liste
dotnet run --project backend/tools/Tezgah.Bolum -- baslat 8
docker compose --profile app up -d --build
```

Son komut sistemi bölümün koduyla yeniden derler. **Kodda her değişiklikten sonra** (bölüm başlatınca
ya da siz düzeltince) bu komutu tekrar çalıştırın.

Sonra sol taraftaki dosya listesinden `bolumler/08-eski-siparisler/KAYIT.md` dosyasını açıp hata
kaydını okuyun. Hata ayıklamanın geri kalanı bilgisayardakiyle aynıdır:
[Başlarken → 6. adım](BASLARKEN.md#6-hata-ayıklayın-üç-soru-üç-araç). Kısaca:

- Tarayıcıda **F12 → Network / Console**: sorun tarayıcıda mı, sunucuda mı?
- **Log**: `docker compose logs api --tail 50`
- **Durdurma noktası**: `docker compose stop api`, sonra soldaki **Run and Debug** → "API (monolit)" → F5.

İpucu ve kontrol:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- ipucu 8
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol
```

## 4. İşiniz bitince Codespace'i durdurun

Codespaces aylık ücretsiz kotayla çalışır; açık kaldığı her saat bu kotadan düşer. Bitirince sol
alttaki **Codespaces** yazısına tıklayın → **Stop Current Codespace**. (Unutursanız GitHub, 30 dakika
hiçbir şey yapılmayınca kendisi durdurur.)

Tekrar açmak için github.com/codespaces adresinden aynı Codespace'i seçin; kaldığınız yerden devam
edersiniz, sistem kendiliğinden açılır. Çalışmanız siz Codespace'i silene kadar orada durur.

## Bilinen farklar

- **Bölüm 6** Codespaces'te tekrar etmez (hata, sitelerin yerel adreslerine bağlı). Bu bölümü
  bilgisayarınızda çözün: [Başlarken](BASLARKEN.md).
- API ve ödeme servisi portları herkese açıktır (siteler onlara tarayıcıdan ulaşır). Sistemde
  yalnızca kurgusal örnek veri vardır; yine de bağlantıyı başkasıyla paylaşmayın.
- Sayfa "port is not public" ya da GitHub giriş sayfası gösterirse: Ports sekmesinde API (5080) ve
  Ödeme servisi (5090) satırlarına sağ tıklayın → Port Visibility → Public.
