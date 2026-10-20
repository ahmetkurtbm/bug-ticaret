# TZ-4553 · Son ürün iki müşteriye satıldı

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4553 |
| Seviye | ●●● (3/5) |
| Modül | Sipariş · Stok |
| Bildiren | Fatih Özgür, depo ve operasyon sorumlusu |
| Öncelik | Kritik |
| Ortam | Test ortamı; yerelde API (`--launch-profile http`) + Ödeme servisi (`--launch-profile http`) |
| Durum | Açık |

## Ne oldu

Dün akşamki flaş indirimde stokta tek adet kalan bir ürün için depoda sorun çıktı. Sistemde tek adet vardı ama aynı ürün için birden fazla müşterinin siparişi var; üçü ödeme sayfasına geçmiş, ikisi ödemeyi tamamlamış. Depoda gönderilecek tek ürün var. Müşterilerden ikisine "stoğumuz tükendi, özür dileriz" diye yazmak zorunda kaldık.

Müşteriler sitede bir hata görmemiş. Hepsi "Siparişiniz alındı" ekranına gitmiş. Oysa ikinci müşterinin şu mesajı görmesi gerekirdi:

> Sepetinizdeki bir ürünün stoğu tükendi. Sepetinizi kontrol edin.

Ürünün stok kartına bakınca ayrılan adedin 1 göründüğünü fark ettim, oysa ödeme bekleyen birden fazla siparişte bu ürün var. Rakamlar birbirini tutmuyor.

QA ekibine söyledim, "tek hesapla deneyince stok bittiğinde düzgün uyarı veriyor" dediler. Yani normal kullanımda çalışıyor, sadece yoğun anlarda bozuluyor.

## Tekrar etme adımları

Elle denemek zordur; sorunun görünmesi için siparişlerin aynı anda gelmesi gerekir. Kontrol aracı bunu sizin için yapar.

1. Altyapıyı başlatın ve örnek veriyi üretin: bkz. [Kurulum](../../docs/KURULUM.md).
2. API'yi başlatın: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
3. Ödeme servisini başlatın: `dotnet run --project backend/src/Tezgah.Payments --launch-profile http`
4. Kontrol aracını çalıştırın. Araç 8 müşteriyi sepetlerine aynı ürünü koyup stoğu 1 adede indirir ve 8 siparişi aynı anda gönderir:

   ```bash
   dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 18
   ```
5. Çıktıdaki "Başarılı sipariş" ve "stok tükendi (409)" sayılarına bakın. Araç bittiğinde açtığı siparişleri iptal eder ve stoğu eski hâline getirir; kanıtı kendiniz yakalamak isterseniz hata ayıklayıcı ya da SQL günlüğü kullanın.

## Beklenen

Stokta 1 adet varken 8 müşteri aynı anda sipariş verirse yalnızca biri siparişi tamamlar. Diğer yedisi "Sepetinizdeki bir ürünün stoğu tükendi" cevabını (409) alır. Stokta ayrılan adet 1'dir ve siparişlerin toplamıyla tutar.

## Gerçekleşen

Birden fazla müşteri siparişi tamamlıyor (kontrol aracı: "Tek kalan ürün N müşteriye satıldı."). Stoktaki ayrılan adet ise sipariş sayısı kadar artmıyor.

## Ek bilgi

- Tek bir müşteriyle ya da arka arkaya (sıralı) siparişlerle hiç sorun görülmüyor. Hata yalnızca siparişler birkaç on milisaniye içinde aynı anda geldiğinde ortaya çıkıyor.
- Flaş indirim gününden önceki haftalarda stokta hiç tutarsızlık görmemiştik; bu sürümden sonra başladı.
- Veritabanında stok tablosunda negatif ya da tutarsız bir kısıt hatası görünmüyor.

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
| Module | Order · Stock |
| Reporter | Fatih Özgür, warehouse and operations lead |
| Priority | Critical |
| Environment | Test environment; locally the API (`--launch-profile http`) + the Payments service (`--launch-profile http`) |
| Status | Open |

### What happened

During yesterday evening's flash sale there was a problem in the warehouse with a product that had a single unit left in stock. The system had one unit, but more than one customer has an order for the same product; three reached the payment page and two completed the payment. There is only one product to ship. We had to write to two customers: "our stock is exhausted, we are sorry".

Customers saw no error on the site. All of them got to the "Siparişiniz alındı" ("Your order was received") screen. But the second customer should have seen this message (in Turkish):

> Sepetinizdeki bir ürünün stoğu tükendi. Sepetinizi kontrol edin. ("The stock of an item in your cart is exhausted. Check your cart.")

Looking at the product's stock card I noticed that the reserved quantity shows 1, although more than one order awaiting payment contains this product. The numbers do not add up.

I told QA; they said "with a single account the warning is shown correctly when the stock runs out". So it works in normal use and only breaks at busy moments.

### Steps to reproduce

Trying it by hand is hard; the problem only shows when orders arrive at the same time. The check tool does this for you.

1. Start the infrastructure and generate the sample data: see [Setup](../../docs/KURULUM.md).
2. Start the API: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
3. Start the Payments service: `dotnet run --project backend/src/Tezgah.Payments --launch-profile http`
4. Run the check tool. It puts the same product in the carts of 8 customers, reduces the stock to 1 unit and sends 8 orders at the same time:

   ```bash
   dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 18
   ```
5. Look at the "Başarılı sipariş" (successful orders) and "stok tükendi (409)" (stock exhausted) numbers in the output. When it finishes, the tool cancels the orders it opened and restores the stock; if you want to catch the evidence yourself, use the debugger or the SQL log.

### Expected

With 1 unit in stock, when 8 customers order at the same time only one completes the order. The other seven get the "Sepetinizdeki bir ürünün stoğu tükendi" response (409). The reserved quantity on the stock row is 1 and matches the sum of the orders.

### Actual

More than one customer completes the order (check tool: "Tek kalan ürün N müşteriye satıldı."). The reserved quantity on the stock row does not grow as much as the number of orders.

### Additional information

- With a single customer, or with orders one after another (sequential), nothing is wrong. The problem appears only when orders arrive within a few tens of milliseconds of each other.
- We saw no stock inconsistency in the weeks before the flash sale day; it started with this release.
- In the database we see no constraint error on the stock table.

### When you are done

Run this while the API and the Payments service are running:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 18
```
