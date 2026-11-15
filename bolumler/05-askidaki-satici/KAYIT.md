# TZ-4150 · Askıya alınan satıcı neden giremediğini göremiyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4150 |
| Seviye | ● (1/5) |
| Nerede görülüyor | Satıcı paneli |
| Bildiren | Hülya Karaca, satıcı ilişkileri |
| Öncelik | Orta |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Kuralları ihlal ettiği için bir mağazayı askıya aldık. Mağaza sahibi bize yazıp "panelim bozuk, sürekli hata veriyor, oturumum mu düştü anlamıyorum" dedi. Ben de aynı hesapla girdim:

- Giriş yapabiliyorum, panel açılıyor ama üstte mağaza adı yerine boşluk var.
- Özet sayfasında içerik yerine bir hata kutusu çıkıyor; "Tekrar dene"ye kaç kez bassam da aynı.
- Ürünler ve Siparişler sayfalarında da aynı durum.
- Daha önce askıya alınan mağazalar giriş yapınca tam sayfa bir bilgi ekranı görüyordu ("Bu sayfaya erişiminiz yok" başlığı, altında sebep ve "Çıkış yap" düğmesi). Şimdi o ekran hiç gelmiyor.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. http://localhost:5175 adresinde `admin@tezgah.test` / `Tezgah123!` ile giriş yapın ve `satici@tezgah.test` hesabının mağazasını askıya alın.
3. http://localhost:5174 adresinde `satici@tezgah.test` / `Tezgah123!` ile giriş yapın.
4. Özet, Ürünler ve Siparişler sayfalarını gezin.

## Beklenen

Askıdaki mağazanın sahibi, mağazasının neden kapalı olduğunu açıkça anlatan bir ekran görür ve çıkış yapabilir.

## Gerçekleşen

Panel açılıyor ama hiçbir bölüm yüklenmiyor; her sayfada hata kutusu çıkıyor.

## Ek bilgi

- Geçen hafta askıya aldığımız mağazada tam sayfa açıklama ekranı çıkmıştı.
- Mağaza sahibi şifresini değiştirmedi; "Çıkış yap" deyip yeniden giriş yapmak da bir şey değiştirmiyor.
- Aktif mağazalarda panel normal çalışıyor.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 5
```

---

## English

# TZ-4150 · Suspended seller never sees why the panel fails

| Field | Value |
| --- | --- |
| Ticket | TZ-4150 |
| Level | ● (1/5) |
| Where it shows | Seller panel |
| Reporter | Hülya Karaca, seller relations |
| Priority | Medium |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

We suspended a store for breaking the rules. The store owner wrote to us: "My panel is broken, it keeps showing errors, I cannot tell whether my session dropped." I logged in with the same account:

- I can log in and the panel opens, but there is a blank space at the top where the store name should be.
- On the Overview page there is an error box instead of content; pressing "Tekrar dene" ("Try again") any number of times gives the same result.
- The Products and Orders pages behave the same way.
- Previously, suspended stores saw a full-page information screen after logging in (the heading "Bu sayfaya erişiminiz yok" / "You do not have access to this page", the reason below it, and a "Çıkış yap" / "Log out" button). That screen never appears now.

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. At http://localhost:5175 log in as `admin@tezgah.test` / `Tezgah123!` and suspend the store of the `satici@tezgah.test` account.
3. At http://localhost:5174 log in as `satici@tezgah.test` / `Tezgah123!`.
4. Browse the Overview, Products and Orders pages.

### Expected

The owner of the suspended store sees a screen that clearly explains why the store is closed and can log out.

### Actual

The panel opens, but no section loads; every page shows an error box.

### Additional information

- The store we suspended last week showed the full-page explanation screen.
- The store owner did not change the password; logging out and in again changes nothing.
- On active stores the panel works normally.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 5
```
