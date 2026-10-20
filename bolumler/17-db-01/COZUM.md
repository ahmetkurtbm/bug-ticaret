# Çözüm · Bölüm 17 (DB-01) · "Kullanılmayan" index'i kaldırmak

## Kök neden

Dal, `RemoveUnusedIndexes` adlı bir migration ekliyor (`backend/src/Tezgah.Infrastructure/Persistence/Migrations/20261111041134_RemoveUnusedIndexes.cs`). Migration `orders` tablosundaki `ix_orders_user_id_placed_at` index'ini düşürüyor, `OrderConfiguration.cs` içinden de `b.HasIndex(x => new { x.UserId, x.PlacedAt })` satırı silinmiş:

```csharp
migrationBuilder.DropIndex(name: "ix_orders_user_id_placed_at", table: "orders");
```

"Siparişlerim" sayfasının sorgusu (`OrderService.ListAsync`) özünde şudur:

```sql
SELECT o.id, o.order_number, o.status, o.placed_at, o.grand_total
  FROM orders o
 WHERE o.user_id = @userId
 ORDER BY o.placed_at DESC, o.id DESC
 LIMIT 10;
```

Hem `WHERE user_id = ...` hem `ORDER BY placed_at DESC` aynı index'le karşılanıyordu: `(user_id, placed_at)`. Index gidince tabloda `user_id` ile başlayan, filtresiz bir index kalmadı (`ix_orders_user_id_idempotency_key` kısmi/filtreli bir index'tir: yalnızca `idempotency_key IS NOT NULL` satırları içerir ve bu sorgu için kullanılamaz). PostgreSQL de tablonun hepsini okuyup `user_id` ile süzmeye (Seq Scan) ve ardından sıralamaya mecbur kaldı. Süre tablo büyüklüğüyle doğru orantılıdır: 50 bin siparişte milisaniyeler, 1 milyon siparişte saniyeler. Aynı sayfadaki toplam sipariş sayısı sorgusu (`COUNT(*) ... WHERE user_id = ...`) de aynı index'e dayanıyordu.

Değişikliği yapan geliştirici şunu düşünmüş (commit `cc518ae`):

> Kullanılmayan index'ler kaldırıldı
> orders tablosu en çok yazılan tablo; her index her INSERT'te güncelleniyor.
> Rapor sorguları (status, placed_at) index'ini kullanıyor, bu index'i kullanan sorgu görmedim.

Kaygı gerçek: her index her yazmada güncellenir (yazma maliyeti, write amplification), kullanılmayan index saf maliyettir. Hata, "görmedim" ifadesinde. Sipariş sorgusu LINQ ile yazılmıştır; SQL metni kodda geçmez ve index adı hiçbir yerde anılmaz. Koda "ix_orders_user_id_placed_at" ya da `user_id` diye arayarak bu sorgu bulunamaz. "Kullanmayan sorgu görmedim" bir kanıt değil, kanıt eksikliğidir.

## Nasıl bulunur

1. **Hangi istek yavaş?** Network sekmesinde `/api/orders?...` isteğinin süresine bakın. Development'ta yanıtta `Server-Timing: db;dur=...;desc="N sorgu"` vardır. Sorgu sayısı küçük (3-4), toplam süre büyükse N+1 değildir; tek bir sorgu yavaştır.
2. **SQL'i yakalayın.** `backend/src/Tezgah.Api/appsettings.Development.json` içine `Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command = Information` ekleyin ve API'yi yeniden başlatın (işiniz bitince geri alın). Konsolda `SELECT ... FROM orders AS o WHERE o.user_id = @p ... ORDER BY o.placed_at DESC, o.id DESC` görünür.
3. **Planı okuyun.** psql'e bağlanın:

   ```bash
   docker exec -it tezgah-postgres-1 psql -U tezgah
   ```

   ```sql
   EXPLAIN (ANALYZE, BUFFERS)
   SELECT o.id, o.order_number, o.status, o.placed_at, o.grand_total
     FROM orders o
    WHERE o.user_id = 4711
    ORDER BY o.placed_at DESC, o.id DESC
    LIMIT 10;
   ```

   Hatalı durumda çıktının biçimi şöyledir (sayılar sizde farklı olur):

   ```text
   Limit  (cost=21890.52..21890.55 rows=10 width=40) (actual time=412.118..412.121 rows=10 loops=1)
     Buffers: shared hit=3200 read=11842
     ->  Sort  (cost=21890.52..21890.76 rows=94 width=40) (actual time=412.116..412.118 rows=10 loops=1)
           Sort Key: placed_at DESC, id DESC
           Sort Method: top-N heapsort  Memory: 26kB
           ->  Seq Scan on orders o  (cost=0.00..21887.40 rows=94 width=40) (actual time=0.9..411.7 rows=87 loops=1)
                 Filter: (user_id = 4711)
                 Rows Removed by Filter: 999913
                 Buffers: shared hit=3200 read=11842
   Planning Time: 0.2 ms
   Execution Time: 412.2 ms
   ```

   Nasıl okunur:
   - **Plan içten dışa okunur.** En içteki düğüm (`Seq Scan`) veriyi üretir; üstündekiler (`Sort`, `Limit`) onu işler.
   - **`Seq Scan on orders` + `Rows Removed by Filter: 999913`**: tablonun tüm satırlarını okumuş, bir milyon satırın neredeyse tamamını atmış. Tüm sorunun imzası bu.
   - **`cost`** planlayıcının tahminidir (birimsiz), **`actual time`** gerçek süredir (ms). `rows=94` tahmin, `rows=87` gerçek: tahmin gerçeğe yakınsa istatistikler güncel demektir (kontrol bu yüzden önce `ANALYZE orders` çalıştırır).
   - **`Buffers: shared hit/read`**: `hit` bellekten, `read` diskten (ya da işletim sistemi önbelleğinden) okunan 8 KB'lık sayfa sayısıdır. 15 bin sayfa = yaklaşık 120 MB okuma; tek bir müşterinin ilk 10 siparişi için.
   - **`loops`**: düğümün kaç kez çalıştığıdır; gerçek süre ve satır sayısı döngü başınadır.
   - **`Execution Time`**: sorgunun gerçek süresi. `EXPLAIN` yalnızca `ANALYZE` ile birlikte sorguyu **gerçekten çalıştırır**; yazma yapan sorgularda dikkat (transaction içinde `ROLLBACK` edin).

   Doğru plan şöyle görünür:

   ```text
   Limit  (cost=0.43..38.12 rows=10 width=40) (actual time=0.041..0.088 rows=10 loops=1)
     ->  Index Scan Backward using ix_orders_user_id_placed_at on orders o
           Index Cond: (user_id = 4711)
           Buffers: shared hit=14
   Execution Time: 0.11 ms
   ```

   (Sıralamadaki ikinci anahtar `id` için planın üstünde bir `Incremental Sort` düğümü de görünebilir; sorun değildir.)
4. **Index'leri listeleyin.** `\d orders` çıktısındaki `Indexes:` bölümünde `user_id` ile başlayan filtresiz bir index yoktur: `ix_orders_user_id_idempotency_key` var ama `WHERE (idempotency_key IS NOT NULL)` kısmi index'idir.
5. **Geçmişe bakın.** `git log --stat -- backend/src/Tezgah.Infrastructure/Persistence/Migrations` ya da `dotnet ef migrations list` son migration'ın `RemoveUnusedIndexes` olduğunu gösterir. `git show cc518ae` index'in `OrderConfiguration.cs` ile birlikte silindiğini gösterir. Veritabanında da `select * from "__EFMigrationsHistory" order by 1 desc limit 3`.
6. **Küçük profil.** `small` profilde (50 bin sipariş) sorgu milisaniyelerde biter ve fark hissedilmez; ama plan yine `Seq Scan` gösterir. Kontrol süreye değil plana baktığı için küçük profilde de başarısız olur. Bu, aynı hatanın ilk gün fark edilmeyip veri büyüdükçe patlamasının nedenidir.

**Visual Studio / VS Code:** Veritabanı için SQL Server Object Explorer işe yaramaz (Postgres'e bağlanmaz); psql, DBeaver ya da DataGrip kullanın. VS Code'da *Container Tools* ile `tezgah-postgres-1` container'ında *Attach Shell* yapıp `psql -U tezgah` çalıştırabilirsiniz.

## Düzeltme

Yayınlanmış migration'a dokunmayın; index'i geri getiren **yeni** bir migration ekleyin. Önce modeli düzeltin:

```diff
         b.HasIndex(x => new { x.Status, x.CreatedAt }).HasFilter("status = 1");
+        b.HasIndex(x => new { x.UserId, x.PlacedAt });
         b.HasIndex(x => new { x.Status, x.PlacedAt });
```

Sonra `backend` klasöründen:

```bash
dotnet ef migrations add RestoreOrdersUserPlacedAtIndex -p src/Tezgah.Infrastructure -s src/Tezgah.Api
dotnet ef database update -p src/Tezgah.Infrastructure -s src/Tezgah.Api
```

Üretilen migration şunu içermelidir (içeriği inceleyin, tam bu iki yönü yapmalı):

```csharp
migrationBuilder.CreateIndex(
    name: "ix_orders_user_id_placed_at",
    table: "orders",
    columns: new[] { "user_id", "placed_at" });
```

Sonra `kontrol 17` çalıştırın; plan `Index Scan`/`Index Only Scan` göstermelidir.

**Büyük canlı tabloda** `CREATE INDEX` tabloya yazmayı kilitler (okumayı kilitlemez). Milyonlarca satırda dakikalar sürebilir. Üretimde index'i `CREATE INDEX CONCURRENTLY` ile oluşturun. Npgsql sağlayıcısı bunu migration'da `IsCreatedConcurrently()` / `Npgsql:CreatedConcurrently` açıklamasıyla destekler; işlem (transaction) dışında çalışması gerekir, ayrıntı için Npgsql belgesine bakın. Yerel çalışmada gerekmez.

### Dalda migration uygulandıktan sonra `main`'e dönmek

Bu dalda migration'ı uygulayınca veritabanı `__EFMigrationsHistory` tablosunda `20261111041134_RemoveUnusedIndexes` kaydını taşır ve index yoktur. `main` dalında o migration sınıfı olmadığı için `main`'e dönünce veritabanı kodundan "ileride" kalır ve index eksik kalır. Temizlik için, **`main`'e dönmeden önce**, hâlâ bu dalda (ya da ondan açtığınız çalışma dalında) iken:

```bash
dotnet ef database update 20261111043003_UniqueSellerOwnerAndShipment -p src/Tezgah.Infrastructure -s src/Tezgah.Api
```

Bu komut `RemoveUnusedIndexes` migration'ının `Down` yöntemini çalıştırır, index'i geri getirir ve veritabanını `main`'in son migration'ına (`Outbox`) indirir. EF geri alırken migration sınıfına ihtiyaç duyar; `main`'e geçtikten sonra bu komut o migration'ı bulamaz. Geçtiyseniz önce dala dönün ya da elle: `CREATE INDEX ix_orders_user_id_placed_at ON orders (user_id, placed_at);` ve `DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261111041134_RemoveUnusedIndexes';` Ya da en kolayı, veriyi `DataGen --reset` ile yeniden üretin ve şemayı `main`'de baştan kurun.

## Neden böyle düzeltiyoruz

- **Yayınlanmış migration'ı silmek ya da düzenlemek.** Migration'lar tarih sırasıyla eklenen bir geçmiştir. `RemoveUnusedIndexes` başka ortamlarda (geliştirici makineleri, test, belki üretim) zaten uygulanmıştır ve `__EFMigrationsHistory` tablosunda kayıtlıdır. Dosyayı silerseniz ya da içeriğini değiştirirseniz o veritabanları değişmez (index yine yoktur), ama kodunuz "bu migration hiç olmadı" der: ortamlar birbirinden ayrışır. Bir sonraki `migrations add` da model anlık görüntüsüyle (snapshot) çelişkili sonuç üretir. İleriye doğru düzeltme (yeni migration) her ortamda aynı yoldan ilerler.
- **`dotnet ef migrations remove`.** Yalnızca henüz hiçbir veritabanına uygulanmamış son migration için güvenlidir. Uygulanmış olanı kaldırmaz ya da geri almaz.
- **Üretimde elle `CREATE INDEX` yazmak.** Üretimi düzeltir ama modeli ve diğer ortamları düzeltmez; bir sonraki geliştirici model farkıyla uğraşır. Elle müdahale gerekiyorsa (yangın) arkasından mutlaka aynı değişikliği migration olarak da ekleyin.
- **Sadece `user_id` üzerine index.** Filtreyi hızlandırır ama sıralama için ayrı bir `Sort` kalır ve çok siparişi olan müşteride tüm siparişlerini sıralamak gerekir. `(user_id, placed_at)` hem filtreyi hem sıralamayı karşılar; ilk 10 satırı okuyup durur.
- **Kapsayan (covering) index (`INCLUDE (...)`).** Tabloya hiç gitmeden index'ten yanıt verebilir; ama daha büyüktür ve her yazmada daha çok iş çıkarır. Önce basit olan, ölçüp gerekirse genişletin.
- **Önbellek ya da sayfa boyutunu küçültmek.** Belirtiyi gizler. Tablo büyüdükçe ilk (önbellek dışı) istek yine yavaştır.
- **Geliştiricinin kaygısını yok saymak.** Yazma maliyeti gerçek. Karar kanıta dayanmalı; aşağıdaki bölüme bakın.

### "Kullanılmayan index" kararı nasıl verilir

PostgreSQL index kullanım sayaçlarını tutar:

```sql
SELECT s.relname AS tablo,
       s.indexrelname AS index,
       s.idx_scan,                                   -- index'in kaç kez taranmaya seçildiği
       pg_size_pretty(pg_relation_size(s.indexrelid)) AS boyut,
       i.indisunique AS benzersiz
  FROM pg_stat_user_indexes s
  JOIN pg_index i ON i.indexrelid = s.indexrelid
 WHERE s.relname = 'orders'
 ORDER BY s.idx_scan, pg_relation_size(s.indexrelid) DESC;
```

`idx_scan = 0` bir silme kararı değil, bir **soru**dur. Önce şunları kontrol edin:

1. **Sayaç ne zamandan beri sayıyor?** İstatistikler `pg_stat_reset()` ya da sunucu çökme kurtarmasıyla sıfırlanır. `select stats_reset from pg_stat_database where datname = 'tezgah'` ile bakın. Geliştirici ortamında sayaç, o sayfayı kimse açmadığı için 0 görünebilir.
2. **Gerçek (üretim) trafiği izlendi mi, tam bir iş döngüsü boyunca mı?** Aylık raporlar, ay sonu, kampanya günleri, yılda bir çalışan işler yalnızca o zaman index'i kullanır. En az bir ay (tercihen bir iş döngüsü) sayaç biriktirin.
3. **Okuma kopyaları (replica) var mı?** İstatistikler sunucu başına tutulur. Okuma sorgularını kopya karşılıyorsa sayaçlar kopyadadır; hepsini toplayın.
4. **Index bir kısıt mı?** Benzersiz index ve birincil anahtar `idx_scan = 0` olsa bile veri bütünlüğünü korur (`indisunique`). Yabancı anahtar (FK) kolonlarındaki index silme/güncelleme sırasında üst tabloyu kontrol etmeyi hızlandırır.
5. **Hangi sorgu bu index'i kullanabilir?** `pg_stat_statements` ile en çok çalışan sorgulara bakın; EF sorgularında SQL'i `ToQueryString()` ya da günlükten alıp `EXPLAIN` ile deneyin. Aramanızı kodda index adıyla yapmayın.
6. **Yazma maliyetini ölçün.** Yazma yükünü (INSERT/s) ve index boyutunu karşılaştırın. Tek bir index genelde yazma süresine yüzde birkaç ekler; okuma tarafındaki zararı da ölçün.
7. **Geri döndürülebilir olsun.** Önce staging'de silin, sorgu gecikmelerini (p95) izleyin. `Down` yöntemini çalışır bırakın. Üretimde index'i geri oluşturmak büyük tabloda uzun sürer; "sil, sorun olursa geri koyarız" aslında saatler sürebilir.

Bu bölümdeki durumda `idx_scan` yüksek olurdu: Siparişlerim her açılışta bu index'i tarar.

## Bir daha olmaması için

- **Test:** Bölümün kontrolü gibi, kritik sorgular için plan testi: orta büyüklükte veriyle (en az birkaç yüz bin satır) `EXPLAIN` çalıştırıp `Seq Scan on orders` olmadığını doğrulayın. Küçük veriyle yazılan testler bu hatayı yakalayamaz, çünkü küçük tabloda Seq Scan zaten hızlı ve bazen planlayıcının da tercihi olur.
- **Kod inceleme:** `DropIndex` içeren her migration için PR'da kanıt isteyin: `pg_stat_user_indexes` çıktısı (hangi ortam, sayaç ne zamandan beri), index'i kullanabilecek sorguların listesi, `EXPLAIN` öncesi/sonrası. "Görmedim" yetmez.
- **Belgeleme:** `OrderConfiguration.cs` içinde her index'in yanına hangi sorguya hizmet ettiğini yazın (ör. `// Siparişlerim: WHERE user_id ORDER BY placed_at DESC`). Silmek isteyen kişi neyi bozacağını görür.
- **İzleme:** Üretimde yavaş sorgu günlüğü (`log_min_duration_statement`), `pg_stat_statements`, `pg_stat_user_tables.seq_scan` / `seq_tup_read` artışı için uyarı, uç nokta bazlı gecikme (p95) panosu. Bir yayından sonra tek bir uç noktanın p95'inin yükselmesi bu hatanın imzasıdır.
- **Yayın:** Şema değişikliklerini kademeli yayınlayın ve ilk saatlerde ilgili sorguların süresini izleyin.

## İlgili

HATALAR.md · **DB-01 Eksik index** (katalogdaki örnek hiç index olmamasıdır; burada index var olduktan sonra "kullanılmıyor" diye kaldırıldı). Bkz. ayrıca DB-02 Index var ama kullanılmıyor.

## English summary

**Root cause:** the `RemoveUnusedIndexes` migration drops `ix_orders_user_id_placed_at` (`user_id`, `placed_at`) from `orders`. The My Orders query (`WHERE user_id = ? ORDER BY placed_at DESC LIMIT 10`) depended on it. Without it PostgreSQL does a `Seq Scan on orders` and filters about a million rows to find one customer's orders: milliseconds with 50 thousand orders, seconds with a million. The commit "Kullanılmayan index'ler kaldırıldı" said no query using the index was seen, but an EF/LINQ query never mentions an index name, so searching the code proves nothing.

**How to find it:** the slow request has few queries but high `db;dur`; log the SQL (`Microsoft.EntityFrameworkCore.Database.Command` at `Information`); run it with `EXPLAIN (ANALYZE, BUFFERS)` in psql (`docker exec -it tezgah-postgres-1 psql -U tezgah`) and read the plan inside-out: `Seq Scan`, `Rows Removed by Filter`, `Buffers`, `Execution Time`; check `\d orders` and the migration history.

**Fix:** put `HasIndex(x => new { x.UserId, x.PlacedAt })` back and add a NEW migration that recreates the index (use `CREATE INDEX CONCURRENTLY` on large production tables). Never delete or edit a published migration. After trying the bug branch, run `dotnet ef database update 20261111043003_UniqueSellerOwnerAndShipment` while still on the branch (before switching to `main`) to roll back.

**Prevent:** decide on "unused" indexes with evidence from `pg_stat_user_indexes` over a full business cycle on production (including replicas, constraints and `stats_reset`), require evidence in code review, plan tests on realistic data volume, document what each index serves, and monitor endpoint p95 and sequential scans.
