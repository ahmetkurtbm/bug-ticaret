# TZ-4150 · Askıya alınan satıcının paneli bozuk, sebep anlaşılmıyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4150 |
| Seviye | ● (1/5) |
| Modül | Satıcı paneli · Yetki |
| Bildiren | Hülya Karaca, satıcı ilişkileri |
| Öncelik | Orta |
| Ortam | Yerel geliştirme; API http://localhost:5080, satıcı paneli http://localhost:5174, yönetim paneli http://localhost:5175, Chrome |
| Durum | Açık |

## Ne oldu

Kuralları ihlal ettiği için bir mağazayı askıya aldık. Mağaza sahibi bize yazıp "panelim bozuk, sürekli hata veriyor, oturumum mu düştü anlamıyorum" dedi. Ben de aynı hesapla girdim:

- Giriş yapabiliyorum, panel açılıyor ama üstte mağaza adı yerine boşluk var.
- Özet sayfasında içerik yerine bir hata kutusu çıkıyor; "Tekrar dene"ye kaç kez bassam da aynı.
- Ürünler ve Siparişler sayfalarında da aynı durum.
- Daha önce askıya alınan mağazalar giriş yapınca tam sayfa bir bilgi ekranı görüyordu ("Bu sayfaya erişiminiz yok" başlığı, altında sebep ve "Çıkış yap" düğmesi). Şimdi o ekran hiç gelmiyor.

Tarayıcı geliştirici araçlarında Network sekmesinde panelin istekleri kırmızı. Her başarısız isteğin arkasından `refresh` adında ikinci bir istek ve aynı isteğin tekrarı görünüyor.

## Tekrar etme adımları

1. API çalışsın: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
2. Satıcı paneli ve yönetim paneli çalışsın: `cd frontend && npm run dev:seller` ve `npm run dev:admin`
3. http://localhost:5175 adresinde `admin@tezgah.test` / `Tezgah123!` ile giriş yapın ve `satici@tezgah.test` hesabının mağazasını askıya alın.
4. http://localhost:5174 adresinde `satici@tezgah.test` / `Tezgah123!` ile giriş yapın.
5. Özet, Ürünler ve Siparişler sayfalarını gezin. F12 → Network sekmesini açık tutun.

## Beklenen

Askıdaki mağazanın sahibi, mağazasının neden kapalı olduğunu açıkça anlatan bir ekran görür ve çıkış yapabilir. Panel her tıklamada hata üretmez.

## Gerçekleşen

Panel kabuğu açılıyor ama hiçbir bölüm yüklenmiyor; her istek başarısız oluyor ve istemci her seferinde yeniden oturum yenilemeyi deniyor.

## Ek bilgi

- Geçen hafta askıya aldığımız mağazada tam sayfa açıklama ekranı çıkmıştı.
- Mağaza sahibi şifresini değiştirmedi; "Çıkış yap" deyip yeniden giriş yapmak da bir şey değiştirmiyor.
- Aktif mağazalarda panel normal çalışıyor.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 5
```

Kontrol kendi satıcı hesabını açar, onaylar, askıya alır ve panelin cevabını sınar; API'nin çalışması yeterli.

---

## English

# TZ-4150 · Suspended seller's panel is broken and the reason is unclear

| Field | Value |
| --- | --- |
| Ticket | TZ-4150 |
| Level | ● (1/5) |
| Module | Seller panel · Authorization |
| Reporter | Hülya Karaca, seller relations |
| Priority | Medium |
| Environment | Local development; API http://localhost:5080, seller panel http://localhost:5174, admin panel http://localhost:5175, Chrome |
| Status | Open |

### What happened

We suspended a store for breaking the rules. The store owner wrote to us: "My panel is broken, it keeps showing errors, I cannot tell whether my session dropped." I logged in with the same account:

- I can log in and the panel opens, but there is a blank space at the top where the store name should be.
- On the Overview page there is an error box instead of content; pressing "Tekrar dene" ("Try again") any number of times gives the same result.
- The Products and Orders pages behave the same way.
- Previously, suspended stores saw a full-page information screen after logging in (the heading "Bu sayfaya erişiminiz yok" / "You do not have access to this page", the reason below it, and a "Çıkış yap" / "Log out" button). That screen never appears now.

In the browser developer tools Network tab, the panel's requests are red. After each failed request there is a second request named `refresh`, and then the same request repeated.

### Steps to reproduce

1. Start the API: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
2. Start the seller and admin panels: `cd frontend && npm run dev:seller` and `npm run dev:admin`
3. At http://localhost:5175 log in as `admin@tezgah.test` / `Tezgah123!` and suspend the store of the `satici@tezgah.test` account.
4. At http://localhost:5174 log in as `satici@tezgah.test` / `Tezgah123!`.
5. Browse the Overview, Products and Orders pages with F12 → Network open.

### Expected

The owner of the suspended store sees a screen that clearly explains why the store is closed and can log out. The panel does not fail on every click.

### Actual

The panel shell opens, but no section loads; every request fails and the client tries to renew the session each time.

### Additional information

- The store we suspended last week showed the full-page explanation screen.
- The store owner did not change the password; logging out and in again changes nothing.
- On active stores the panel works normally.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 5
```

The check creates its own seller account, approves it, suspends it and tests the panel's response; the API running is enough.
