# TZ-4142 · Başvuru formu neyin yanlış olduğunu söylemiyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4142 |
| Seviye | ● (1/5) |
| Nerede görülüyor | Satıcı paneli · Başvuru |
| Bildiren | Serkan Aydoğdu, satıcı ilişkileri |
| Öncelik | Orta |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Mağaza açmak isteyen bir esnaf bize telefonla "Başvuruyu gönder düğmesine basıyorum, bir şey olmuyor" dedi. Kendim denedim: düğme bir an "Gönderiliyor…" yazıyor, sonra "Başvuruyu gönder"e dönüyor. Ekranda hiçbir hata yazısı yok, kırmızı bir alan yok, "başvurunuz alındı" da yok. IBAN'ı bilerek eksik yazmıştım; formun bunu söylemesini beklerdim. Esnaf yarım saat boyunca neyi yanlış yaptığını bulmaya çalışmış.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. http://localhost:5174/kayit adresinden yeni bir hesap açın (örneğin `yeni.satici@tezgah.test` / `Tezgah123!`).
3. Giriş sonrası açılan başvuru formunu doldurun:
   - Mağaza adı: `Deniz Seramik`
   - Ticari unvan / ad soyad: `Deniz Seramik Ltd. Şti.`
   - Vergi no / T.C. kimlik no: `4829163057`
   - Vergi dairesi: `Kadıköy`
   - IBAN: `TR12 3456` (bilerek yanlış)
   - Şehir: `İstanbul`
4. "Başvuruyu gönder"e basın.

## Beklenen

IBAN alanının altında neyin yanlış olduğunu söyleyen bir hata yazısı (örneğin "IBAN geçersiz. TR ile başlayan 26 karakterli IBAN girin.") görünür. Başka alanlarda sorun varsa onlar da kendi altında görünür.

## Gerçekleşen

Hiçbir alanda hata yazısı yok; form sessizce eski hâline dönüyor.

## Ek bilgi

- Geçen hafta başvuru formu hataları gösteriyordu; vergi numarası daha önce kullanılmışsa o alanın altında "Bu vergi numarasıyla kayıtlı bir mağaza var." çıkıyordu.
- Doğru bilgilerle başvuru yapılınca sorun yok, yani yalnızca hatalı gönderimde sessiz kalıyor.
- Müşteri sitesindeki formlarda bu sorunu görmedim.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 4
```

---

## English

# TZ-4142 · Seller application form hides validation errors

| Field | Value |
| --- | --- |
| Ticket | TZ-4142 |
| Level | ● (1/5) |
| Where it shows | Seller panel · Application |
| Reporter | Serkan Aydoğdu, seller relations |
| Priority | Medium |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

A shopkeeper who wants to open a store told us on the phone: "I press the submit button and nothing happens." I tried it myself: the button says "Gönderiliyor…" ("Sending…") for a moment, then returns to "Başvuruyu gönder" ("Submit application"). There is no error text on screen, no red field, and no "application received" message either. I had deliberately typed an incomplete IBAN; I would expect the form to tell me. The shopkeeper spent half an hour trying to find out what was wrong.

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Create a new account at http://localhost:5174/kayit (for example `yeni.satici@tezgah.test` / `Tezgah123!`).
3. After logging in, fill in the application form:
   - Store name (Mağaza adı): `Deniz Seramik`
   - Legal name (Ticari unvan / ad soyad): `Deniz Seramik Ltd. Şti.`
   - Tax number (Vergi no / T.C. kimlik no): `4829163057`
   - Tax office (Vergi dairesi): `Kadıköy`
   - IBAN: `TR12 3456` (deliberately wrong)
   - City (Şehir): `İstanbul`
4. Press "Başvuruyu gönder".

### Expected

An error text under the IBAN field says what is wrong (for example "IBAN geçersiz. TR ile başlayan 26 karakterli IBAN girin."). If other fields have problems, they show their own messages too.

### Actual

No field shows any error text; the form silently goes back to its previous state.

### Additional information

- Last week the application form showed errors; if a tax number was already used, "Bu vergi numarasıyla kayıtlı bir mağaza var." appeared under that field.
- With correct data the application works, so it is silent only on invalid submissions.
- I did not see this problem in the forms on the customer site.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 4
```
