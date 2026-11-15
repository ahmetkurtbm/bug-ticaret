# TZ-4163 · Bildirim API'si anahtarsız açılıyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4163 |
| Seviye | ● (1/5) |
| Nerede görülüyor | Bildirim servisi |
| Bildiren | Derya Koçak, güvenlik ekibi |
| Öncelik | Acil (müşteri verisi açıkta) |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Haftalık güvenlik taramasında Bildirim servisine hiçbir kimlik bilgisi vermeden istek attım. Servis bu adrese anahtar istemeliydi; istemedi:

```bash
curl http://localhost:5095/api/notifications
```

Yanıt hemen `200 OK` geldi ve gövdede gönderilmiş e-postaların listesi vardı: alıcı e-posta adresleri, konular, durumlar. Adrese `?email=musteri@tezgah.test` ekleyince o müşterinin bildirimleri süzülüp geldi.

Yanlış bir anahtar (`X-Api-Key: yanlis`) göndermek de sonucu değiştirmedi; yine 200.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. Hiçbir başlık eklemeden: `curl -i http://localhost:5095/api/notifications`
3. Yanlış anahtarla: `curl -i http://localhost:5095/api/notifications -H "X-Api-Key: yanlis"`
4. Karşılaştırma için sağlık adresi: `curl -i http://localhost:5095/health`

## Beklenen

Anahtarsız ve yanlış anahtarlı istek `401 Unauthorized` alır. Doğru anahtarla `200` döner. `/health` anahtarsız `200` döner (izleme araçları kullanıyor).

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

---

## English

# TZ-4163 · Notifications API works without a key

| Field | Value |
| --- | --- |
| Ticket | TZ-4163 |
| Level | ● (1/5) |
| Where it shows | Notification service |
| Reporter | Derya Koçak, security team |
| Priority | Urgent (customer data exposed) |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

During the weekly security scan I sent a request to the Notification service without any credentials. The service should require a key for this address; it did not:

```bash
curl http://localhost:5095/api/notifications
```

The response was an immediate `200 OK`, and the body contained the list of sent emails: recipient addresses, subjects, statuses. Adding `?email=musteri@tezgah.test` to the address filtered and returned that customer's notifications.

Sending a wrong key (`X-Api-Key: yanlis`) did not change the result either; still 200.

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Without adding any header: `curl -i http://localhost:5095/api/notifications`
3. With a wrong key: `curl -i http://localhost:5095/api/notifications -H "X-Api-Key: yanlis"`
4. For comparison, the health address: `curl -i http://localhost:5095/health`

### Expected

A request without a key or with a wrong key gets `401 Unauthorized`. With the right key it returns `200`. `/health` returns `200` without a key (monitoring tools use it).

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
