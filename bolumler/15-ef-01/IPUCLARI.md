# İpuçları · Bölüm 15 (EF-01)

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Süreye değil, sorgu sayısına bakın. Development ortamında API her yanıta `Server-Timing` başlığı ekler. Siparişler sayfasını açın, tarayıcıda Network → siparişler isteği → **Timing** bölümünde `db;dur=..;desc="N sorgu"` değerini okuyun. Sayfa boyutunu değiştirip (`Tezgah.Api.http` dosyasındaki isteğe `pageSize=5`, sonra `pageSize=50` yazın) N'nin nasıl değiştiğini izleyin. İsterseniz `appsettings.Development.json` içinde `Microsoft.EntityFrameworkCore.Database.Command` günlük düzeyini `Information` yapıp API konsolunda SQL'leri de okuyabilirsiniz.

*EN:* Look at the query count, not the time. In the Development environment the API adds a `Server-Timing` header to every response. Open the Orders page, then in the browser go to Network, select the orders request and read `db;dur=..;desc="N sorgu"` in the **Timing** section. Change the page size (in `Tezgah.Api.http` try `pageSize=5`, then `pageSize=50`) and watch how N changes. If you like, set the `Microsoft.EntityFrameworkCore.Database.Command` log level to `Information` in `appsettings.Development.json` and read the SQL statements in the API console too.

## İpucu 2

Sorgu sayısı sayfadaki satır sayısıyla orantılı büyüyorsa her satır için ayrı bir sorgu çalışıyor demektir. Buna N+1 sorgu sorunu denir: 1 sorgu listeyi, N sorgu da her satırın ilişkili verisini getirir. Sipariş listesini hazırlayan servisi bulun (`Application/Admin`) ve ilişkili verinin (müşteri, sipariş kalemleri) nasıl yüklendiğine bakın. Soru şu: ilişkili veri tek sorguda mı geliyor, yoksa bir döngünün içinde mi?

*EN:* If the query count grows in proportion to the number of rows on the page, a separate query is running for each row. This is called the N+1 query problem: 1 query fetches the list and N queries fetch each row's related data. Find the service that prepares the order list (`Application/Admin`) and look at how the related data (customer, order items) is loaded. The question is: does the related data arrive in one query, or inside a loop?

## İpucu 3

`backend/src/Tezgah.Application/Admin/AdminOrderService.cs` içindeki `ListAsync`, siparişleri çektikten sonra `foreach` içinde her sipariş için `db.Users...SingleAsync` ve `db.OrderItems...ToListAsync` çalıştırıyor. Yani sayfa başına 2 + 2N sorgu var. Müşteri bilgisi ile kalem toplamları, liste sorgusunun kendisinde `Select` ile (projeksiyon) istenmeli; veritabanı bunları aynı sorguda birleştirir. Döngüdeki `await`'ler kalkınca sorgu sayısı satır sayısına bağlı olmaktan çıkar.

*EN:* `ListAsync` in `backend/src/Tezgah.Application/Admin/AdminOrderService.cs` fetches the orders and then, inside a `foreach`, runs `db.Users...SingleAsync` and `db.OrderItems...ToListAsync` for every order. That is 2 + 2N queries per page. The customer data and the item totals should be requested in the list query itself with `Select` (a projection); the database joins them in the same query. Once the `await`s in the loop are gone, the query count no longer depends on the number of rows.
