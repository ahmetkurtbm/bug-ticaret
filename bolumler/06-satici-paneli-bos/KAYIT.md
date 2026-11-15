# TZ-4155 · Satıcı paneli hiçbir veriyi yüklemiyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4155 |
| Seviye | ● (1/5) |
| Nerede görülüyor | Satıcı paneli |
| Bildiren | Tuna Erdem, QA mühendisi |
| Öncelik | Yüksek |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Satıcı panelinde giriş sayfasında `satici@tezgah.test` ile giriş yapmaya çalışıyorum. Düğme "Giriş yapılıyor…" yazıp geri dönüyor ve formun üstünde kırmızı kutuda şu çıkıyor:

> Giriş yapılamadı.

Şifre doğru. Dünden açık kalan bir sekmede (oturum zaten açıktı) panel açılıyor ama hiçbir bölüm yüklenmiyor; her yerde şu yazıyor:

> Bağlantı kurulamadı. İnternetinizi kontrol edip tekrar deneyin.

Tarayıcı konsolunda (F12 → Console) kırmızı hata satırları var.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. http://localhost:5174/giris adresini açın, F12 → Console sekmesini açık tutun.
3. `satici@tezgah.test` / `Tezgah123!` ile giriş yapmayı deneyin.

## Beklenen

Giriş yapılır ve satıcı paneli özet verilerle açılır.

## Gerçekleşen

Giriş yapılamıyor; "Giriş yapılamadı." yazıyor.

## Ek bilgi

- Müşteri sitesi (5173) ve yönetim paneli (5175) aynı sistemle sorunsuz çalışıyor; yalnızca satıcı paneli (5174) etkileniyor.
- Dün bu sürümden önce panel çalışıyordu.

## Bitirdiğinizde

Düzeltmeden sonra API'yi yeniden başlatıp:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 6
```

---

## English

# TZ-4155 · Seller panel loads no data

| Field | Value |
| --- | --- |
| Ticket | TZ-4155 |
| Level | ● (1/5) |
| Where it shows | Seller panel |
| Reporter | Tuna Erdem, QA engineer |
| Priority | High |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

On the seller panel login page I try to log in with `satici@tezgah.test`. The button says "Giriş yapılıyor…" ("Logging in…") and goes back, and a red box above the form says:

> Giriş yapılamadı. ("Login failed.")

The password is correct. In a tab left open since yesterday (the session was already active) the panel opens but no section loads; everywhere it says:

> Bağlantı kurulamadı. İnternetinizi kontrol edip tekrar deneyin. ("Could not connect. Check your internet and try again.")

The browser console (F12 → Console) shows red error lines.

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Open http://localhost:5174/giris with F12 → Console open.
3. Try to log in with `satici@tezgah.test` / `Tezgah123!`.

### Expected

Login succeeds and the seller panel opens with the summary data.

### Actual

Login fails with "Giriş yapılamadı.".

### Additional information

- The customer site (5173) and the admin panel (5175) work fine with the same system; only the seller panel (5174) is affected.
- The panel worked yesterday, before this build.

### When you are done

After your fix, restart the API and run:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 6
```
