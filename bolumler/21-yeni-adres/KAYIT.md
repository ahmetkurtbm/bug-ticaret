# TZ-4617 · Yeni eklenen adres listede görünmüyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4617 |
| Seviye | ● (1/5) |
| Nerede görülüyor | Müşteri sitesi · Ödeme |
| Bildiren | Elif Doğan, müşteri hizmetleri |
| Öncelik | Orta |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Birkaç müşteri aradı: "Ödeme sayfasında yeni adres ekledim, 'Adresi kaydet'e bastım, form kapandı ama adresim listede yok. Kaydedilmedi sandım, tekrar ekledim." Ben de denedim. Formu doldurup "Adresi kaydet"e basınca form kapanıyor, hata mesajı yok; ama adres listesinde yeni adres görünmüyor. Eski adresler aynen duruyor.

Sayfayı elle yenileyince (F5) yeni adres listede çıkıyor. Bazı müşteriler adresi iki kez eklemiş; yenileyince aynı adresi iki kez görmüşler.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. http://localhost:5173 adresinde `musteri@tezgah.test` / `Tezgah123!` ile giriş yapın (örnek veri hesaplarının şifresi hepsinde aynıdır).
3. Sepete bir ürün ekleyip Ödeme sayfasına gidin.
4. "Yeni adres ekle"ye basın, formu doldurun ve "Adresi kaydet"e basın.
5. Adres listesine bakın, ardından sayfayı yenileyip tekrar bakın.

## Beklenen

Form kapanır ve yeni adres, ekranda sayfayı yenilemeye gerek kalmadan listede görünür.

## Gerçekleşen

Form kapanıyor ama yeni adres listede yok. Sayfa yenilenince görünüyor.

## Ek bilgi

- Hata kutusu ya da kırmızı bir uyarı çıkmıyor.
- Yenileyince adres listede duruyor; yani kaybolmuş değil.
- Sepete ürün ekleme ve ödeme adımının geri kalanı normal çalışıyor.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 21
```

---

## English

# TZ-4617 · A newly added address does not appear in the list

| Field | Value |
| --- | --- |
| Ticket | TZ-4617 |
| Level | ● (1/5) |
| Where it shows | Storefront · Checkout |
| Reporter | Elif Doğan, customer service |
| Priority | Medium |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

A few customers called: "I added a new address on the checkout page and pressed 'Adresi kaydet' ('Save address'); the form closed but my address is not in the list. I thought it was not saved and added it again." I tried it as well. When I fill the form and press "Adresi kaydet", the form closes and there is no error message; but the new address does not show in the address list. The old addresses stay as they were.

When I refresh the page by hand (F5), the new address appears in the list. Some customers added the address twice; after refreshing they saw the same address twice.

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Log in at http://localhost:5173 with `musteri@tezgah.test` / `Tezgah123!` (all sample accounts share the same password).
3. Add a product to the cart and go to the checkout page.
4. Press "Yeni adres ekle" ("Add new address"), fill in the form and press "Adresi kaydet".
5. Look at the address list, then refresh the page and look again.

### Expected

The form closes and the new address shows in the list without refreshing the page.

### Actual

The form closes but the new address is not in the list. It shows up after the page is refreshed.

### Additional information

- No error box or red warning appears.
- After refreshing the address stays in the list; so it is not lost.
- Adding products to the cart and the rest of the checkout step work normally.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 21
```
