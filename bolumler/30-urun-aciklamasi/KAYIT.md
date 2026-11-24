# TZ-4748 · Bir ürün sayfası açılınca tuhaf bir uyarı penceresi çıkıyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4748 |
| Seviye | ●● (2/5) |
| Nerede görülüyor | Müşteri sitesi · Ürün sayfası |
| Bildiren | Gökhan Yurt, güvenlik ekibi (bir müşterinin bildirimini iletiyor) |
| Öncelik | Acil (müşteri oturumu risk altında) |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Bir müşteri destek formundan yazdı: belirli bir ürünün sayfasını açınca tarayıcıda "Tezgah" yazan bir uyarı penceresi çıkıyormuş; sayfayı her açışında tekrar ediyormuş. Ürünün kendisi normal görünüyor, ama pencereyi kapatınca bile sayfanın başka birine ait bir şey çalıştırdığından emin olamıyor.

Güvenlik ekibi olarak ürünün açıklama metnine baktık: bir satıcı açıklamasının sonuna aşağıdaki metni eklemiş:

```text
<img src=x onerror="alert('Tezgah')">
```

Metin kullanıcıya görünmüyor, ama sayfa açılınca uyarı penceresi çıkıyor. Satıcılar bu alana istediklerini yazabiliyor ve ürün sayfasını görüntüleyen herkes etkileniyor. Sorun bu hafta başladı; geçen hafta aynı metin sayfada yalnızca sıradan yazı olarak görünüyordu.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. http://localhost:5174 adresinde `satici@tezgah.test` / `Tezgah123!` ile giriş yapın.
3. Ürünler sayfasında bir ürünü düzenleyin; açıklamanın sonuna yukarıdaki metni ekleyip kaydedin. Ürün onaya düşer.
4. http://localhost:5175 adresinde `admin@tezgah.test` / `Tezgah123!` ile giriş yapın; Ürün onayı sayfasında ürünün değişikliğini onaylayın.
5. http://localhost:5173 adresinde (giriş yapmadan da olur) o ürünün sayfasını açın.

## Beklenen

Ürünün açıklaması sayfada yazı olarak görünür; hiçbir pencere çıkmaz.

## Gerçekleşen

Ürün sayfası açılır açılmaz "Tezgah" yazan bir uyarı penceresi çıkıyor.

## Ek bilgi

- Yalnızca eklenen metni taşıyan ürünün sayfasında oluyor; diğer ürünlerde olmuyor.
- Sayfayı yenilemek ya da başka bir sekmede açmak aynı sonucu veriyor.
- Satıcı ve yönetim panellerinde uyarı çıkmıyor; yalnızca müşteri sitesinin ürün sayfasında.
- Müşteri sitesinde giriş yapmış bir kullanıcı için de aynı şey oluyor.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 30
```

---

## English

# TZ-4748 · A strange alert pops up on a product page

| Field | Value |
| --- | --- |
| Ticket | TZ-4748 |
| Level | ●● (2/5) |
| Where it shows | Storefront · Product page |
| Reporter | Gökhan Yurt, security team (relaying a customer's report) |
| Priority | Urgent (customer sessions at risk) |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

A customer wrote through the support form: when they open the page of a certain product, an alert window saying "Tezgah" appears in the browser; it repeats every time they open the page. The product itself looks normal, but even after closing the window they cannot be sure the page is not running something that belongs to someone else.

As the security team we looked at the product's description text: a seller added the following text to the end of the description:

```text
<img src=x onerror="alert('Tezgah')">
```

The text is not visible to the user, but when the page opens the alert window appears. Sellers can write anything they like in this field, and everyone who views the product page is affected. The problem started this week; last week the same text appeared on the page only as ordinary text.

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Log in at http://localhost:5174 with `satici@tezgah.test` / `Tezgah123!`.
3. On the Products page edit a product; add the text above to the end of the description and save. The product goes to approval.
4. Log in at http://localhost:5175 with `admin@tezgah.test` / `Tezgah123!`; on the product approval page approve the change to the product.
5. At http://localhost:5173 (you do not have to log in) open the page of that product.

### Expected

The product description appears on the page as text; no window pops up.

### Actual

As soon as the product page opens, an alert window saying "Tezgah" appears.

### Additional information

- It happens only on the page of the product that carries the added text; not on other products.
- Refreshing the page or opening it in another tab gives the same result.
- No alert appears in the seller and admin panels; only on the storefront's product page.
- It happens for a user who is logged in on the storefront too.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 30
```
