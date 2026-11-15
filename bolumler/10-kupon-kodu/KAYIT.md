# TZ-4181 · Hiçbir kupon kodu kabul edilmiyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4181 |
| Seviye | ● (1/5) |
| Nerede görülüyor | Müşteri sitesi · Sepet |
| Bildiren | Sibel Arıkan, müşteri hizmetleri |
| Öncelik | Yüksek |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Müşteriler "kupon kodu çalışmıyor" diye yazıyor. Kampanya duyurusundaki `KARGOBEDAVA` kodunu denedim. Sepet sayfasında "İndirim kodu" kutusuna yazıp "Uygula"ya basınca kutunun altında kırmızı bir yazı çıkıyor:

> One or more validation errors occurred.

Kod doğru yazılmış, büyük harfle; sepet tutarı da 150 TL'nin üstünde. `HOSGELDIN10` kodunu da denedim (sepet 200 TL üstü), aynı yazı. İndirim hiç uygulanmıyor. Yazının İngilizce olması da garip; başka hatalar Türkçe geliyor.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. http://localhost:5173 adresinde sepete toplam tutarı en az 200 TL olan bir ürün ekleyin (misafir olarak da olur; ya da `musteri@tezgah.test` / `Tezgah123!` ile giriş yapın).
3. Sepet sayfasında "İndirim kodu" kutusuna `KARGOBEDAVA` yazıp "Uygula"ya basın.

## Beklenen

Kod uygulanır, sepette kupon görünür ve kargo ücreti sıfırlanır.

## Gerçekleşen

Kutunun altında "One or more validation errors occurred." yazar; kupon uygulanmaz.

## Ek bilgi

- Geçen hafta `KARGOBEDAVA` çalışıyordu.
- Kodun kendisinde sorun yok gibi: yönetim panelinde aktif görünüyor ve süresi dolmamış.
- Sepete ürün ekleme, adet değiştirme ve silme normal çalışıyor, yalnızca kupon uygulama bozuk.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 10
```

---

## English

# TZ-4181 · No coupon code is accepted

| Field | Value |
| --- | --- |
| Ticket | TZ-4181 |
| Level | ● (1/5) |
| Where it shows | Storefront · Cart |
| Reporter | Sibel Arıkan, customer service |
| Priority | High |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

Customers write "the coupon code does not work". I tried the code `KARGOBEDAVA` from the campaign announcement. On the cart page I type it into the "İndirim kodu" ("Discount code") box and press "Uygula" ("Apply"); a red text appears under the box:

> One or more validation errors occurred.

The code is spelled correctly, in capitals, and the cart total is above 150 TL. I also tried `HOSGELDIN10` (cart above 200 TL) with the same text. No discount is applied at all. It is also odd that the text is in English; other errors come in Turkish.

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. At http://localhost:5173 add a product to the cart with a total of at least 200 TL (as a guest is fine, or log in as `musteri@tezgah.test` / `Tezgah123!`).
3. On the cart page type `KARGOBEDAVA` into the "İndirim kodu" box and press "Uygula".

### Expected

The code is applied, the coupon shows in the cart and the shipping fee drops to zero.

### Actual

The text "One or more validation errors occurred." appears under the box; the coupon is not applied.

### Additional information

- `KARGOBEDAVA` worked last week.
- There seems to be nothing wrong with the code itself: it shows as active in the admin panel and has not expired.
- Adding products to the cart, changing quantities and removing them work normally; only applying a coupon is broken.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 10
```
