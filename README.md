# bug-ticaret

**Gerçek bir şirket kod tabanında hata ayıklamayı öğrenmek için açık kaynak bir pazar yeri.**
*An open-source marketplace codebase for learning how to debug like you would on the job.* ([English](#english))

Okulda yazdığınız kod küçüktür ve hepsini siz yazmışsınızdır. İşe başladığınız ilk hafta ise
başkasının yazdığı, yüz binlerce satır veri ve onlarca dosya arasında bir hata kaydı alırsınız:
"Bazı müşteriler giriş yapamıyor." Bu proje o haftayı yaşatır.

**Tezgah** kurgusal bir çok satıcılı pazar yeridir. Müşteri sitesi, satıcı paneli, yönetim paneli,
ayrı bir ödeme servisi, kuyruk üzerinden çalışan bir bildirim servisi ve gerçekten büyük bir
veritabanı vardır. `main` dalı her zaman doğru çalışan koddur. Her hata **ayrı bir dalda** durur:
dala geçersiniz, hata kaydını okursunuz, hatayı tekrar edersiniz, kaynağını bulursunuz, düzeltirsiniz
ve kontrol aracıyla doğrularsınız.

## İçindekiler

| Bölüm | Açıklama |
| --- | --- |
| [Kurulum](docs/KURULUM.md) | Docker, .NET 10, Node 22; ilk çalıştırma ve örnek veri |
| [Mimari](docs/MIMARI.md) | Monolit, Ödeme ve Bildirim servisleri, olay akışı, veri modeli |
| [IDE rehberi](docs/IDE.md) | Visual Studio ve VS Code ile çalıştırma, kesme noktası, `.http` dosyaları |
| [Bölümler](bolumler/README.md) | Hata senaryoları, nasıl başlanır, nasıl kontrol edilir |
| [Hata kataloğu](HATALAR.md) | Şirketlerde sık görülen 108 hata: belirti, kök neden, nasıl bulunur |
| [Plan](PLAN.md) | Proje kararları ve yol haritası |

## Hızlı başlangıç

```bash
git clone https://github.com/ahmetkurtbm/bug-ticaret.git && cd bug-ticaret
docker compose --profile app up -d --build
```

İlk açılış birkaç dakika sürer (internet hızına bağlı olarak 3-10 dk) (imajlar derlenir, şema kurulur, ~900 bin satırlık örnek veri üretilir).
Sonra:

| Adres | Ne |
| --- | --- |
| http://localhost:5173 | Müşteri sitesi |
| http://localhost:5174 | Satıcı paneli |
| http://localhost:5175 | Yönetim paneli |
| http://localhost:5080/health | API |
| http://localhost:8025 | Mailpit (giden e-postalar) |
| http://localhost:15680 | RabbitMQ yönetim arayüzü (tezgah / tezgah) |

Demo hesaplar (şifre hepsinde `Tezgah123!`): `musteri@tezgah.test`, `satici@tezgah.test`,
`admin@tezgah.test`. Ödeme sayfasında test SMS kodu `123456`; kuruşu `,13` ile biten tutarlar
bankadan reddedilir.

Kodu değiştirip hata ayıklayıcıyla çalışmak için servisleri yerelde çalıştırın:
[Kurulum → Yerel geliştirme](docs/KURULUM.md#yerel-geliştirme).

## Bölüm çözmek

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- liste
dotnet run --project backend/tools/Tezgah.Bolum -- baslat 8
# ... hata kaydını okuyun, hatayı bulun, düzeltin ...
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 8
```

Ayrıntılar: [bolumler/README.md](bolumler/README.md).

## Teknoloji

- **Backend:** .NET 10, ASP.NET Core Web API, EF Core 10 (PostgreSQL), Dapper (eski rapor modülü),
  RabbitMQ, MailKit
- **Frontend:** React 19, TypeScript, Vite, TanStack Query, React Router, Tailwind CSS
- **Altyapı:** Docker Compose, PostgreSQL 17, RabbitMQ 4, Redis, Mailpit, Nginx

## Lisans

[MIT](LICENSE). Tezgah, ürünler, satıcılar ve kişiler kurgusaldır; örnek veri rastgele üretilir.

---

## English

**bug-ticaret** is an open-source, deliberately realistic multi-seller marketplace ("Tezgah", a
fictional company) built to teach new graduates how debugging works on a real team: large unfamiliar
codebase, several services, a big database, logs, and a bug report that says only what the user saw.

- `main` always contains working code. Each bug lives on its own branch (`bolum/NN-...`).
- Start a chapter, read the ticket (Turkish and English), reproduce, find the root cause, fix it,
  and run the checker: `dotnet run --project backend/tools/Tezgah.Bolum -- kontrol NN`.
- Code is in English; explanations, tickets and hints are in Turkish and English.

Quick start: `docker compose --profile app up -d --build`, then open http://localhost:5173.
Demo users: `musteri@tezgah.test`, `satici@tezgah.test`, `admin@tezgah.test` (password `Tezgah123!`).
Setup, architecture and IDE guides are in [`docs/`](docs/) (Turkish); chapter tickets include English.
