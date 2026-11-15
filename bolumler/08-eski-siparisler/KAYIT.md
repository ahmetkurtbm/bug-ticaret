# TZ-4170 · Bazı eski siparişlerin detayı açılmıyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4170 |
| Seviye | ● (1/5) |
| Nerede görülüyor | Müşteri sitesi · Siparişlerim |
| Bildiren | Cem Başaran, müşteri hizmetleri |
| Öncelik | Yüksek |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Bir müşteri (goksin.ozgorkey@mail.test) aradı: "En eski siparişlerimden birinin, yaklaşık iki yıl öncekinin detayı açılmıyor. Fatura bilgisi lazım, Siparişlerim'de görüyorum ama üstüne tıklayınca açılmıyor." Ben de aynı hesapla baktım. Siparişlerim listesi açılıyor, siparişler orada duruyor. Ama o eski siparişe tıklayınca detay sayfasında yalnızca bir hata kutusu çıkıyor:

> An error occurred while processing your request.

"Tekrar dene" düğmesi işe yaramıyor. Aynı müşterinin diğer siparişlerinin detayı sorunsuz açılıyor. Yeni siparişlerde hiç sorun yok.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. http://localhost:5173 adresinde `goksin.ozgorkey@mail.test` / `Tezgah123!` ile giriş yapın (örnek veri hesaplarının şifresi hepsinde aynıdır).
3. Hesabım → Siparişlerim sayfasını açın ve yaklaşık iki yıl önceki en eski siparişlerden birine tıklayın.
4. Karşılaştırma için aynı hesabın diğer siparişlerine tıklayın.

## Beklenen

Sipariş detayı açılır: kalemler, tutarlar, durum geçmişi görünür.

## Gerçekleşen

O sipariş için detay sayfası açılmıyor, hata kutusu çıkıyor.

## Ek bilgi

- Geçen ay bu sipariş açılıyordu; müşteri hizmetleri ekibi eski siparişlere sık bakıyor.
- Aynı müşterinin diğer siparişleri açılıyor.
- Siparişlerim listesi etkilenmiyor.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 8
```

---

## English

# TZ-4170 · Some old orders cannot be opened

| Field | Value |
| --- | --- |
| Ticket | TZ-4170 |
| Level | ● (1/5) |
| Where it shows | Storefront · My Orders |
| Reporter | Cem Başaran, customer service |
| Priority | High |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

A customer (goksin.ozgorkey@mail.test) called: "One of my oldest orders, from about two years ago, does not open. I need its invoice details; I can see it in My Orders but it does not open when I click it." I checked with the same account. The My Orders list opens and the orders are there. But when I click that old order, the detail page shows only an error box:

> An error occurred while processing your request.

The "Tekrar dene" ("Try again") button does not help. The detail pages of the same customer's other orders open fine. New orders have no problem.

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Log in at http://localhost:5173 with `goksin.ozgorkey@mail.test` / `Tezgah123!` (all sample accounts share the same password).
3. Open My Account → My Orders and click one of the oldest orders, from about two years ago.
4. For comparison, click the other orders of the same account.

### Expected

The order detail opens: items, amounts and status history are visible.

### Actual

For that order the detail page does not open; an error box appears.

### Additional information

- This order opened last month; the customer service team often looks at old orders.
- The same customer's other orders open.
- The My Orders list is not affected.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 8
```
