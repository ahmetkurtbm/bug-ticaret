# İpuçları · Bölüm 27

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Önce e-postanın hiç oluşup oluşmadığını ayırın. E-postaları üreten Bildirim servisinin logunu okuyun (`docker compose logs notifications`) ve kargo siparişinden sonra yeni bir satır var mı bakın. Servis mesajı hiç almıyorsa sorun e-posta gönderiminde değil, mesajın servise ulaşmasındadır; o zaman bakışınızı API tarafına çevirin.

*EN:* First separate whether the email is ever created. Read the log of the Notifications service, which produces the emails (`docker compose logs notifications`), and check whether there is a new line after the shipping. If the service never receives the message, the problem is not in sending the email but in the message reaching the service; then turn your attention to the API side.

## İpucu 2

API logunda (`docker compose logs api`) kargo anından sonra tekrar tekrar aynı yayın hatasının çıktığını göreceksiniz. Siparişin yanında bir mesaj kaydı da yazılıyor: `docker exec -it tezgah-postgres-1 psql -U tezgah` ile `outbox_messages` tablosuna bakın; `published_at` boş kalan kayıtlar ve `type` sütunu ne diyor? Mesaj aracının (RabbitMQ) yönetim ekranına da bakabilirsiniz: http://localhost:15680, kullanıcı/şifre `docker-compose.yml` içinde yazıyor. `tezgah.events` aracı kuruluşunu ve bağlı kuyrukları inceleyin.

*EN:* In the API log (`docker compose logs api`) you will see the same publish error repeating after the shipping. A message record is also written next to the order: look at the `outbox_messages` table with `docker exec -it tezgah-postgres-1 psql -U tezgah`; what do the rows with an empty `published_at` and the `type` column say? You can also look at the message broker's (RabbitMQ) management screen: http://localhost:15680, the user/password are in `docker-compose.yml`. Inspect the `tezgah.events` exchange and the queues bound to it.

## İpucu 3

`outbox_messages` içindeki kargo kaydının `type` değeri ile Bildirim servisinin dinlediği adları karşılaştırın. Adı üreten yer `backend/src/Tezgah.Application/Sellers/SellerOrderService.cs` içindeki kargoya verme metodu (`Outbox.Add(...)` satırı); adı tanımlayan yer `backend/src/Tezgah.Contracts/Events/IntegrationEvents.cs`. Dinleyen taraf ise `backend/src/Tezgah.Notifications/Messaging/NotificationConsumer.cs`: kuyruğu hangi adlara bağlıyor, `Logic/MessageTypeResolver.cs` hangi adları tanıyor? İki taraf aynı adı konuşuyor mu? Önceki hâli için `git log -p -- backend/src/Tezgah.Application/Sellers/SellerOrderService.cs`.

*EN:* Compare the `type` value of the shipping row in `outbox_messages` with the names the Notifications service listens for. The place that produces the name is the shipping method in `backend/src/Tezgah.Application/Sellers/SellerOrderService.cs` (the `Outbox.Add(...)` line); the place that defines it is `backend/src/Tezgah.Contracts/Events/IntegrationEvents.cs`. The listening side is `backend/src/Tezgah.Notifications/Messaging/NotificationConsumer.cs`: which names does it bind the queue to, and which names does `Logic/MessageTypeResolver.cs` recognise? Do the two sides speak the same name? For the previous version, see `git log -p -- backend/src/Tezgah.Application/Sellers/SellerOrderService.cs`.
