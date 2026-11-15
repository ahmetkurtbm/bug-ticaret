# TZ-4553 · Son ürün iki müşteriye satıldı

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4553 |
| Seviye | ●●● (3/5) |
| Nerede görülüyor | Müşteri sitesi · Sipariş |
| Bildiren | Fatih Özgür, depo ve operasyon sorumlusu |
| Öncelik | Kritik |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Dün akşamki flaş indirimde stokta tek adet kalan bir ürün için depoda sorun çıktı. Sistemde tek adet vardı ama aynı ürün için birden fazla müşterinin siparişi var; üçü ödeme sayfasına geçmiş, ikisi ödemeyi tamamlamış. Depoda gönderilecek tek ürün var. Müşterilerden ikisine "stoğumuz tükendi, özür dileriz" diye yazmak zorunda kaldık.

Müşteriler sitede bir hata görmemiş. Hepsi "Siparişiniz alındı" ekranına gitmiş. Oysa ikinci müşterinin şu mesajı görmesi gerekirdi:

> Sepetinizdeki bir ürünün stoğu tükendi. Sepetinizi kontrol edin.

Ürünün stok kartına bakınca ayrılan adedin 1 göründüğünü fark ettim, oysa ödeme bekleyen birden fazla siparişte bu ürün var. Rakamlar birbirini tutmuyor.

QA ekibine söyledim, "tek hesapla deneyince stok bittiğinde düzgün uyarı veriyor" dediler. Yani normal kullanımda çalışıyor, sadece yoğun anlarda bozuluyor.

## Tekrar etme adımları

Sorun yoğun saatlerde görülüyor; elle denemek zordur.

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. Altyapıyı başlatın ve örnek veriyi üretin: bkz. [Kurulum](../../docs/KURULUM.md).
3. O anki trafiği canlandırmak için kontrol aracını bir kez çalıştırın:

   ```bash
   dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 18
   ```
5. Sonrasında ilgili ürünün stok durumuna ve açılan siparişlere bakın.

## Beklenen

Stokta 1 adet varken birden fazla müşteri sipariş verirse yalnızca biri siparişi tamamlar. Diğerleri "Sepetinizdeki bir ürünün stoğu tükendi" mesajını görür. Stok kartındaki ayrılan adet 1'dir ve siparişlerin toplamıyla tutar.

## Gerçekleşen

Birden fazla müşteri siparişi tamamlıyor. Stok kartındaki ayrılan adet sipariş sayısı kadar artmıyor.

## Ek bilgi

- Tek bir müşteriyle ya da arka arkaya (sırayla) verilen siparişlerde hiç sorun görülmüyor; yalnızca yoğun anlarda ortaya çıkıyor.
- Flaş indirim gününden önceki haftalarda stokta hiç tutarsızlık görmemiştik; bu sürümden sonra başladı.

## Bitirdiğinizde

API ve Ödeme servisi çalışırken şunu çalıştırın:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 18
```

---

## English

# TZ-4553 · The last item was sold twice

| Field | Value |
| --- | --- |
| Ticket | TZ-4553 |
| Level | ●●● (3/5) |
| Where it shows | Storefront · Order |
| Reporter | Fatih Özgür, warehouse and operations lead |
| Priority | Critical |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

During yesterday evening's flash sale there was a problem in the warehouse with a product that had a single unit left in stock. The system had one unit, but more than one customer has an order for the same product; three reached the payment page and two completed the payment. There is only one product to ship. We had to write to two customers: "our stock is exhausted, we are sorry".

Customers saw no error on the site. All of them got to the "Siparişiniz alındı" ("Your order was received") screen. But the second customer should have seen this message (in Turkish):

> Sepetinizdeki bir ürünün stoğu tükendi. Sepetinizi kontrol edin. ("The stock of an item in your cart is exhausted. Check your cart.")

Looking at the product's stock card I noticed that the reserved quantity shows 1, although more than one order awaiting payment contains this product. The numbers do not add up.

I told QA; they said "with a single account the warning is shown correctly when the stock runs out". So it works in normal use and only breaks at busy moments.

### Steps to reproduce

The problem shows at busy times; trying it by hand is hard.

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Start the infrastructure and generate the sample data: see [Setup](../../docs/KURULUM.md).
3. To recreate the traffic of that moment, run the check tool once:

   ```bash
   dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 18
   ```
5. Afterwards look at the stock status of the product concerned and at the orders that were opened.

### Expected

With 1 unit in stock, when more than one customer orders, only one completes the order. The others see the "Sepetinizdeki bir ürünün stoğu tükendi" message. The reserved quantity on the stock card is 1 and matches the sum of the orders.

### Actual

More than one customer completes the order. The reserved quantity on the stock card does not grow as much as the number of orders.

### Additional information

- With a single customer, or with orders placed one after another, nothing is wrong; it only appears at busy moments.
- We saw no stock inconsistency in the weeks before the flash sale day; it started with this release.

### When you are done

Run this while the API and the Payments service are running:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 18
```
