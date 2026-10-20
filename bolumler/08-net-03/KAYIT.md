# TZ-4170 · Bazı eski siparişlerin detayı açılmıyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4170 |
| Seviye | ● (1/5) |
| Modül | Sipariş detayı |
| Bildiren | Cem Başaran, müşteri hizmetleri |
| Öncelik | Yüksek |
| Ortam | Yerel geliştirme (örnek veri, küçük profil); API http://localhost:5080, müşteri sitesi http://localhost:5173 |
| Durum | Açık |

## Ne oldu

Bir müşteri aradı: "Eski siparişimin fatura bilgisi lazım, Siparişlerim'de görüyorum ama üstüne tıklayınca açılmıyor." Ben de baktım. Siparişlerim listesi açılıyor, siparişler orada duruyor. Ama bazı eski siparişlerin üstüne tıklayınca detay sayfasında yalnızca bir hata kutusu çıkıyor:

> An error occurred while processing your request.

"Tekrar dene" düğmesi işe yaramıyor. Aynı müşterinin diğer siparişlerinin detayı sorunsuz açılıyor; yalnızca birkaçı açılmıyor. Yeni siparişlerde hiç sorun yok.

Müşterinin kişisel bilgilerini kayda koymuyorum. Örnek veride böyle bir sipariş bulunduğunu doğruladım; aşağıdaki araç onu ve sahibini kendisi buluyor.

## Tekrar etme adımları

1. API'yi başlatın: `dotnet run --project backend/src/Tezgah.Api --launch-profile http` (örnek veri yüklü olmalı).
2. Kontrol aracını çalıştırın: `dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 8`. Araç, adres bilgisi olmayan eski bir siparişi ve sahibini bulup şöyle bir satır yazar: `Eski sipariş: <sipariş no> (<e-posta>)`.
3. http://localhost:5173 adresinde o e-postayla giriş yapın (örnek veri hesaplarının şifresi `Tezgah123!`).
4. Siparişlerim sayfasından o siparişe tıklayın ya da adresi doğrudan açın: `http://localhost:5173/hesabim/siparisler/<sipariş no>`.

## Beklenen

Sipariş detayı açılır: kalemler, tutarlar, durum geçmişi görünür. Teslimat adresi bilgisi yoksa bu bölüm boş ya da "Adres bilgisi yok." diye gösterilir.

## Gerçekleşen

Detay sayfası açılmıyor, hata kutusu çıkıyor.

## Ek bilgi

- Geçen ay bu siparişler açılıyordu; müşteri hizmetleri ekibi eski siparişlere sık bakıyor.
- Eski siparişlerin hepsi etkilenmiyor, ama etkilenenler hep eski siparişler.
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
| Module | Order detail |
| Reporter | Cem Başaran, customer service |
| Priority | High |
| Environment | Local development (sample data, small profile); API http://localhost:5080, storefront http://localhost:5173 |
| Status | Open |

### What happened

A customer called: "I need the invoice details of my old order. I can see it in My Orders but it does not open when I click it." I checked. The My Orders list opens and the orders are there. But when I click some of the old orders, the detail page shows only an error box:

> An error occurred while processing your request.

The "Tekrar dene" ("Try again") button does not help. The detail pages of the same customer's other orders open fine; only a few do not. New orders have no problem.

I am not putting the customer's personal details in the ticket. I confirmed that the sample data contains such an order; the tool below finds it and its owner by itself.

### Steps to reproduce

1. Start the API: `dotnet run --project backend/src/Tezgah.Api --launch-profile http` (sample data must be loaded).
2. Run the check tool: `dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 8`. The tool finds an old order without address information and its owner and prints a line like `Eski sipariş: <order no> (<email>)`.
3. Log in at http://localhost:5173 with that email (the password of the sample accounts is `Tezgah123!`).
4. Click that order in My Orders, or open the address directly: `http://localhost:5173/hesabim/siparisler/<order no>`.

### Expected

The order detail opens: items, amounts and status history are visible. If there is no delivery address information, that section is empty or says "Adres bilgisi yok." ("No address information.").

### Actual

The detail page does not open; an error box appears.

### Additional information

- These orders opened last month; the customer service team often looks at old orders.
- Not all old orders are affected, but the affected ones are always old.
- The My Orders list is not affected.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 8
```
