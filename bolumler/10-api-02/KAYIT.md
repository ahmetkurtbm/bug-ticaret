# TZ-4181 · Hiçbir kupon kodu kabul edilmiyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4181 |
| Seviye | ● (1/5) |
| Modül | Müşteri sitesi · Sepet |
| Bildiren | Sibel Arıkan, müşteri hizmetleri |
| Öncelik | Yüksek |
| Ortam | Yerel geliştirme; API http://localhost:5080, müşteri sitesi http://localhost:5173 (`npm run dev:storefront`), Chrome |
| Durum | Açık |

## Ne oldu

Müşteriler "kupon kodu çalışmıyor" diye yazıyor. Kampanya duyurusundaki `KARGOBEDAVA` kodunu denedim. Sepet sayfasında "İndirim kodu" kutusuna yazıp "Uygula"ya basınca kutunun altında kırmızı bir yazı çıkıyor:

> One or more validation errors occurred.

Kod doğru yazılmış, büyük harfle; sepet tutarı da 150 TL'nin üstünde. `HOSGELDIN10` kodunu da denedim (sepet 200 TL üstü), aynı yazı. İndirim hiç uygulanmıyor. Yazının İngilizce olması da garip; başka hatalar Türkçe geliyor.

## Tekrar etme adımları

1. API çalışsın: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
2. `cd frontend && npm run dev:storefront`
3. http://localhost:5173 adresinde sepete toplam tutarı en az 200 TL olan bir ürün ekleyin (misafir olarak da olur; ya da `musteri@tezgah.test` / `Tezgah123!` ile giriş yapın).
4. Sepet sayfasında "İndirim kodu" kutusuna `KARGOBEDAVA` yazıp "Uygula"ya basın.
5. F12 → Network sekmesini açık tutun.

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

Bu kontrol bir birim testi çalıştırır (`frontend/apps/storefront/src/__bolum__/api02-kupon.test.ts`); API'nin çalışması gerekmez.

---

## English

# TZ-4181 · No coupon code is accepted

| Field | Value |
| --- | --- |
| Ticket | TZ-4181 |
| Level | ● (1/5) |
| Module | Storefront · Cart |
| Reporter | Sibel Arıkan, customer service |
| Priority | High |
| Environment | Local development; API http://localhost:5080, storefront http://localhost:5173 (`npm run dev:storefront`), Chrome |
| Status | Open |

### What happened

Customers write "the coupon code does not work". I tried the code `KARGOBEDAVA` from the campaign announcement. On the cart page I type it into the "İndirim kodu" ("Discount code") box and press "Uygula" ("Apply"); a red text appears under the box:

> One or more validation errors occurred.

The code is spelled correctly, in capitals, and the cart total is above 150 TL. I also tried `HOSGELDIN10` (cart above 200 TL) with the same text. No discount is applied at all. It is also odd that the text is in English; other errors come in Turkish.

### Steps to reproduce

1. Start the API: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
2. `cd frontend && npm run dev:storefront`
3. At http://localhost:5173 add a product to the cart with a total of at least 200 TL (as a guest is fine, or log in as `musteri@tezgah.test` / `Tezgah123!`).
4. On the cart page type `KARGOBEDAVA` into the "İndirim kodu" box and press "Uygula".
5. Keep F12 → Network open.

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

This check runs a unit test (`frontend/apps/storefront/src/__bolum__/api02-kupon.test.ts`); the API does not need to be running.
