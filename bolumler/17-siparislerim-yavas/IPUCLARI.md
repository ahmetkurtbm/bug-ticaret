# İpuçları · Bölüm 17 (DB-01)

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Yavaş olan şeyin kodun mu yoksa tek bir SQL sorgusunun mu olduğunu veritabanına sorun. API'nin SQL günlüğünü açın (`Microsoft.EntityFrameworkCore.Database.Command` düzeyini `Information` yapın), Siparişlerim isteğinin çalıştırdığı `SELECT ... FROM orders ... WHERE user_id = ...` sorgusunu kopyalayın ve `docker exec -it tezgah-postgres-1 psql -U tezgah` ile bağlanıp başına `EXPLAIN (ANALYZE, BUFFERS)` ekleyerek çalıştırın. Çıktı sorgunun nasıl çalıştığını (planı) ve nerede zaman harcadığını gösterir.

*EN:* Ask the database whether the slow part is your code or a single SQL query. Turn on the API's SQL log (set `Microsoft.EntityFrameworkCore.Database.Command` to `Information`), copy the `SELECT ... FROM orders ... WHERE user_id = ...` query that the My Orders request runs, connect with `docker exec -it tezgah-postgres-1 psql -U tezgah` and run it with `EXPLAIN (ANALYZE, BUFFERS)` in front. The output shows how the query runs (the plan) and where it spends time.

## İpucu 2

Plandaki en iç düğüme bakın. `Seq Scan on orders` görüyorsanız PostgreSQL tablonun tüm satırlarını okuyup `Rows Removed by Filter` ile atıyor demektir. Sorgu `WHERE user_id = ... ORDER BY placed_at DESC LIMIT ...` biçiminde; bunu hızlandıracak bir index hangi sütunlardan, hangi sırayla oluşur? `\d orders` ile tablodaki mevcut index'leri listeleyin ve bu sorguya uyan var mı bakın. Sonra `git log` ve migration klasörüne bakarak son sürümde index'lerle ilgili ne olduğunu araştırın.

*EN:* Look at the innermost node of the plan. If you see `Seq Scan on orders`, PostgreSQL reads every row of the table and throws most of them away (`Rows Removed by Filter`). The query has the form `WHERE user_id = ... ORDER BY placed_at DESC LIMIT ...`; which columns, in which order, would make an index that speeds it up? List the existing indexes with `\d orders` and see whether any of them fits this query. Then use `git log` and the migrations folder to find out what happened to indexes in the latest release.

## İpucu 3

Son migration `RemoveUnusedIndexes`, `orders` tablosundaki `ix_orders_user_id_placed_at` index'ini (`user_id`, `placed_at`) kaldırıyor. Commit "kullanılmayan" demiş ama Siparişlerim sorgusu bu index'i kullanıyordu; EF sorgusu koddaki bir SQL metni olmadığı için aramayla bulunmamıştı. Düzeltme: o migration'ı silmeyin ya da düzenlemeyin (yayınlanmış bir migration geçmişin parçasıdır); index'i geri getiren yeni bir migration ekleyin ve `OrderConfiguration.cs` içindeki `HasIndex(x => new { x.UserId, x.PlacedAt })` satırını geri koyun.

*EN:* The latest migration, `RemoveUnusedIndexes`, drops the `ix_orders_user_id_placed_at` index (`user_id`, `placed_at`) on the `orders` table. The commit called it "unused", but the My Orders query used it; an EF query is not a SQL string in the code, so searching did not find it. The fix: do not delete or edit that migration (a published migration is part of history); add a new migration that brings the index back and restore the `HasIndex(x => new { x.UserId, x.PlacedAt })` line in `OrderConfiguration.cs`.
