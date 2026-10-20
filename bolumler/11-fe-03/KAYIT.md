# TZ-4311 · Adet butonuna çok basınca sepet yanlış adet kaydediyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4311 |
| Seviye | ●● (2/5) |
| Modül | Müşteri sitesi · Sepet |
| Bildiren | Derya Koç, müşteri deneyimi uzmanı |
| Öncelik | Orta |
| Ortam | Yerel kurulum: API (`--launch-profile http`) + `npm run dev:storefront`, müşteri sitesi http://localhost:5173, Chrome |
| Durum | Açık |

## Ne oldu

Sepette bir ürünün adedini artırmak için `+` düğmesine arka arkaya hızlıca 4 kez bastım. Adedin 5 olmasını bekliyordum. Ekranda `2` yazdı. Sayfayı yenileyince de `2` kaldı, yani sunucuya da 2 gitmiş. Ekranda hata mesajı çıkmadı, konsolda da kırmızı bir şey görmedim.

Sonra yavaş yavaş denedim: bir kez bastım, bekledim, tekrar bastım. Adet yine 2'nin üstüne çıkmadı gibi geldi. Aynı şey `−` düğmesinde de var gibi, ama onu iyi denemedim.

## Tekrar etme adımları

1. API'yi başlatın: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
2. Başka bir terminalde: `cd frontend && npm run dev:storefront`
3. http://localhost:5173 adresinde `musteri@tezgah.test` / `Tezgah123!` ile giriş yapın.
4. Stoğu yeterli herhangi bir ürünü sepete ekleyin (adedi 1 olsun) ve http://localhost:5173/sepet sayfasını açın.
5. `+` düğmesine hızlıca 4 kez basın. Ekrandaki adedi not edin.
6. Sayfayı yenileyin (F5) ve adedi karşılaştırın.

## Beklenen

Her basış adedi 1 artırır. 4 basıştan sonra adet 5 olur, yenileyince de 5 kalır. Arka arkaya basışlar tek istekle sunucuya gider.

## Gerçekleşen

4 basıştan sonra adet 2 görünüyor ve sunucuya 2 kaydediliyor. Sayfayı yenileyince de 2.

## Ek bilgi

- Ürünün stoğu yeterli; en fazla adet sınırına ulaşmadım.
- Bu ekran geçen hafta doğru çalışıyordu, QA'de de sorun yoktu. Sorun bu hafta fark edildi.
- Başka bir tarayıcıda (Firefox) da aynı.
- Sepette birden fazla satır olunca her satırda aynı şey oluyor.

## Bitirdiğinizde

Aşağıdaki komut müşteri sitesinin bölüm testini çalıştırır (API'nin açık olması gerekmez):

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 11
```

---

## English

# TZ-4311 · Cart saves the wrong quantity after fast clicks

| Field | Value |
| --- | --- |
| Ticket | TZ-4311 |
| Level | ●● (2/5) |
| Module | Storefront · Cart |
| Reporter | Derya Koç, customer experience specialist |
| Priority | Medium |
| Environment | Local setup: API (`--launch-profile http`) + `npm run dev:storefront`, storefront at http://localhost:5173, Chrome |
| Status | Open |

### What happened

In the cart I pressed the `+` button four times in a row, quickly, to raise a product's quantity. I expected 5. The screen showed `2`. After I refreshed the page it was still `2`, so the server received 2 as well. There was no error message on screen and I saw nothing red in the console.

Then I tried slowly: one press, wait, press again. The quantity still did not seem to go above 2. It looks the same for the `−` button, but I did not test that properly.

### Steps to reproduce

1. Start the API: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
2. In another terminal: `cd frontend && npm run dev:storefront`
3. Open http://localhost:5173 and sign in with `musteri@tezgah.test` / `Tezgah123!`.
4. Add any product with enough stock to the cart (quantity 1) and open http://localhost:5173/sepet.
5. Press the `+` button quickly 4 times. Note the quantity on screen.
6. Refresh the page (F5) and compare the quantity.

### Expected

Each press raises the quantity by 1. After 4 presses the quantity is 5 and it stays 5 after a refresh. Presses in quick succession go to the server as one request.

### Actual

After 4 presses the quantity shows 2 and 2 is saved on the server. After a refresh it is still 2.

### Additional information

- The product has enough stock; I did not reach the maximum quantity.
- This screen worked last week and QA found no problem. We noticed it this week.
- Same in another browser (Firefox).
- With several lines in the cart, the same thing happens on every line.

### When you are done

The command below runs the storefront's chapter test (the API does not need to be running):

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 11
```
