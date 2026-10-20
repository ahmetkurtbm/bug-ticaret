# Çözüm · Bölüm 3 (FE-12) · SPA sayfasında yenileyince 404

## Kök neden

`frontend/nginx/spa.conf` içindeki `location /` bloğunda `try_files $uri $uri/ =404;` yazıyor. `/urun/deneme-p42` gibi bir adres için diskte dosya yok, bu yüzden Nginx 404 döner. Tarayıcı React uygulamasını (`index.html`) hiç almadığı için React Router çalışamaz.

Uygulama içinde gezerken sorun görünmez: bağlantıya tıklayınca React Router adresi tarayıcıda değiştirir, sunucuya sayfa isteği gitmez. Yalnızca yenileme, adresi yeni sekmede açma ya da paylaşılan bağlantı sunucuya gider.

Değişikliği yapan geliştirici şunu düşünmüş (commit `adccd8f`):

> Nginx: bulunmayan dosyalar için 404 dönülüyor
>
> Bulunmayan dosya için index.html dönmek tarayıcıda anlaşılmaz hatalara yol açıyordu (ör. eksik bir .js dosyası yerine HTML geliyordu). Artık düz 404 dönülüyor.

Kaygısı gerçek: eksik bir `.js` dosyası yerine HTML gelirse tarayıcı "Unexpected token '<'" gibi anlaşılmaz bir hata verir. Ama bu durum zaten çözülmüş: `/assets/` için ayrı bir `location` var ve orada `try_files $uri =404` kullanılıyor. Dosya adlarında özet (hash) olan her şey `/assets/` altındadır. `location /` ise uygulama adresleri içindir ve dosya değil sayfa bekler.

Yerelde görünmemesinin nedeni: Vite geliştirme sunucusu bilinmeyen her adrese `index.html` döner. Yani hata yalnızca Nginx'li üretim benzeri ortamda ortaya çıkar.

## Nasıl bulunur

1. **Belirti:** Yenileyince "404 Not Found / nginx". Sayfa Nginx'in kendi sayfası; React'in 404 sayfası (`NotFoundPage`) değil. Bu, isteğin React'e hiç ulaşmadığını gösterir.
2. **Araç: Network sekmesi.** F5'ten sonra ilk satır (Doc): durum 404, yanıt başlığında `Server: nginx`. Dev sunucusunda bu satır 200 döner.
3. **Araç: curl.** `curl -i http://localhost:5173/hesabim/siparisler` 404 gösterir. Karşılaştırma: `curl -i http://localhost:5173/` 200. Kök çalışıyor çünkü `$uri/` ile `index.html` bulunuyor.
4. **Soru:** Yerelde neden çalışıyor? `npm run dev` kapalıyken 5173'ü kim dinliyor? Cevap: Nginx container'ı. Dev sunucusu açıkken port çakışırdı.
5. **Dosya:** `frontend/Dockerfile` son satırlarında `COPY nginx/spa.conf /etc/nginx/conf.d/default.conf` var. Ayar `frontend/nginx/spa.conf`. İçeriği doğrulamak için: `docker compose exec storefront cat /etc/nginx/conf.d/default.conf`.
6. **Git:** `git log -p -- frontend/nginx/spa.conf` ayarın son değişiklikte `/index.html`'den `=404`'e dönüştüğünü ve yorumun yeniden yazıldığını gösterir.

**Visual Studio / VS Code:** Bu bölümde kesme noktası gerekmez. Nginx ayarını düzenleyip `docker compose --profile app up -d --build storefront` ile yeniden derleyin; ayar imajın içine kopyalandığı için yeniden başlatmak yetmez, yeniden derlemek gerekir.

## Düzeltme

```diff
     location / {
-        try_files $uri $uri/ =404;
+        try_files $uri $uri/ /index.html;
         add_header Cache-Control "no-cache";
     }
```

Yorum satırlarını da eski hâline getirin: bu blok neden `index.html` dönüyor, bir sonraki kişi bilsin. Ardından `docker compose --profile app up -d --build storefront`.

`/assets/olmayan-dosya-123.js` hâlâ 404 döner, çünkü `location /assets/` bloğu `=404` ile kalır. Kontrol bunu da sınar.

## Neden böyle düzeltiyoruz

- **Her şeyi `index.html`'e yönlendirmek (`/assets/` dahil)** yanlış. Eksik bir `.js` dosyası 200 ve HTML döner; geliştiricinin anlattığı anlaşılmaz hata geri gelir ve önbellekler HTML'i JS diye saklayabilir. Bu yüzden `/assets/` ayrı kalır.
- **`error_page 404 =200 /index.html`** hem uygulama adreslerini hem eksik dosyaları 200 yapar; aynı sorun.
- **Hash router (`/#/urun/...`)** yenileme sorununu kaldırır ama bütün adresleri ve paylaşılan bağlantıları değiştirir, arama motoru ve analitik için kötüdür. Sorun sunucu ayarındadır, istemcide çözmek gereksizdir.
- **Sunucuya her uygulama rotası için ayrı `location` yazmak** rota eklendikçe unutulur. Genel kural "dosya yoksa uygulamayı ver" daha dayanıklıdır; gerçek 404'ü React'teki `NotFoundPage` gösterir.

## Bir daha olmaması için

- **Test:** Bu bölümdeki kontrol gibi iki adımlı bir duman testi (smoke test) CI'da container'a karşı çalışsın: derin bağlantı 200 + `id="root"` içermeli, olmayan `.js` 404 olmalı.
- **Ortam farkı:** Dev sunucusu üretimdeki sunucu değildir. Yönlendirme, önbellek başlıkları, sıkıştırma gibi sunucu davranışlarını değiştiren her PR'ı `docker compose --profile app up --build` ile deneyin.
- **Kod inceleme maddesi:** SPA sunucu ayarında `try_files` son parametresini `=404` yapan değişikliği "derin bağlantıyı yenileyerek denediniz mi?" diye sorgulayın.
- **İzleme:** Kullanıcı sayfalarında (HTML döndürmesi gereken yollarda) Nginx 404 oranı için uyarı koyun; normalde sıfıra yakın olmalıdır.

## İlgili

HATALAR.md · **FE-12 SPA sayfasında yenileyince 404**

## English summary

**Root cause:** `frontend/nginx/spa.conf` had `try_files $uri $uri/ =404;` in `location /`. SPA routes like `/urun/...` are not files, so Nginx returned 404 on refresh or direct links and React never loaded. The Vite dev server always returns `index.html`, which is why it works locally. The developer (commit "Nginx: bulunmayan dosyalar için 404 dönülüyor") wanted to avoid HTML being served for missing `.js` files, but `/assets/` already has its own `try_files $uri =404`.

**How to find it:** Network tab -> the Doc request returns 404 with `Server: nginx` -> `curl -i` on a deep link -> `frontend/nginx/spa.conf` and `git log -p` on it.

**Fix:** `try_files $uri $uri/ /index.html;` in `location /`, keep `/assets/` at `=404`, rebuild the image (`docker compose --profile app up -d --build storefront`).

**Prevent:** a smoke test for a deep link (200) and a missing asset (404), test server-level changes in the container, and challenge `=404` in SPA fallbacks during review.
