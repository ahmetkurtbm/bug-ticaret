# İpuçları · Bölüm 3 (FE-12)

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Tarayıcıda F5'ten sonra Network sekmesine bakın: ilk satır olan sayfa isteğinin (Doc) durum kodu ve `Server` yanıt başlığı ne? Aynı isteği `curl -i http://localhost:5173/hesabim/siparisler` ile terminalden de görebilirsiniz. Ayrıca `npm run dev:storefront` çalışırken aynı adresin neden sorunsuz açıldığını düşünün; hangi sunucu cevap veriyor?

*EN:* After pressing F5, open the Network tab: what are the status code and the `Server` response header of the first row, the page request (Doc)? You can see the same with `curl -i http://localhost:5173/hesabim/siparisler` in a terminal. Also think about why the same address opens fine while `npm run dev:storefront` is running: which server is answering?

## İpucu 2

Bu bir tek sayfa uygulaması (SPA). `/urun/...` ya da `/hesabim/siparisler` diye bir dosya sunucuda yoktur; bu adresleri tarayıcıdaki React Router yorumlar. Uygulama içinde gezerken sunucuya hiç istek gitmez, o yüzden sorun görünmez. Yenileyince tarayıcı bu adresi sunucudan ister. Container'da bu isteği Nginx karşılıyor; ayarı `frontend/nginx/` altında.

*EN:* This is a single-page application (SPA). There is no file on the server called `/urun/...` or `/hesabim/siparisler`; React Router in the browser interprets those addresses. Navigating inside the app never sends a request to the server, so the problem stays hidden. On refresh the browser asks the server for that address. In the container, Nginx answers that request; its configuration is under `frontend/nginx/`.

## İpucu 3

`frontend/nginx/spa.conf` içindeki `location /` bloğunda `try_files` satırının son parametresi `=404`. Dosya bulunamayınca Nginx'e ne yapması söylenmeli ki React yüklensin? Bu satırı `git log -p -- frontend/nginx/spa.conf` ile geçmişte nasıl olduğuna da bakın. `location /assets/` bloğuna da göz atın: orada var olmayan dosyaya ne oluyor?

*EN:* In the `location /` block of `frontend/nginx/spa.conf`, the last parameter of `try_files` is `=404`. When no file is found, what should Nginx be told to do so that React gets loaded? Look at how this line used to be with `git log -p -- frontend/nginx/spa.conf`. Also look at the `location /assets/` block: what happens there to a file that does not exist?
