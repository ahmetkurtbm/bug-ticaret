# TZ-4358 · 1.249,90 TL'lik ürün 1,25 TL'ye satılıyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4358 |
| Seviye | ●● (2/5) |
| Modül | Satıcı paneli · Fiyat |
| Bildiren | Ece Demirtaş, satıcı destek uzmanı (bir mağaza sahibinin şikâyetini iletiyor) |
| Öncelik | Yüksek (para kaybı) |
| Ortam | Yerel kurulum: API + `npm run dev:seller`, satıcı paneli http://localhost:5174, Chrome |
| Durum | Açık |

## Ne oldu

Bir mağaza sahibi arayıp şunu anlattı: Ürün fiyatını `1.249,90` yazıp kaydetmiş. Panel hata vermemiş, kayıt başarılı görünmüş. Ürün listesinde ve müşteri sitesinde fiyat `₺1,25` çıkıyor. Müşteri sitesinde ürün gerçekten bu fiyata satışta.

Satıcı şöyle dedi: "Bu hafta güncellemeden sonra `249.90` yazınca artık hata vermiyor, ona teşekkürler. Ama binlik ayraçlı fiyat yazınca bozuldu. `249,90` yazınca sorun yok."

Kendim de denedim, aynı sonucu aldım. Ekranda hata mesajı ya da uyarı çıkmıyor.

## Tekrar etme adımları

1. API'yi başlatın: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
2. Başka bir terminalde: `cd frontend && npm run dev:seller`
3. http://localhost:5174 adresinde `satici@tezgah.test` / `Tezgah123!` ile giriş yapın.
4. Ürünler sayfasından (http://localhost:5174/urunler) bir ürünü açın, bir varyantın fiyatını `1.249,90` yapıp kaydedin.
5. Ürün listesine dönüp fiyata bakın. İsterseniz müşteri sitesinde (`npm run dev:storefront`) ürünü açın.
6. Karşılaştırma için aynı varyanta `249,90` yazıp kaydedin.

## Beklenen

`1.249,90` yazınca fiyat 1.249,90 TL (`₺1.249,90`) olarak kaydedilir. Anlaşılamayan bir girdi varsa panel bunu söyler ve kaydetmez.

## Gerçekleşen

Fiyat `₺1,25` olarak kaydediliyor. Panel hata göstermiyor.

## Ek bilgi

- `249,90` doğru kaydoluyor. `1.249,90` ve `12.500` gibi binlik ayraçlı değerlerde sorun var.
- Mağaza sahibi kaydetmeden önce panelin gösterdiği başka bir uyarı görmemiş.
- Başka satıcılar için de olabilir; bu hafta fiyatını 1.000 TL üstü giren satıcıların listesini henüz çıkarmadık.
- Bu form geçen hafta binlik ayraçlı girişleri doğru kaydediyordu.

## Bitirdiğinizde

Aşağıdaki komut satıcı panelinin bölüm testini çalıştırır (API'nin açık olması gerekmez):

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 12
```

---

## English

# TZ-4358 · A 1,249.90 TL product sells for 1.25 TL

| Field | Value |
| --- | --- |
| Ticket | TZ-4358 |
| Level | ●● (2/5) |
| Module | Seller panel · Price |
| Reporter | Ece Demirtaş, seller support specialist (passing on a store owner's complaint) |
| Priority | High (loss of money) |
| Environment | Local setup: API + `npm run dev:seller`, seller panel at http://localhost:5174, Chrome |
| Status | Open |

### What happened

A store owner called and said: they typed `1.249,90` as the price and saved. The panel showed no error and the save looked successful. The product list and the storefront now show `₺1,25`. The product is really on sale at that price in the storefront.

The seller said: "After this week's update, typing `249.90` no longer gives an error, thanks for that. But prices with a thousands separator are now broken. `249,90` is fine."

I tried it myself and got the same result. No error message or warning appears on screen.

### Steps to reproduce

1. Start the API: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
2. In another terminal: `cd frontend && npm run dev:seller`
3. Open http://localhost:5174 and sign in with `satici@tezgah.test` / `Tezgah123!`.
4. From the products page (http://localhost:5174/urunler), open a product, set a variant's price to `1.249,90` and save.
5. Go back to the product list and look at the price. Optionally open the product in the storefront (`npm run dev:storefront`).
6. For comparison, save `249,90` on the same variant.

### Expected

Typing `1.249,90` saves the price as 1,249.90 TL (`₺1.249,90`). If an input cannot be understood, the panel says so and does not save.

### Actual

The price is saved as `₺1,25`. The panel shows no error.

### Additional information

- `249,90` is saved correctly. The problem is with values that use a thousands separator, such as `1.249,90` and `12.500`.
- The store owner saw no other warning from the panel before saving.
- Other sellers may be affected; we have not yet pulled the list of sellers who entered a price above 1,000 TL this week.
- Last week this form saved thousands-separated prices correctly.

### When you are done

The command below runs the seller panel's chapter test (the API does not need to be running):

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 12
```
