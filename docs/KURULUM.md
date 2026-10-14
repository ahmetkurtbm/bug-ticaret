# Kurulum

İki çalışma biçimi vardır:

1. **Her şey container'da.** En hızlı yol. Sistemi tanımak ve hatayı tekrar etmek için yeter.
2. **Yerel geliştirme.** Altyapı container'da, servisler ve arayüzler bilgisayarınızda. Kesme noktası
   koymak, kodu değiştirip hemen denemek için bu kullanılır. Çoğu bölüm bunu ister.

İkisini aynı anda çalıştırmayın; aynı portları kullanırlar.

## Gerekenler

| Araç | Sürüm | Not |
| --- | --- | --- |
| Docker Desktop (ya da Docker Engine + Compose) | 4.x / Compose v2 | Windows'ta WSL 2 arka ucu |
| .NET SDK | 10.0 | `dotnet --version` |
| Node.js | 22 LTS | `node --version` |
| Git | 2.40+ | bölüm dalları için |
| IDE | Visual Studio 2022 17.14+ ya da VS Code | [IDE rehberi](IDE.md) |

Donanım: en az 8 GB RAM (küçük veri profili). Orta profil için 16 GB RAM ve 10 GB boş disk önerilir.

## 1. Her şey container'da

```bash
docker compose --profile app up -d --build
docker compose --profile app ps        # hepsi "running" ya da "healthy" olmalı
docker compose logs -f seed            # örnek veri üretimini izleyin; "Bitti" yazınca hazır
```

Durdurmak: `docker compose --profile app down`. Veritabanını da silmek: `... down -v`.

## 2. Yerel geliştirme

### Altyapı

```bash
docker compose up -d                   # Postgres 5440, Redis 6390, RabbitMQ 5680/15680, Mailpit 1025/8025
```

### Veritabanı ve örnek veri

```bash
cd backend
dotnet tool restore
dotnet ef database update -p src/Tezgah.Infrastructure -s src/Tezgah.Api
dotnet run --project tools/Tezgah.DataGen -- --profile small --reset
```

Ödeme ve Bildirim servisleri kendi veritabanlarını (`tezgah_payments`, `tezgah_notifications`)
Development ortamında açılışta kendileri kurar.

**Veri profilleri:**

| Profil | Kullanıcı | Satıcı | Ürün | Sipariş | Süre (yaklaşık) |
| --- | --- | --- | --- | --- | --- |
| small | 5 bin | 60 | 7 bin | 50 bin | 10 sn |
| medium | 100 bin | 600 | 100 bin | 1 milyon | 3-5 dk |
| large | 1 milyon | 3 bin | 700 bin | 10 milyon | 30+ dk, 20 GB+ disk |

Performans bölümleri (ör. DB-01) en az `medium` ister: `--profile medium --reset`.

### Servisler

Üç ayrı terminalde (ya da IDE'den, bkz. [IDE rehberi](IDE.md)):

```bash
dotnet run --project src/Tezgah.Api --launch-profile http             # http://localhost:5080
dotnet run --project src/Tezgah.Payments --launch-profile http        # http://localhost:5090
dotnet run --project src/Tezgah.Notifications --launch-profile http   # http://localhost:5095
```

### Arayüzler

```bash
cd frontend
npm install
npm run dev:storefront    # http://localhost:5173
npm run dev:seller        # http://localhost:5174
npm run dev:admin         # http://localhost:5175
```

### Testler

```bash
cd backend && dotnet test
cd frontend && npm run build && npm run lint
```

## Sık karşılaşılan sorunlar

| Belirti | Neden ve çözüm |
| --- | --- |
| `Failed to connect to 127.0.0.1:5440` | Docker çalışmıyor ya da `docker compose up -d` yapılmadı. |
| `port is already allocated` | Aynı port başka bir süreçte. Container ve yerel çalışmayı aynı anda açmayın. |
| API açılırken `RabbitMq:HostName ayarlanmamış` | `ASPNETCORE_ENVIRONMENT=Development` değil; `--launch-profile http` ile çalıştırın. |
| Girişte "E-posta veya şifre hatalı" | Örnek veri üretilmedi. DataGen'i çalıştırın; demo şifre `Tezgah123!`. |
| Ödeme sayfası açılıyor ama sipariş "Ödeme bekleniyor"da kalıyor | Ödeme servisi webhook'u API'ye ulaştıramıyor; API açık mı, `Payments:WebhookUrl` doğru mu? |
| E-posta gelmiyor | Bildirim servisi açık mı? RabbitMQ arayüzünde `notifications.email` kuyruğuna bakın. |

> Bu tablodaki hataların bazıları bölüm olarak da var. Takılırsanız önce burayı okuyun; hâlâ
> çözülmüyorsa o büyük ihtimalle bölümün kendisidir.
