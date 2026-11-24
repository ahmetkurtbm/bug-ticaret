# TZ-4622 · Kupon "kaydedildi" dedi ama listede yok

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4622 |
| Seviye | ● (1/5) |
| Nerede görülüyor | Yönetim paneli · Kampanyalar |
| Bildiren | Pınar Çetin, kampanya uzmanı (pazarlama) |
| Öncelik | Orta |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Hafta sonu kampanyası için panelde yeni bir kupon oluşturmaya çalıştım. "Kuponu oluştur"a basınca form hiçbir uyarı vermeden kapandı; her şey yolunda görünüyordu. Ama Kampanyalar listesine baktığımda yeni kupon yok. Sayfayı yenileyip tekrar baktım, yine yok. Müşterilere kodu duyurmadan önce fark ettim; ama bir meslektaşım aynı şeyi yapıp kodu bülten taslağına yazmış.

Geçen ay yanlışlıkla aynı kodu iki kez girmiştim ve o zaman formda kırmızı bir uyarı çıkmıştı. Bu sefer hiçbir şey çıkmadı.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. http://localhost:5175 adresinde `admin@tezgah.test` / `Tezgah123!` ile giriş yapın.
3. Kampanyalar sayfasında (http://localhost:5175/kampanyalar) **Yeni kupon**'a basın.
4. Kod olarak `KARGOBEDAVA` yazın (bu kod listede zaten var); diğer alanları geçerli değerlerle doldurun ve "Kuponu oluştur"a basın.
5. Formun kapanmasını ve kupon listesini izleyin.

## Beklenen

Bu kod zaten kullanıldığı için kupon oluşturulmaz; form açık kalır ve kod alanının altında nedenini söyleyen bir uyarı görünür.

## Gerçekleşen

Form hiçbir uyarı göstermeden kapanıyor. Panel işlemin başarılı olduğunu varsayıyor; listede yeni bir kupon yok.

## Ek bilgi

- Başka bir kodla (listede olmayan) kupon oluşturmak sorunsuz çalışıyor.
- Aynı kod için geçen ay kırmızı bir uyarı görmüştüm.
- Kupon listesinde bu kodun yalnızca eski kaydı var.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 22
```

---

## English

# TZ-4622 · Coupon said "saved" but is not in the list

| Field | Value |
| --- | --- |
| Ticket | TZ-4622 |
| Level | ● (1/5) |
| Where it shows | Admin panel · Campaigns |
| Reporter | Pınar Çetin, campaign specialist (marketing) |
| Priority | Medium |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

I tried to create a new coupon in the panel for the weekend campaign. When I pressed "Kuponu oluştur" ("Create coupon") the form closed without any warning; everything looked fine. But when I looked at the Campaigns list, the new coupon was not there. I refreshed the page and looked again; still not there. I noticed it before announcing the code to customers, but a colleague did the same thing and wrote the code into the newsletter draft.

Last month I had entered the same code twice by mistake and a red warning appeared on the form at that time. This time nothing appeared.

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Open http://localhost:5175 and sign in with `admin@tezgah.test` / `Tezgah123!`.
3. On the Campaigns page (http://localhost:5175/kampanyalar) press **Yeni kupon** ("New coupon").
4. Type `KARGOBEDAVA` as the code (this code is already in the list); fill the other fields with valid values and press "Kuponu oluştur".
5. Watch whether the form closes, and look at the coupon list.

### Expected

Since this code is already in use, the coupon is not created; the form stays open and a warning under the code field says why.

### Actual

The form closes without showing any warning. The panel assumes the operation succeeded; there is no new coupon in the list.

### Additional information

- Creating a coupon with another code (one that is not in the list) works fine.
- Last month I saw a red warning for the same kind of code.
- The coupon list has only the old record of this code.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 22
```
