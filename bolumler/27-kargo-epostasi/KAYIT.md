# TZ-4702 · "Kargoya verildi" e-postası müşteriye gitmiyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4702 |
| Seviye | ●● (2/5) |
| Nerede görülüyor | Satıcı paneli · Siparişler · E-postalar |
| Bildiren | Selma Günay, müşteri hizmetleri |
| Öncelik | Yüksek |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Müşteriler "siparişiniz kargoya verildi" e-postasını almadıklarını söylüyor ve kargonun ne zaman çıktığını bize soruyor; bu hafta bu konuda çağrı sayısı belirgin biçimde arttı. Aynı müşteriler siparişi verdikleri anda gelen "ödemeniz alındı" e-postasını sorunsuz almışlar.

Ben de kendim denedim: bir satıcı hesabıyla ödenmiş bir siparişi kargoya verdim. Satıcı panelinde her şey normal görünüyor ama e-posta kutusuna (geliştirme ortamında Mailpit) kargo e-postası düşmüyor. Geçen hafta aynı adımlarda e-posta geliyordu.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. http://localhost:5174 adresinde `satici@tezgah.test` / `Tezgah123!` ile giriş yapın.
3. Siparişler sayfasında durumu "ödendi" olan bir siparişi seçip kargoya verin (kargo firması Aras Kargo, takip numarası örneğin `ABCD12345678`).
4. http://localhost:8025 adresindeki Mailpit gelen kutusuna bakın; birkaç dakika bekleyin.

## Beklenen

Siparişi veren müşteriye "siparişiniz kargoya verildi" e-postası (kargo firması ve takip numarasıyla) gelir; Mailpit'te görünür.

## Gerçekleşen

Mailpit'te kargo e-postası yok. Beklemek bir şey değiştirmiyor.

## Ek bilgi

- Sipariş durumu satıcı panelinde "kargoda"ya geçiyor; panelde hata görünmüyor.
- Aynı siparişin "ödendi" e-postası Mailpit'te duruyor, yani e-posta sistemi genel olarak çalışıyor.
- Geçen hafta aynı akışta kargo e-postası geliyordu.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 27
```

---

## English

# TZ-4702 · The "shipped" email never reaches the customer

| Field | Value |
| --- | --- |
| Ticket | TZ-4702 |
| Level | ●● (2/5) |
| Where it shows | Seller panel · Orders · Emails |
| Reporter | Selma Günay, customer service |
| Priority | High |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

Customers say they do not receive the "your order has been shipped" email and ask us when the parcel left; the number of calls on this has clearly increased this week. The same customers received the "your payment was received" email that arrives the moment they place the order without any problem.

I tried it myself: with a seller account I shipped an order that had been paid. Everything looks normal in the seller panel, but no shipping email arrives in the mailbox (Mailpit in the development environment). Last week the email arrived with the same steps.

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Open http://localhost:5174 and sign in with `satici@tezgah.test` / `Tezgah123!`.
3. On the Orders page pick an order whose status is "ödendi" ("paid") and ship it (carrier Aras Kargo, tracking number for example `ABCD12345678`).
4. Look at the Mailpit inbox at http://localhost:8025; wait a few minutes.

### Expected

The customer who placed the order receives the "your order has been shipped" email (with the carrier and tracking number); it shows up in Mailpit.

### Actual

There is no shipping email in Mailpit. Waiting changes nothing.

### Additional information

- The order status changes to "kargoda" ("shipped") in the seller panel; no error is visible in the panel.
- The "paid" email of the same order is in Mailpit, so the email system works in general.
- Last week the shipping email arrived in the same flow.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 27
```
