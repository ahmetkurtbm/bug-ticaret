# İpuçları · Bölüm 1 (ORT-01)

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Ekrandaki mesaj kullanıcıya yöneliktir, nedeni söylemez. Nedeni API'nin logu söyler. Sipariş verdikten hemen sonra `docker compose logs api` çıktısının sonuna bakın ve "Ödeme" geçen satırları arayın. Hata satırının altındaki iç içe istisna (inner exception) mesajını da okuyun.

*EN:* The on-screen message is for users and does not give the reason; the API log does. Right after placing an order, look at the end of `docker compose logs api` and search for lines mentioning "Ödeme" (payment). Also read the inner exception message under the error line.

## İpucu 2

Sorun kodda değil, ayarda. Aynı kod yerelde çalışıyor, container'da çalışmıyor; yani fark ortamın ayarlarında. API'nin ödeme servisine hangi adresle gittiğini belirleyen ayarı bulun ve yerelde (`appsettings.Development.json`) ile container'da (`docker-compose.yml`) aynı olup olmadığına bakın. Bir container'ın içinde `localhost` kelimesinin neyi gösterdiğini düşünün.

*EN:* The problem is in configuration, not code. The same code works locally and fails in the container, so the difference is in the environment settings. Find the setting that decides which address the API uses to reach the payment service, and compare it between `appsettings.Development.json` (local) and `docker-compose.yml` (container). Think about what the word `localhost` points to inside a container.

## İpucu 3

`docker-compose.yml` içinde `api` servisinin `Payments__BaseUrl` ortam değişkenine bakın. Değeri `localhost:5090`; container içinde bu adres API container'ının kendisidir ve orada 5090'ı dinleyen kimse yok. `git log -p -- docker-compose.yml` bu satırın yakın zamanda değiştiğini gösterir. Aynı dosyada `payments` servisinin `PublicBaseUrl` ayarının neden `localhost` kaldığını da düşünün; o adresi API değil tarayıcı kullanıyor.

*EN:* In `docker-compose.yml`, look at the `Payments__BaseUrl` environment variable of the `api` service. Its value is `localhost:5090`; inside the container that address is the API container itself, and nothing listens on 5090 there. `git log -p -- docker-compose.yml` shows this line changed recently. Also think about why the `payments` service's own `PublicBaseUrl` setting rightfully stays `localhost` in the same file: that address is used by the browser, not by the API.
