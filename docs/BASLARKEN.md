# Başlarken: sıfırdan ilk bölüme

> Hiçbir şey kurmadan tarayıcıdan çalışmak isterseniz: [Codespaces'te çalışmak](CODESPACES.md).

Bu sayfa projeyi ilk kez indiren biri için yazıldı. Sırayla gidin; her adımda ne yaptığınızı ve
neden yaptığınızı anlatır. Ayrıntılar için: [Kurulum](KURULUM.md), [IDE rehberi](IDE.md).

## 1. Bilgisayarı hazırlayın (bir kez, yaklaşık 30 dakika)

| Program | Ne işe yarar | Nereden |
| --- | --- | --- |
| Git | Projeyi indirmek, bölümler arasında geçmek | git-scm.com |
| Docker Desktop | Veritabanı ve servisleri tek komutla çalıştırmak | docker.com |
| .NET 10 SDK | Backend'i ve bölüm aracını çalıştırmak | dotnet.microsoft.com |
| Node.js 22 | Arayüzleri geliştirme modunda çalıştırmak | nodejs.org |
| VS Code + **C# Dev Kit** eklentisi | Kodu okumak, durdurma noktası koymak | code.visualstudio.com |

Docker Desktop'ı açın ve "Engine running" yazısını bekleyin. Terminalde (Windows'ta PowerShell) dört
programın da kurulduğunu kontrol edin:

```bash
git --version
dotnet --version
node --version
docker --version
```

## 2. Projeyi indirin

```bash
git clone https://github.com/ahmetkurtbm/bug-ticaret.git
cd bug-ticaret
```

VS Code'da bu klasörü açın (File → Open Folder). Sağ altta "önerilen eklentiler" çıkarsa kurun.

## 3. Sistemi çalıştırın ve tanıyın

```bash
docker compose --profile app up -d --build
```

İlk seferde 3-10 dakika sürer. Bitince:

| Adres | Ne |
| --- | --- |
| http://localhost:5173 | Müşteri sitesi |
| http://localhost:5174 | Satıcı paneli |
| http://localhost:5175 | Yönetim paneli |
| http://localhost:8025 | Giden e-postalar |

Hesaplar (şifre hepsinde `Tezgah123!`): `musteri@tezgah.test` → müşteri sitesi,
`satici@tezgah.test` → satıcı paneli, `admin@tezgah.test` → yönetim paneli. Tarayıcı sekmesinin adı
hangi sitede olduğunuzu söyler: **Tezgah**, **Tezgah Satıcı Paneli**, **Tezgah Yönetim**.

Satıcı paneli yalnızca o mağazanın ürünlerinin siparişlerini gösterir; demo satıcının ürünleri için
müşteri sitesinin ana sayfasındaki **Tezgah Demo Mağaza** çipine tıklayın.

Geliştirici ekranları:

| Adres | Ne |
| --- | --- |
| http://localhost:5080 | API'ye gelen son istekler: adres, durum kodu (200/401/500), süre, SQL sayısı, hata ayrıntısı |
| http://localhost:5090 | Sahte bankanın paneli: son ödemeler ve ödeme bildirimlerinin durumu |

Bir tur atın: müşteri olarak sipariş verin (ödeme sayfasında SMS kodu `123456`), e-postanın geldiğini
görün, satıcı olarak siparişi kargoya verin, yönetici olarak siparişlere bakın. Sistem normalde nasıl
çalışıyor bilmezseniz, bozulduğunda farkı göremezsiniz.

## 4. Bir bölüm seçin ve başlatın

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- liste
dotnet run --project backend/tools/Tezgah.Bolum -- baslat 8
```

`baslat`, kodu o bölümün sürümüne geçirir. Çalışan sistem hâlâ eski koddadır; yeniden derleyin:

```bash
docker compose --profile app up -d --build
```

> Kural: **kod değiştiğinde (bölüm başlatınca ya da siz düzenleyince) sistem yeniden derlenir.**

## 5. Hata kaydını okuyun ve hatayı kendi gözünüzle görün

`bolumler/08-eski-siparisler/KAYIT.md` dosyasını açın. Kayıt, şikâyeti ve hangi hesapla nasıl
tekrar edeceğinizi anlatır. Cevabı söylemez. Kayıttaki adımları uygulayın ve hatayı görün.
Göremediğiniz hatayı düzelttiğinizden emin olamazsınız.

## 6. Hata ayıklayın: üç soru, üç araç

> İpucu: API'nin "gelen istekler" ekranı (http://localhost:5080) hangi isteğin hangi kodla döndüğünü
> tek bakışta gösterir; F12 → Network'ün sunucu tarafındaki karşılığıdır.

Her hata için aynı sırayla ilerleyin:

| Soru | Araç | Ne arıyorsunuz |
| --- | --- | --- |
| **1. Sorun nerede: tarayıcıda mı, sunucuda mı?** | Tarayıcıda **F12 → Network** ve **Console** | Kırmızı (başarısız) bir istek var mı, kodu ne? Konsolda hata var mı? |
| **2. Sunucuda ne oldu?** | Servisin **logu** | Hata mesajı, hangi dosya, hangi satır |
| **3. O anda değerler neydi?** | **Durdurma noktası** (breakpoint) | Değişkenlerin gerçek değeri, kodun hangi yoldan gittiği |

### Logu okumak

Sistem container'da çalışırken:

```bash
docker compose logs api --tail 50              # API (monolit)
docker compose logs payments --tail 50         # Ödeme servisi
docker compose logs notifications --tail 50    # Bildirim servisi
```

Şirketlerde loglar bir panelde (Kibana, Grafana, Datadog…) toplanır; kendi bilgisayarınızda bu
komutlarla ya da servisin çalıştığı terminalde görürsünüz.

### Backend'de durdurma noktası koymak

Container içindeki koda durdurma noktası konmaz. Şirketlerde de yapılan şudur: altyapı container'da
kalır, incelenen servis IDE'den çalıştırılır.

1. Container'daki API'yi durdurun:
   ```bash
   docker compose stop api
   ```
2. VS Code'da **Run and Debug** (Ctrl+Shift+D) → listeden **"API (monolit)"** → **F5**.
   Alttaki terminalde `Now listening on: http://localhost:5080` görünce hazırdır. Bu terminal artık
   API'nin logudur.
3. İncelemek istediğiniz dosyayı açın (Ctrl+P, dosya adını yazın), satıra gidin (Ctrl+G), satır
   numarasının soluna tıklayın: kırmızı nokta.
4. Tarayıcıda hatayı tekrar edin. VS Code o satırda durur; soldaki **Variables** panelinde değerleri,
   **Call Stack**'te oraya nasıl gelindiğini görürsünüz. **F10** bir satır ilerler, **F11** fonksiyonun
   içine girer, **F5** devam eder.
5. Kodu düzeltin, kaydedin, **Shift+F5** ile durdurup **F5** ile yeniden başlatın, tekrar deneyin.
6. İşiniz bitince VS Code'da Shift+F5 yapın ve container'ı geri açın:
   ```bash
   docker compose --profile app up -d --build api
   ```

Ödeme ya da Bildirim servisini incelemek için aynısı: `docker compose stop payments` → VS Code'da
"Ödeme servisi". Not: API container dışındayken ödeme servisi ödeme sonucunu API'ye ulaştıramaz;
ödeme akışını inceleyecekseniz iki servisi birlikte IDE'den çalıştırın ("Tüm backend").

### Arayüzde (React) hata ayıklamak

1. İlgili container'ı durdurun ve arayüzü geliştirme modunda açın (ilk seferde `npm install`):
   ```bash
   docker compose stop storefront          # satıcı paneli için: seller, yönetim için: admin
   cd frontend
   npm install
   npm run dev:storefront                  # dev:seller, dev:admin
   ```
2. Tarayıcıda **F12 → Sources** (Chrome) ya da **Debugger** (Firefox) → Ctrl+P ile dosyayı açın,
   satıra tıklayarak durdurma noktası koyun. Kodda değişiklik yaptığınızda sayfa kendiliğinden yenilenir.
3. React Developer Tools eklentisi bileşenlerin o anki durumunu (state, props) gösterir.

## 7. Takılırsanız ipucu alın

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- ipucu 8      # 1. ipucu
dotnet run --project backend/tools/Tezgah.Bolum -- ipucu 8 2    # 2. ipucu
```

Önce 20-30 dakika kendiniz deneyin. İpuçları giderek açılır; üçüncüsü neredeyse cevaptır.

## 8. Düzeltmeyi doğrulayın

Kodu düzelttiyseniz ve sistemi yeniden derlediyseniz (ya da servis IDE'de çalışıyorsa):

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol
```

Kontrol aracı yalnızca "düzelmiş" ya da "henüz düzelmemiş" der; nerede olduğunu söylemez.

## 9. Çözümle karşılaştırın ve sonraki bölüme geçin

`COZUM.md` cevap anahtarıdır: bölümü bitirdikten sonra okuyun, kendi çözümünüzle karşılaştırın.
Değişikliklerinizi commit'leyip ana sürüme dönün:

```bash
git add -A && git commit -m "Bölüm 8 çözümü"
dotnet run --project backend/tools/Tezgah.Bolum -- bitir
docker compose --profile app up -d --build
```

## Kurallar

- Hata kaydı, ipuçları ve çözüm dışında cevaba giden kestirmeler kullanmayın: `git diff main`,
  `COZUM.md`'yi erken açmak, `docs/HATALAR.md`'de aramak, kontrol aracının kaynak kodunu okumak.
- `git log`, `git blame` ve commit mesajları serbesttir; gerçek ekipler de bunları kullanır.

## Sık sorunlar

| Belirti | Çözüm |
| --- | --- |
| `docker` komutları hata veriyor | Docker Desktop açık mı? "Engine running" yazmalı. |
| `port is already allocated` | Aynı portu başka bir şey kullanıyor; IDE'de çalışan servis ile container aynı anda açık olmasın. |
| Kodu değiştirdim ama hiçbir şey değişmedi | Sistemi yeniden derlediniz mi? `docker compose --profile app up -d --build` |
| Bilgisayarı yeniden başlattım, siteler açılmıyor | Docker Desktop'ı açın, sonra `docker compose --profile app up -d` |
| Her şeyi sıfırlamak istiyorum | `docker compose --profile app down -v` (veritabanı da silinir), sonra 3. adım |
