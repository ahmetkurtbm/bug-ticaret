# TZ-4715 · Kupon kabul edilmiyor ama sebebi yazmıyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4715 |
| Seviye | ● (1/5) |
| Nerede görülüyor | Müşteri sitesi · Sepet |
| Bildiren | Berk Aydemir, müşteri hizmetleri |
| Öncelik | Orta |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Müşteriler sepette hangi kupon kodunu girerse girsin yalnızca "Kupon uygulanamadı." mesajını görüyor. Kodu yanlış mı yazdıklarını, kuponun süresinin mi dolduğunu, yoksa başka bir koşulu mu sağlamadıklarını anlayamıyorlar ve bizi arıyorlar. Çağrı merkezinde bu hafta en sık sorulan konulardan biri oldu.

Ben de denedim: hem hiç var olmayan bir kod hem de süresi geçmiş bir kupon aynı mesajı veriyor. Geçen ay aynı hatalarda "Kupon kodu geçersiz." ve "Bu kuponun süresi doldu." gibi ayrı mesajlar çıkıyordu; müşteriye ne yapacağını söylemek çok daha kolaydı.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. http://localhost:5173 adresinde `musteri@tezgah.test` / `Tezgah123!` ile giriş yapın.
3. Herhangi bir ürünü sepete ekleyin ve sepet sayfasını açın.
4. Kupon kodu alanına var olmayan bir kod (`YOKBOYLEKUPON`) yazıp uygulayın.
5. Süresi geçmiş bir kodla tekrar deneyin. Böyle bir kodu bulmak için http://localhost:5175 adresinde `admin@tezgah.test` ile giriş yapıp Kampanyalar listesinde durumu "Süresi doldu" olan bir kuponun kodunu kullanın.

## Beklenen

Kodun neden kabul edilmediği müşteriye söylenir: kod geçersizse "geçersiz", süresi dolmuşsa "süresi doldu" gibi, ne yapacağını anlatan ayrı mesajlar.

## Gerçekleşen

İki durumda da (ve diğer kupon sorunlarında da) aynı mesaj çıkıyor: "Kupon uygulanamadı."

## Ek bilgi

- Geçerli bir kupon sorunsuz uygulanıyor; yalnızca reddedilen kodlarda mesaj belirsiz.
- Sepetteki kupon alanı dışında bir hata görünmüyor; sayfa normal çalışıyor.
- Geçen ay aynı denemelerde kodun neden reddedildiğini söyleyen mesajlar çıkıyordu.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 28
```

---

## English

# TZ-4715 · Coupon is rejected without a reason

| Field | Value |
| --- | --- |
| Ticket | TZ-4715 |
| Level | ● (1/5) |
| Where it shows | Storefront · Cart |
| Reporter | Berk Aydemir, customer service |
| Priority | Medium |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

Whichever coupon code customers enter in the cart, they see only "Kupon uygulanamadı." ("The coupon could not be applied."). They cannot tell whether they typed the code wrongly, whether the coupon has expired, or whether they miss some other condition, and they call us. It has been one of the most frequent topics at the call centre this week.

I tried it too: both a code that does not exist at all and an expired coupon give the same message. Last month the same mistakes produced separate messages such as "Kupon kodu geçersiz." ("The coupon code is invalid.") and "Bu kuponun süresi doldu." ("This coupon has expired."); it was much easier to tell the customer what to do.

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Log in at http://localhost:5173 with `musteri@tezgah.test` / `Tezgah123!`.
3. Add any product to the cart and open the cart page.
4. Type a code that does not exist (`YOKBOYLEKUPON`) into the coupon code field and apply it.
5. Try again with an expired code. To find one, sign in at http://localhost:5175 with `admin@tezgah.test` and use the code of a coupon whose status is "Süresi doldu" ("Expired") in the Campaigns list.

### Expected

The customer is told why the code was not accepted: separate messages that explain what to do, such as "invalid" if the code is wrong and "expired" if its time has run out.

### Actual

The same message appears in both cases (and for other coupon problems too): "Kupon uygulanamadı."

### Additional information

- A valid coupon is applied without any problem; only for rejected codes is the message unclear.
- Apart from the coupon field, no error is visible; the page works normally.
- Last month the same attempts showed messages that said why the code was rejected.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 28
```
