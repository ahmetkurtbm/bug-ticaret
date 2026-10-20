# TZ-4163 · Bildirim servisinin listesi anahtarsız açılıyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4163 |
| Seviye | ● (1/5) |
| Modül | Bildirim servisi |
| Bildiren | Derya Koçak, güvenlik ekibi |
| Öncelik | Acil (müşteri verisi açıkta) |
| Ortam | Yerel; altyapı Docker'da (`docker compose up -d`), Bildirim servisi `dotnet run --project backend/src/Tezgah.Notifications --launch-profile http` ile http://localhost:5095 |
| Durum | Açık |

## Ne oldu

Haftalık güvenlik taramasında Bildirim servisine hiçbir kimlik bilgisi vermeden istek attım. Servis bu uca anahtar istemeliydi; istemedi:

```bash
curl http://localhost:5095/api/notifications
```

Yanıt hemen `200 OK` geldi ve gövdede gönderilmiş e-postaların listesi vardı: alıcı e-posta adresleri (`recipientEmail`), konular, durumlar. `?email=musteri@tezgah.test` ekleyince o müşterinin bildirimleri süzülüp geldi.

Yanlış bir anahtar (`X-Api-Key: yanlis`) göndermek de sonucu değiştirmedi; yine 200. Yani anahtarın hiçbir etkisi yok gibi.

## Tekrar etme adımları

1. Altyapıyı başlatın: `docker compose up -d`
2. Bildirim servisini başlatın: `dotnet run --project backend/src/Tezgah.Notifications --launch-profile http`
3. Hiçbir başlık eklemeden: `curl -i http://localhost:5095/api/notifications`
4. Yanlış anahtarla: `curl -i http://localhost:5095/api/notifications -H "X-Api-Key: yanlis"`
5. Karşılaştırma için sağlık ucu: `curl -i http://localhost:5095/health`

## Beklenen

Anahtarsız ve yanlış anahtarlı istek `401 Unauthorized` alır (`Yetkisiz istek` başlıklı bir hata yanıtı). Doğru anahtarla `200` döner. `/health` anahtarsız `200` döner (izleme araçları kullanıyor).

## Gerçekleşen

Anahtarsız ve yanlış anahtarlı istek `200` ile müşteri e-posta adreslerini döndürüyor.

## Ek bilgi

- Geçen ayki taramada aynı komut `401` dönmüştü.
- Servis canlıya henüz çıkmadı; ama bu dal bu hâlde birleşirse canlıya gider.
- Erişim yalnızca servis ağından olsa bile müşteri e-postalarının anahtarsız okunabilmesi kabul edilemez.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 7
```

Kontrol üç şeyi sınar: `/health` anahtarsız açık, anahtarsız `/api/notifications` `401`, doğru anahtarla `200`.

---

## English

# TZ-4163 · Notification service list opens without a key

| Field | Value |
| --- | --- |
| Ticket | TZ-4163 |
| Level | ● (1/5) |
| Module | Notification service |
| Reporter | Derya Koçak, security team |
| Priority | Urgent (customer data exposed) |
| Environment | Local; infrastructure in Docker (`docker compose up -d`), Notification service via `dotnet run --project backend/src/Tezgah.Notifications --launch-profile http` at http://localhost:5095 |
| Status | Open |

### What happened

During the weekly security scan I sent a request to the Notification service without any credentials. The service should require a key for this endpoint; it did not:

```bash
curl http://localhost:5095/api/notifications
```

The response was an immediate `200 OK`, and the body contained the list of sent emails: recipient addresses (`recipientEmail`), subjects, statuses. Adding `?email=musteri@tezgah.test` filtered and returned that customer's notifications.

Sending a wrong key (`X-Api-Key: yanlis`) did not change the result either; still 200. So the key seems to have no effect at all.

### Steps to reproduce

1. Start the infrastructure: `docker compose up -d`
2. Start the Notification service: `dotnet run --project backend/src/Tezgah.Notifications --launch-profile http`
3. Without adding any header: `curl -i http://localhost:5095/api/notifications`
4. With a wrong key: `curl -i http://localhost:5095/api/notifications -H "X-Api-Key: yanlis"`
5. For comparison, the health endpoint: `curl -i http://localhost:5095/health`

### Expected

A request without a key or with a wrong key gets `401 Unauthorized` (an error response titled `Yetkisiz istek`, "Unauthorized request"). With the right key it returns `200`. `/health` returns `200` without a key (monitoring tools use it).

### Actual

Requests without a key or with a wrong key get `200` and customer email addresses.

### Additional information

- The same command returned `401` in last month's scan.
- The service is not in production yet, but if this branch is merged as is, it will be.
- Even if access is only possible from the service network, customer emails readable without a key are not acceptable.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 7
```

The check tests three things: `/health` is open without a key, `/api/notifications` without a key returns `401`, and with the right key it returns `200`.
