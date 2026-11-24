# TZ-4601 · Başka bir müşterinin siparişi açılabiliyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4601 |
| Seviye | ● (1/5) |
| Nerede görülüyor | Müşteri sitesi · Siparişlerim |
| Bildiren | Mehmet Sarı, güvenlik ekibi |
| Öncelik | Acil (müşteri verisi açıkta) |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Bir müşteri destek formundan yazdı: tarayıcının adres çubuğundaki sipariş numarasını elle değiştirince, kendisine ait olmayan bir siparişin sayfası açılmış. Sayfada başka birinin adı, teslimat adresi, telefonu ve sipariş kalemleri görünüyormuş.

Ben de iki müşteri hesabıyla denedim ve aynısını gördüm. Müşteri sitesinde kendi siparişlerim listeleniyor, orada sorun yok; ama başkasının sipariş numarasını adrese yazınca o siparişin bütün ayrıntısı önüme geliyor. Sipariş numaraları kolay bulunuyor: `TZ-2026-` ile başlıyor.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. http://localhost:5173 adresinde `musteri@tezgah.test` / `Tezgah123!` ile giriş yapın (örnek veri hesaplarının şifresi hepsinde aynıdır).
3. Hesabım → Siparişlerim sayfasından bir siparişin detayını açın ve adres çubuğundaki sipariş numarasını (`/hesabim/siparisler/<numara>`) not alın.
4. Çıkış yapın, başka bir müşteriyle (örneğin `akas.velioglu@posta.test` / `Tezgah123!`) giriş yapın.
5. Not aldığınız adresi (aynı sipariş numarasıyla) tarayıcının adres çubuğuna yazın.

## Beklenen

Bu müşteriye ait olmayan sipariş açılmaz; "Sipariş bulunamadı" görünür.

## Gerçekleşen

Sipariş, ait olduğu müşterinin adı, adresi ve kalemleriyle birlikte açılıyor.

## Ek bilgi

- İki hesapta da giriş yapılmış durumda; oturumla ilgili bir uyarı yok.
- Siparişlerim listesi her müşteriye yalnızca kendi siparişlerini gösteriyor.
- Var olmayan bir sipariş numarası yazınca "Sipariş bulunamadı" çıkıyor.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 20
```

---

## English

# TZ-4601 · Another customer's order can be opened

| Field | Value |
| --- | --- |
| Ticket | TZ-4601 |
| Level | ● (1/5) |
| Where it shows | Storefront · My Orders |
| Reporter | Mehmet Sarı, security team |
| Priority | Urgent (customer data exposed) |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

A customer wrote through the support form: when they edited the order number in the browser's address bar by hand, the page of an order that does not belong to them opened. The page showed someone else's name, delivery address, phone number and order items.

I tried it with two customer accounts and saw the same. On the storefront my own orders are listed and that is fine; but when I type someone else's order number into the address bar, the full detail of that order appears. Order numbers are easy to come by: they start with `TZ-2026-`.

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Log in at http://localhost:5173 with `musteri@tezgah.test` / `Tezgah123!` (all sample accounts share the same password).
3. Open the detail of one order from My Account → My Orders and note the order number in the address bar (`/hesabim/siparisler/<number>`).
4. Log out and log in as another customer (for example `akas.velioglu@posta.test` / `Tezgah123!`).
5. Type the address you noted (with the same order number) into the browser's address bar.

### Expected

An order that does not belong to this customer does not open; "Sipariş bulunamadı" ("Order not found") is shown.

### Actual

The order opens, together with the name, address and items of the customer it belongs to.

### Additional information

- Both accounts are logged in; there is no session warning.
- The My Orders list shows each customer only their own orders.
- When I type an order number that does not exist, "Sipariş bulunamadı" appears.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 20
```
