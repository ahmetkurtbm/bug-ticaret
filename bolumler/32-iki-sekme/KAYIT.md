# TZ-4783 · İki sekme açıkken bir anda oturum kapanıyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4783 |
| Seviye | ●●● (3/5) |
| Nerede görülüyor | Müşteri sitesi · Giriş |
| Bildiren | Tolga Eren, müşteri hizmetleri |
| Öncelik | Yüksek |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Birkaç müşteri aynı şikâyeti iletti: mağazayı iki sekmede açık tutuyorlarmış (birinde ürünlere bakıyor, diğerinde sepetlerini düzenliyorlarmış). Bir süre sonra, hiçbir şey yapmadan, iki sekmede de oturumları kapanmış ve giriş sayfasına atılmışlar. Sepetteki işlemlerini bu yüzden yarıda bırakanlar olmuş.

Tek sekmeyle kullananlarda böyle bir şikâyet yok. Ben de kendi hesabımla denedim ve aynısını gördüm; ama her zaman değil, yalnızca bir süre geçtikten sonra oluyor.

## Tekrar etme adımları

1. Bekleme süresini kısaltmak için API'yi erişim süresi 1 dakika olacak şekilde başlatın. Ayar `backend/src/Tezgah.Api/appsettings.json` içinde `Jwt` bölümünün altındaki `AccessTokenMinutes`'tır (dakika cinsinden tam sayı, varsayılan 15); ortam değişkeni olarak `Jwt__AccessTokenMinutes=1` verilir.
   - Her şey container'da çalışıyorsa: `docker-compose.yml` içinde `api` servisinin `environment` bölümüne geçici olarak `Jwt__AccessTokenMinutes: "1"` satırını ekleyip `docker compose --profile app up -d --build` çalıştırın (bu satırı depoya göndermeyin).
   - API'yi yerelde çalıştırıyorsanız (PowerShell): `$env:Jwt__AccessTokenMinutes = "1"` yazıp aynı pencerede `dotnet run --project backend/src/Tezgah.Api` çalıştırın.
2. http://localhost:5173 adresinde iki ayrı sekme açın ve ikisinde de `musteri@tezgah.test` / `Tezgah123!` ile giriş yapın (ikinci sekmede giriş sayfası gelmeyebilir; zaten giriş yapılmışsa sorun değil). İkisinde de biraz gezinin (ürün sayfaları, Hesabım).
3. Erişim süresinin dolması için yaklaşık iki dakika bekleyin.
4. Birinci sekmede başka bir sayfaya geçin ya da sayfayı yenileyin; oturum açık kalır.
5. Hemen ardından ikinci sekmede, sayfayı yenilemeden, menüden başka bir sayfaya geçin (ör. Hesabım → Siparişlerim).

## Beklenen

İkinci sekmede de oturum açık kalır; müşteri kaldığı yerden devam eder.

## Gerçekleşen

İkinci sekme giriş sayfasına atılıyor; birinci sekmede bir sonraki işlemde o da giriş sayfasına düşüyor. Müşteri iki sekmede de oturumunu kaybediyor.

## Ek bilgi

- Yalnızca iki sekme açıkken oluyor; tek sekmede süre dolsa bile oturum sorunsuz devam ediyor.
- Hata erişim süresi dolduktan sonra, ikinci sekmenin ilk işleminde ortaya çıkıyor.
- Giriş bilgilerini yeniden yazınca her şey yine çalışıyor; hesap kilitlenmiyor.
- Geçen ay iki sekmeyle gün boyu çalışan müşteriler vardı; şikâyet son zamanlarda başladı.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 32
```

---

## English

# TZ-4783 · Session drops when two tabs are open

| Field | Value |
| --- | --- |
| Ticket | TZ-4783 |
| Level | ●●● (3/5) |
| Where it shows | Storefront · Login |
| Reporter | Tolga Eren, customer service |
| Priority | High |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

Several customers reported the same complaint: they keep the store open in two tabs (browsing products in one, editing their cart in the other). After a while, without doing anything, they are logged out in both tabs and thrown to the login page. Some have abandoned their cart actions halfway because of this.

There is no such complaint from people who use a single tab. I tried it with my own account and saw the same; but not every time, only after some time has passed.

### Steps to reproduce

1. To shorten the waiting time, start the API with an access time of 1 minute. The setting is `AccessTokenMinutes` under the `Jwt` section of `backend/src/Tezgah.Api/appsettings.json` (an integer in minutes, default 15); as an environment variable it is given as `Jwt__AccessTokenMinutes=1`.
   - If everything runs in containers: temporarily add the line `Jwt__AccessTokenMinutes: "1"` to the `environment` section of the `api` service in `docker-compose.yml` and run `docker compose --profile app up -d --build` (do not push this line to the repository).
   - If you run the API locally (PowerShell): type `$env:Jwt__AccessTokenMinutes = "1"` and run `dotnet run --project backend/src/Tezgah.Api` in the same window.
2. At http://localhost:5173 open two separate tabs and log in as `musteri@tezgah.test` / `Tezgah123!` in both (the login page may not show in the second tab; if you are already logged in that is fine). Browse a little in both (product pages, My Account).
3. Wait about two minutes for the access time to run out.
4. In the first tab go to another page or refresh the page; the session stays open.
5. Right after that, in the second tab, without refreshing, go to another page from the menu (e.g. My Account → My Orders).

### Expected

The session stays open in the second tab too; the customer continues where they left off.

### Actual

The second tab is thrown to the login page; in the first tab, at its next action, it drops to the login page too. The customer loses the session in both tabs.

### Additional information

- It happens only when two tabs are open; in a single tab the session continues without problems even when the time runs out.
- The error shows up at the first action of the second tab after the access time has run out.
- Typing the login details again makes everything work; the account is not locked.
- Last month there were customers who worked all day with two tabs; the complaints started recently.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 32
```
