# IDE rehberi

Bölümlerin çoğu "kesme noktası koy, isteği gönder, değişkene bak" ile çözülür. Bu sayfa iki IDE'de
bunun nasıl yapılacağını anlatır. Önce [Kurulum → Yerel geliştirme](KURULUM.md#yerel-geliştirme)
adımlarıyla altyapıyı ve örnek veriyi hazırlayın.

## Visual Studio 2022 (Windows)

1. `backend/Tezgah.slnx` dosyasını açın (17.14 ve sonrası `.slnx` açar).
2. **Birden fazla başlangıç projesi:** Çözüme sağ tık → *Configure Startup Projects…* →
   *Multiple startup projects*: `Tezgah.Api`, `Tezgah.Payments`, `Tezgah.Notifications` için
   *Start*. Profil olarak her birinde `http` seçili olsun.
3. **F5** ile üçü birlikte hata ayıklayıcıyla açılır.
4. **Kesme noktası:** Satır numarasının soluna tıklayın. Sağ tık → *Conditions* ile koşullu kesme
   (ör. `orderNumber == "TZ-2026-0050011"`), *Actions* ile durmadan log yazdırma.
5. **İstek göndermek:** `src/Tezgah.Api/Tezgah.Api.http` dosyasını açın, isteğin üstündeki
   *Send request* bağlantısına tıklayın. Önce "Giriş" isteğini gönderin; token sonrakilere geçer.
6. **Faydalı pencereler:** *Locals*, *Watch*, *Call Stack* (Debug → Windows),
   *Exception Settings* (Ctrl+Alt+E): "Common Language Runtime Exceptions" işaretlenirse
   yakalanan istisnada da durur — "nerede patladı?" sorusunun en hızlı cevabı.
7. **Veritabanı:** *SQL Server Object Explorer* Postgres'e bağlanmaz; DBeaver, DataGrip ya da
   `docker exec -it tezgah-postgres-1 psql -U tezgah` kullanın.

Frontend için Visual Studio yerine VS Code ya da tarayıcı geliştirici araçlarını kullanın.

## VS Code (Windows, macOS, Linux)

1. Depo kökünü açın. Önerilen eklentileri kurun (sağ altta çıkan bildirim ya da
   *Extensions → Recommended*): C# Dev Kit, Container Tools, Oxc, Tailwind CSS, REST Client.
2. **Çalıştır ve hata ayıkla** (Ctrl+Shift+D) listesinde hazır yapılandırmalar var
   (`.vscode/launch.json`):
   - **Tüm backend** — API, Ödeme ve Bildirim servislerini birlikte açar.
   - **API (monolit)**, **Ödeme servisi**, **Bildirim servisi** — tek tek.
   - **Veri üretici (small)** — örnek veriyi sıfırdan üretir.
   - **Müşteri sitesi / Satıcı paneli / Yönetim paneli (Chrome)** — tarayıcıyı VS Code'a bağlar;
     React kodunda kesme noktası koyabilirsiniz. Önce ilgili `npm run dev:*` görevini başlatın.
3. **Görevler** (Ctrl+Shift+P → *Tasks: Run Task*): `altyapı: docker compose up`,
   `veritabanı: migration uygula`, `test: backend`, `frontend: müşteri sitesi` vb.
4. **İstek göndermek:** `backend/src/Tezgah.Api/Tezgah.Api.http` dosyasında her isteğin üstünde
   *Send Request* çıkar (REST Client eklentisi).
5. **İstisnada durmak:** Hata ayıklama panelinde *Breakpoints* bölümünde "All Exceptions" /
   "User-Unhandled Exceptions" kutuları.

## Tarayıcı geliştirici araçları (her iki IDE ile)

- **Network** sekmesi: isteğin adresi, durum kodu, gönderilen gövde ve dönen ProblemDetails.
  400 yanıtında `errors` alanı hangi alanın neden reddedildiğini söyler.
- **Console**: React uyarıları (hook sırası, key, kontrolsüz input) burada görünür.
- **Application → Local Storage**: oturum (`tezgah.session`, `tezgah.seller.session`,
  `tezgah.admin.session`) ve misafir sepet kimliği (`tezgah.cartId`).
- React Developer Tools ve TanStack Query Devtools (müşteri sitesinde sağ altta) bileşen durumunu
  ve önbelleği gösterir.

## Loglar

- Yerel çalışmada her servis kendi terminaline yazar. Sipariş ve ödeme akışı Türkçe log mesajları
  üretir (ör. `Sipariş oluşturuldu TZ-2026-...`); sipariş numarasıyla arayın.
- Container'da: `docker compose logs -f api payments notifications`.
- RabbitMQ: http://localhost:15680 → *Queues* → `notifications.email` (bekleyen, işlenen, DLQ).
- E-postalar: http://localhost:8025.
