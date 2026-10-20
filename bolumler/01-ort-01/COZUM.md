# Çözüm · Bölüm 1 (ORT-01) · Container içinde localhost

## Kök neden

`docker-compose.yml` içinde `api` servisinin `Payments__BaseUrl` ayarı `http://localhost:5090` yapılmış. API bu adresle ödeme servisine istek atıyor. Container içinde `localhost` o container'ın kendisidir; API container'ında 5090 portunu dinleyen bir şey yok. Bağlantı reddediliyor, sipariş kaydediliyor ama ödeme oturumu açılamıyor.

Değişikliği yapan geliştirici şunu düşünmüş (commit `de6ad59`):

> Compose: ödeme servisi adresi geliştirme ayarlarıyla aynı yapıldı
> Yerelde de container'da da aynı adres kullanılsın, iki ayrı değer karışıklık çıkarıyordu.

Düşüncesi anlaşılır: `appsettings.Development.json` içinde adres `http://localhost:5090`, compose'ta `http://payments:8080` idi; iki değer tutarsız göründü. Ama iki değer farklı olmak zorunda. Yerelde API bilgisayarda çalışır ve ödeme servisi bilgisayarın 5090 portundadır. Container'da API ayrı bir ağ alanındadır ve ödeme servisine Docker ağındaki servis adıyla ve container'ın kendi portuyla (`payments:8080`) ulaşır.

## Nasıl bulunur

1. **Belirti:** Ekranda "Ödeme şu anda başlatılamadı". Bu metin kullanıcı içindir. Kaynak kodda aratınca `OrderService` içindeki `PaymentGatewayException` yakalama bloğuna çıkar; orası hatayı loglayıp bu mesajı döner.
2. **Araç: API logu.** `docker compose logs api` çalıştırın. `Ödeme başlatılamadı TZ-2026-...` satırının altında `Ödeme servisine ulaşılamadı.` ve iç istisna olarak `Connection refused (localhost:5090)` benzeri bir mesaj görürsünüz. Adresin `localhost` olması ilk ipucudur.
3. **Araç: container içinden bakmak.** `docker compose exec api printenv Payments__BaseUrl` komutu API'nin gerçekte hangi adresi kullandığını gösterir. Servis adıyla ulaşılabildiğini görmek için `docker compose exec api curl -s http://payments:8080/health` deneyin (imajda `curl` yoksa bu adımı atlayın; compose sağlık kontrolü zaten `curl` kullandığı için genelde vardır).
4. **Dosya:** `docker-compose.yml`, `api` servisi, `Payments__BaseUrl`. `git log -p -- docker-compose.yml` satırın son commit'te `payments:8080`'den `localhost:5090`'a değiştiğini gösterir.
5. **Doğrulama:** Yerelde çalışması (`appsettings.Development.json` içinde `localhost:5090` var) "aynı kod, farklı ayar" tespitini destekler.

**Visual Studio:** Container'da çalışan sürece hata ayıklayıcı bağlamak gerekmez. Logları *Containers* penceresinden (View → Other Windows → Containers) ya da terminalden okuyun.

**VS Code:** Container Tools eklentisinde `api` container'ına sağ tık → *View Logs* ya da *Attach Shell*. Kabukta `printenv Payments__BaseUrl` çalıştırın.

## Düzeltme

```diff
     environment: &dotnet-env
       ASPNETCORE_ENVIRONMENT: Development
       ConnectionStrings__Tezgah: Host=postgres;Port=5432;Database=tezgah;Username=tezgah;Password=tezgah
       Database__MigrateOnStartup: "true"
-      Payments__BaseUrl: http://localhost:5090
+      Payments__BaseUrl: http://payments:8080
```

Uygulamak için `docker compose --profile app up -d` yeterlidir; değişen servis yeniden oluşturulur, imaj yeniden derlenmez.

## Neden böyle düzeltiyoruz

- **İki adres farklıdır, çünkü iki farklı istemci vardır.** Servisler arası çağrıda (API → Ödeme) adres Docker ağındaki servis adı ve container portudur: `http://payments:8080`. Tarayıcının erişeceği adres ise bilgisayarda yayınlanan porttur: `http://localhost:5090`. Bu yüzden `payments` servisinin `Payments__PublicBaseUrl` değeri `localhost` kalır; ödeme sayfası adresini (`checkoutUrl`) müşterinin tarayıcısı açar. Aynı dosyada `Payments__WebhookUrl: http://api:8080/...` de servis adını kullanır.
- **`host.docker.internal:5090` kötü.** Container'dan çıkıp bilgisayara, oradan yayınlanan porttan tekrar container'a döner. Linux'ta ek ayar ister, bilgisayardaki port çakışmalarına bağımlı olur ve servislerin aynı ağda olması fikrini boşa çıkarır.
- **`network_mode: host`.** Port izolasyonunu kaldırır, Docker Desktop'ta (Windows/macOS) beklendiği gibi çalışmaz ve servis adlarıyla erişimi bozar.
- **Adresi koda gömmek.** Adres ortama göre değişir; kod değil ayar olmalı. Zaten ayar ortam değişkeninden okunuyor.

## Bir daha olmaması için

- **Test:** Bu bölümün kontrolü gibi, container'da uçtan uca bir sipariş denemesi CI'da `docker compose --profile app up` sonrası çalıştırılır. Yerelde geçip container'da kırılan hatayı yalnızca bu tür bir test yakalar.
- **Kod inceleme maddesi:** `docker-compose.yml` ya da Dockerfile içinde `localhost`/`127.0.0.1` gören herkes "bu adresi kim kullanıyor, tarayıcı mı başka bir container mı?" diye sormalı. "İki ayar tutarsız görünüyor" tek başına birleştirme sebebi değildir; önce neden farklı olduklarını bulun.
- **Belgeleme:** Compose dosyasında adresin yanına tek satır yorum ("container'lar arası: servis adı + container portu") koyun.
- **İzleme:** `HttpPaymentGateway` hata loguna hedef adresi (`http.BaseAddress`) ekleyin; "ulaşılamadı" satırı hangi adrese gidildiğini kendisi söylesin. API açılışında ödeme servisinin `/health` adresine hazırlık kontrolü yapılabilir.

## İlgili

HATALAR.md · **ORT-01 Container içinde localhost** (katalogdaki örnek veritabanı bağlantısıdır; burada aynı kök neden ödeme servisi adresinde).

## English summary

**Root cause:** the `api` service in `docker-compose.yml` had `Payments__BaseUrl: http://localhost:5090`. Inside a container, `localhost` is the container itself, so the API cannot reach the payment service (connection refused). The commit "Compose: ödeme servisi adresi geliştirme ayarlarıyla aynı yapıldı" tried to unify the local and container values, but they must differ.

**How to find it:** the message "Ödeme şu anda başlatılamadı" -> `docker compose logs api` shows "Ödeme servisine ulaşılamadı" with a connection error to `localhost:5090` -> `docker compose exec api printenv Payments__BaseUrl` -> `git log -p -- docker-compose.yml`.

**Fix:** use the service name and container port: `Payments__BaseUrl: http://payments:8080`. Keep `Payments__PublicBaseUrl` as `http://localhost:5090` on the payments service, because the browser, not another container, opens that address.

**Prevent:** run an end-to-end order in containers in CI, question every `localhost` in compose files during review, and log the target address in connection errors.
