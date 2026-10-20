# TZ-4155 · Satıcı paneli hiçbir veriyi yüklemiyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4155 |
| Seviye | ● (1/5) |
| Modül | API · CORS |
| Bildiren | Tuna Erdem, QA mühendisi |
| Öncelik | Yüksek |
| Ortam | Yerel geliştirme; API http://localhost:5080 (`--launch-profile http`), satıcı paneli http://localhost:5174 (`npm run dev:seller`), Chrome |
| Durum | Açık |

## Ne oldu

Satıcı panelinde giriş sayfasında `satici@tezgah.test` ile giriş yapmaya çalışıyorum. Düğme "Giriş yapılıyor…" yazıp geri dönüyor ve formun üstünde kırmızı kutuda şu çıkıyor:

> Giriş yapılamadı.

Şifre doğru. Dünden açık kalan bir sekmede (oturum zaten açıktı) panel açılıyor ama hiçbir bölüm yüklenmiyor; her yerde şu yazıyor:

> Bağlantı kurulamadı. İnternetinizi kontrol edip tekrar deneyin.

Tarayıcı konsolunda kırmızı olarak şu satır var:

> Access to fetch at 'http://localhost:5080/api/auth/login' from origin 'http://localhost:5174' has been blocked by CORS policy: Response to preflight request doesn't pass access control check: No 'Access-Control-Allow-Origin' header is present on the requested resource.

## Tekrar etme adımları

1. API'yi başlatın: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
2. `cd frontend && npm run dev:seller`
3. http://localhost:5174/giris adresini açın, F12 → Console ve Network sekmelerini açık tutun.
4. `satici@tezgah.test` / `Tezgah123!` ile giriş yapmayı deneyin.

## Beklenen

Giriş yapılır ve satıcı paneli özet verilerle açılır.

## Gerçekleşen

Giriş yapılamıyor. Konsolda CORS hatası var.

## Ek bilgi

- Müşteri sitesi (5173) ve yönetim paneli (5175) aynı API'ye karşı sorunsuz çalışıyor.
- Tarayıcıdan http://localhost:5080/health açılıyor, `curl` ile API'ye yaptığım istekler de yanıt veriyor.
- API'nin terminalinde hata ya da uyarı satırı yok.
- Dün bu sürümden önce panel çalışıyordu.

## Bitirdiğinizde

API'yi düzeltmeden sonra yeniden başlatıp:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 6
```

Kontrol üç uygulamanın (5173, 5174, 5175) adresi için tarayıcının yapacağı ön kontrol isteğini (preflight) API'ye gönderir.

---

## English

# TZ-4155 · Seller panel loads no data

| Field | Value |
| --- | --- |
| Ticket | TZ-4155 |
| Level | ● (1/5) |
| Module | API · CORS |
| Reporter | Tuna Erdem, QA engineer |
| Priority | High |
| Environment | Local development; API http://localhost:5080 (`--launch-profile http`), seller panel http://localhost:5174 (`npm run dev:seller`), Chrome |
| Status | Open |

### What happened

On the seller panel login page I try to log in with `satici@tezgah.test`. The button says "Giriş yapılıyor…" ("Logging in…") and goes back, and a red box above the form says:

> Giriş yapılamadı. ("Login failed.")

The password is correct. In a tab left open since yesterday (the session was already active) the panel opens but no section loads; everywhere it says:

> Bağlantı kurulamadı. İnternetinizi kontrol edip tekrar deneyin. ("Could not connect. Check your internet and try again.")

The browser console shows this in red:

> Access to fetch at 'http://localhost:5080/api/auth/login' from origin 'http://localhost:5174' has been blocked by CORS policy: Response to preflight request doesn't pass access control check: No 'Access-Control-Allow-Origin' header is present on the requested resource.

### Steps to reproduce

1. Start the API: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
2. `cd frontend && npm run dev:seller`
3. Open http://localhost:5174/giris with F12 → Console and Network open.
4. Try to log in with `satici@tezgah.test` / `Tezgah123!`.

### Expected

Login succeeds and the seller panel opens with the summary data.

### Actual

Login fails. The console shows a CORS error.

### Additional information

- The customer site (5173) and the admin panel (5175) work fine against the same API.
- http://localhost:5080/health opens in the browser, and requests I make with `curl` get answers too.
- The API terminal shows no error or warning lines.
- The panel worked yesterday, before this build.

### When you are done

After fixing, restart the API and run:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 6
```

The check sends the API the pre-check request (preflight) a browser would send for the addresses of all three apps (5173, 5174, 5175).
