# İpuçları · Bölüm 6 (NET-01)

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Network sekmesinde başarısız isteğin hemen önündeki `OPTIONS` isteğine (ön kontrol, preflight) bakın. Yanıt başlıklarında `Access-Control-Allow-Origin` var mı? Aynı kontrolü terminalden yapabilirsiniz: `curl -i -X OPTIONS http://localhost:5080/api/seller/dashboard -H "Origin: http://localhost:5174" -H "Access-Control-Request-Method: GET"`. Origin değerini 5173 yaparak da deneyin ve iki yanıtı karşılaştırın.

*EN:* In the Network tab, look at the `OPTIONS` request (the preflight) just before the failed request. Is there an `Access-Control-Allow-Origin` header in the response? You can run the same check from a terminal: `curl -i -X OPTIONS http://localhost:5080/api/seller/dashboard -H "Origin: http://localhost:5174" -H "Access-Control-Request-Method: GET"`. Try it with the Origin set to 5173 as well and compare the two responses.

## İpucu 2

CORS bir tarayıcı kuralıdır; sunucu isteği alır, ama tarayıcı yanıtta izin başlığı yoksa sonucu sayfaya vermez. Bu yüzden sunucu logunda hata yok ve `curl` çalışıyor. Sunucu hangi adreslere izin verdiğini yapılandırmadan okuyor. Üç uygulamadan yalnızca biri sorunlu olduğuna göre, o adresin izin listesinde nasıl yazıldığına bakın; adres karşılaştırması harfi harfine yapılır.

*EN:* CORS is a browser rule: the server receives the request, but the browser withholds the result from the page if the response lacks the permission header. That is why the server log has no error and `curl` works. The server reads the allowed addresses from configuration. Since only one of the three apps is affected, look at how that address is written in the allow list; the comparison is exact, character by character.

## İpucu 3

`backend/src/Tezgah.Api/appsettings.Development.json` içindeki `Cors:AllowedOrigins` listesinde `http://localhost:5174/` yazıyor; sonda eğik çizgi var. Tarayıcının gönderdiği `Origin` başlığı ise `http://localhost:5174` biçimindedir (yol ve eğik çizgi içermez), bu yüzden eşleşme olmaz. Ayarı düzelttikten sonra API'yi yeniden başlatmanız gerekir: CORS politikası açılışta kurulur.

*EN:* The `Cors:AllowedOrigins` list in `backend/src/Tezgah.Api/appsettings.Development.json` contains `http://localhost:5174/` with a trailing slash. The `Origin` header the browser sends looks like `http://localhost:5174` (no path, no slash), so there is no match. After correcting the setting you must restart the API: the CORS policy is built at startup.
