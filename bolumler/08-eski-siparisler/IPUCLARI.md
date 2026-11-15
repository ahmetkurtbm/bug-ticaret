# İpuçları · Bölüm 8 (NET-03)

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Ekrandaki mesaj genel bir 500 mesajıdır; ayrıntı API'nin logundadır. Etkilenen siparişi açın ve API'nin çalıştığı terminale bakın. `Beklenmeyen hata: /api/orders/...` satırının altındaki istisna türünü ve yığın izindeki (stack trace) ilk `Tezgah` ile başlayan satırı okuyun. Satırın sonundaki `line` numarası sizi kodda tam yere götürür.

*EN:* The on-screen message is a generic 500 message; the details are in the API's log. Open an affected order and look at the terminal the API runs in. Read the exception type below the `Beklenmeyen hata: /api/orders/...` line and the first stack trace line that starts with `Tezgah`. The `line` number at the end of that line takes you to the exact place in the code.

## İpucu 2

Hata yalnızca bazı siparişlerde olduğuna göre sorun koddan çok veride ya da koda bu veriyle ilgili yazılmış bir varsayımda. Hatalı ve hatasız siparişlerin farkını arayın: veritabanında (`docker exec -it tezgah-postgres-1 psql -U tezgah`) etkilenen siparişin `shipping_` ile başlayan sütunlarına bakın. İstisna türü `NullReferenceException`. Satırdaki hangi ifade `null` olabilir?

*EN:* Since the error occurs only for some orders, the problem is less the code than the data, or an assumption the code makes about that data. Look for the difference between failing and working orders: in the database (`docker exec -it tezgah-postgres-1 psql -U tezgah`), check the columns starting with `shipping_` for an affected order. The exception type is `NullReferenceException`. Which expression on that line can be `null`?

## İpucu 3

`backend/src/Tezgah.Application/Orders/OrderService.cs` içinde `GetAsync` metodunda `var a = order.ShippingAddress!;` satırı adresin hiçbir zaman `null` olmadığını varsayıyor; sonraki satır `a.FullName` ile patlıyor. Sondaki `!` derleyiciyi susturur ama çalışma anında hiçbir şey denetlemez. Eski sistemden taşınan siparişlerin adresi boş. `OrderDetailDto.ShippingAddress` zaten `null` kabul eden bir türde (`OrderAddressDto?`) ve müşteri sitesi `null` durumunu gösteriyor. Önceki hâli için `git log -p -- backend/src/Tezgah.Application/Orders/OrderService.cs`.

*EN:* In `backend/src/Tezgah.Application/Orders/OrderService.cs`, in the `GetAsync` method, the line `var a = order.ShippingAddress!;` assumes the address is never `null`; the next line crashes on `a.FullName`. The trailing `!` silences the compiler but checks nothing at runtime. Orders migrated from the old system have no address. `OrderDetailDto.ShippingAddress` is already a type that accepts `null` (`OrderAddressDto?`), and the storefront handles the `null` case. For the previous version, see `git log -p -- backend/src/Tezgah.Application/Orders/OrderService.cs`.
