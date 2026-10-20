# TZ-4138 · Sayfayı yenileyince 404

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4138 |
| Seviye | ● (1/5) |
| Modül | Nginx · Müşteri sitesi |
| Bildiren | Ece Yalçın, müşteri hizmetleri |
| Öncelik | Yüksek |
| Ortam | Docker Compose (`--profile app`), müşteri sitesi http://localhost:5173, Chrome |
| Durum | Açık |

## Ne oldu

Bir müşteri, ürün sayfasının bağlantısını arkadaşına gönderdiğini, arkadaşının bağlantıya tıklayınca boş bir sayfa gördüğünü söyledi. Ben de denedim: sitede gezerken her şey düzgün, ama bir ürün sayfasındayken F5'e basınca ya da adresi yeni sekmeye yapıştırınca şu çıkıyor:

> 404 Not Found
> nginx

Aynı şey "Hesabım → Siparişlerim" sayfasında da oluyor. Ana sayfayı yenilemek sorun çıkarmıyor.

## Tekrar etme adımları

1. Yerelde `npm run dev:storefront` kapalı olsun (5173 portunu kullanmasın).
2. `docker compose --profile app up -d --build storefront api`
3. http://localhost:5173 adresini açın, bir ürüne tıklayın (adres `/urun/...` olur).
4. F5'e basın. Alternatif: adres çubuğundaki adresi kopyalayıp yeni sekmede açın.
5. İsterseniz `musteri@tezgah.test` / `Tezgah123!` ile giriş yapıp http://localhost:5173/hesabim/siparisler adresini doğrudan açın.

## Beklenen

Sayfa yenilenince aynı ürün (ya da sipariş listesi) yeniden açılır.

## Gerçekleşen

Tarayıcı "404 Not Found" gösterir; site hiç yüklenmez.

## Ek bilgi

- Geliştiricilerin makinesinde `npm run dev` ile açınca aynı adresler yenilenince sorunsuz açılıyor, "bende çalışıyor" diyorlar.
- Bu davranış yalnızca container'dan açılan sürümde var.
- Önceki sürümde yenileme sorunu yoktu.

## Bitirdiğinizde

Container'ı düzeltmeyi yeniden derleyip başlattıktan sonra:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 3
```

Kontrol hem `/hesabim/siparisler` adresinin doğrudan açıldığını hem de var olmayan bir dosya adresinin hâlâ 404 döndüğünü sınar.

---

## English

# TZ-4138 · Refreshing a page returns 404

| Field | Value |
| --- | --- |
| Ticket | TZ-4138 |
| Level | ● (1/5) |
| Module | Nginx · Storefront |
| Reporter | Ece Yalçın, customer service |
| Priority | High |
| Environment | Docker Compose (`--profile app`), storefront at http://localhost:5173, Chrome |
| Status | Open |

### What happened

A customer said they sent a product page link to a friend and the friend saw an empty page after clicking it. I tried it myself: browsing the site works fine, but on a product page, pressing F5 or pasting the address into a new tab gives:

> 404 Not Found
> nginx

The same happens on "My Account → My Orders". Refreshing the home page causes no problem.

### Steps to reproduce

1. Make sure `npm run dev:storefront` is not running (it must not use port 5173).
2. `docker compose --profile app up -d --build storefront api`
3. Open http://localhost:5173 and click a product (the address becomes `/urun/...`).
4. Press F5. Alternatively copy the address and open it in a new tab.
5. Optionally log in as `musteri@tezgah.test` / `Tezgah123!` and open http://localhost:5173/hesabim/siparisler directly.

### Expected

After refreshing, the same product (or the order list) opens again.

### Actual

The browser shows "404 Not Found"; the site does not load at all.

### Additional information

- When developers run it with `npm run dev`, the same addresses open fine after a refresh; they say "works on my machine".
- This only happens in the version served from the container.
- The previous version did not have the refresh problem.

### When you are done

After rebuilding and restarting the container with your fix:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 3
```

The check verifies that `/hesabim/siparisler` opens directly, and also that the address of a file that does not exist still returns 404.
