# TZ-4640 · "Ödemeye geç"e iki kez basınca iki sipariş açıldı

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4640 |
| Seviye | ● (1/5) |
| Nerede görülüyor | Müşteri sitesi · Ödeme |
| Bildiren | Orhan Taş, müşteri hizmetleri |
| Öncelik | Yüksek |
| Ortam | Geliştirme ortamı (Docker Compose), Chrome |
| Durum | Açık |

## Ne oldu

Bir müşteri aradı: "Ödeme sayfasında 'Ödemeye geç'e bastım, sayfa biraz bekledi, ben de bir daha bastım. Şimdi Siparişlerim'de aynı sepetten iki sipariş görüyorum, ikisi de ödeme bekliyor. İki kez ücretlendirilecek miyim?" Aynı şikâyet son iki günde üç müşteriden daha geldi; hepsi bağlantının yavaş olduğu anlarda.

Ben de denedim. Bağlantı yavaşken düğmeye art arda iki-üç kez basınca Siparişlerim'de aynı tutarda ve aynı kalemlerle birden fazla "Ödeme bekleniyor" siparişi oluşuyor. Tek basışta sorun yok.

## Tekrar etme adımları

1. Sistem çalışıyor olsun: bölümü başlattıktan sonra `docker compose --profile app up -d --build` (bkz. [Başlarken](../../docs/BASLARKEN.md)).
2. http://localhost:5173 adresinde `musteri@tezgah.test` / `Tezgah123!` ile giriş yapın (örnek veri hesaplarının şifresi hepsinde aynıdır).
3. Sepete bir ürün ekleyip Ödeme sayfasına gidin. Yavaş bağlantıyı taklit etmek için F12 → Network sekmesinde ağ hızını "Slow 3G" yapın; hatayı yakalamak daha kolay olur.
4. Bir adres seçip "Ödemeye geç" düğmesine hızlıca iki-üç kez basın.
5. Hesabım → Siparişlerim sayfasını açın.

## Beklenen

Tek bir sipariş oluşur; düğmeye kaç kez basılırsa basılsın Siparişlerim'de aynı sepetten yalnızca bir sipariş görünür.

## Gerçekleşen

Basış sayısı kadar sipariş oluşuyor; Siparişlerim'de aynı sepetin birden fazla "Ödeme bekleniyor" kaydı var.

## Ek bilgi

- Tek basışta sorun yok.
- Yavaş bağlantıda ya da hızlı art arda basışta görülüyor.
- Fazla siparişlerin tutarı ve kalemleri birbirinin aynısı.
- Bunu geçen hafta fark etmemiştik; ödeme sayfasında daha önce böyle bir şikâyet gelmemişti.

## Bitirdiğinizde

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 23
```

---

## English

# TZ-4640 · Two orders were created after pressing "Ödemeye geç" twice

| Field | Value |
| --- | --- |
| Ticket | TZ-4640 |
| Level | ● (1/5) |
| Where it shows | Storefront · Checkout |
| Reporter | Orhan Taş, customer service |
| Priority | High |
| Environment | Development environment (Docker Compose), Chrome |
| Status | Open |

### What happened

A customer called: "On the checkout page I pressed 'Ödemeye geç' ('Proceed to payment'), the page waited a bit, so I pressed again. Now I see two orders from the same cart in My Orders, both waiting for payment. Will I be charged twice?" The same complaint came from three more customers in the last two days, all at moments when the connection was slow.

I tried it too. When the connection is slow and I press the button two or three times in a row, My Orders gets several "waiting for payment" orders with the same total and the same items. A single press is fine.

### Steps to reproduce

1. The system is running: after starting the chapter, `docker compose --profile app up -d --build` (see [Başlarken](../../docs/BASLARKEN.md)).
2. Log in at http://localhost:5173 with `musteri@tezgah.test` / `Tezgah123!` (all sample accounts share the same password).
3. Add a product to the cart and go to the checkout page. To imitate a slow connection, set the network speed to "Slow 3G" in F12 → Network; the bug is easier to catch that way.
4. Select an address and press the "Ödemeye geç" button quickly two or three times.
5. Open My Account → My Orders.

### Expected

A single order is created; no matter how many times the button is pressed, My Orders shows only one order for the same cart.

### Actual

As many orders are created as the number of presses; My Orders has several "waiting for payment" records for the same cart.

### Additional information

- A single press is fine.
- It shows on a slow connection or with quick repeated presses.
- The extra orders have identical totals and items.
- We did not notice it last week; there had been no such complaint about the checkout page before.

### When you are done

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 23
```
