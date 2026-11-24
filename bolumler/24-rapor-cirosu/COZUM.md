> **Cevap anahtarı.** Bölümü kendiniz çözmeden okumayın; önce `ipucu` komutunu deneyin.

# Çözüm · Bölüm 24 (DB-07) · JOIN ile çoğalan toplam

## Kök neden

Gösterge sayfasının özet kutularını `backend/src/Tezgah.Infrastructure/Reporting/DapperReportingQueries.cs` içindeki özet sorgusu doldurur. Sorgu, "satılan ürün adedi" eklensin diye şu hâle getirilmiş:

```sql
SELECT count(DISTINCT o.id)::int                      AS order_count,
       coalesce(sum(o.grand_total), 0)                AS revenue,
       coalesce(avg(o.grand_total), 0)                AS average_order_value,
       coalesce(sum(oi.quantity), 0)::int             AS items_sold,
       coalesce((SELECT sum(round(oi2.line_total * oi2.commission_rate / 100, 2))
                   FROM order_items oi2 JOIN orders o2 ON o2.id = oi2.order_id
                  WHERE o2.status IN (...) AND ...), 0) AS commission
  FROM orders o
  JOIN order_items oi ON oi.order_id = o.id
 WHERE o.status IN (...) AND o.paid_at >= ... AND o.paid_at < ...
```

(Sorgu, okunabilirlik için sadeleştirilerek gösterilmiştir.) `orders` ile `order_items` arasındaki ilişki **bire-çok**: bir siparişin birden çok kalemi var. `JOIN` sonrası her sipariş satırı kalem sayısı kadar kez çoğalır (fan-out). 3 kalemli, 300 TL'lik bir sipariş birleştirmeden sonra üç satır olur ve her satırda `o.grand_total = 300` yazar:

| sipariş | kalem | `o.grand_total` |
| --- | --- | --- |
| A | 1 | 300 |
| A | 2 | 300 |
| A | 3 | 300 |

- `count(DISTINCT o.id)` → **1** (doğru, çünkü `DISTINCT` çoğalan satırları tekilleştirir).
- `sum(o.grand_total)` → **900** (yanlış, 300 olmalı).
- `avg(o.grand_total)` → ortalama da kalem sayısı fazla olan siparişlere doğru kayar (çok kalemli siparişler daha çok "oy" alır).

Bu yüzden sipariş adedi muhasebeyle tutuyor, ciro ve ortalama sepet tutmuyor. Çok kalemli siparişler payını ağırlıklandırdığı için fark sabit bir çarpan değil, veriye bağlıdır.

Değişikliği yapan geliştirici şunu düşünmüş (commit `abc1234`):

> Göstergeye "satılan ürün adedi" eklendi
> Özet sorgusuna kalemler birleştirildi; sipariş adedi bozulmasın diye COUNT(DISTINCT).

Önlem alınmış ama eksik: `COUNT(DISTINCT o.id)` yalnızca sayım için işe yarar. Aynı sorguda `o` tablosunun sütunlarını **toplayan** her işlev (`sum`, `avg`) çoğalan satırlardan etkilenir. Ders: **1-N birleştirmeden sonra üst tablonun sütununu toplamak yanlış sonuç verir; `DISTINCT` sayıyı düzeltir, toplamı düzeltmez.** `sum(DISTINCT o.grand_total)` de çözüm değildir: iki farklı siparişin tutarı aynıysa biri kaybolur.

## Nasıl bulunur

1. **Belirti:** Sipariş sayısı doğru, tutar fazla. Sayı doğru ve toplam yanlışsa satırların çoğalmasından şüphelenin.
2. **Önce rakamı doğrulayın:** `psql` içinde birleştirmesiz bağımsız sorgu:

   ```sql
   SELECT count(*), sum(grand_total), avg(grand_total)
     FROM orders
    WHERE status IN (...) AND paid_at >= '...' AND paid_at < '...';
   ```

   Sonucu panelle ve `kontrol 24` çıktısıyla karşılaştırın. Panel bundan yüksektir.
3. **Tek siparişi izole edin:** Aynı özet sorgusunu psql'de, `AND o.id = <bir sipariş>` ekleyerek ve `sum(o.grand_total)` yerine `count(*)` seçerek çalıştırın: sorgu o tek sipariş için kaç satır üretiyor? 3 kalemli siparişte `3`. Satır sayısı kalem sayısına eşitse çoğalma kesindir. Daha görünür hâli: `SELECT o.id, o.grand_total, oi.id FROM orders o JOIN order_items oi ON oi.order_id = o.id WHERE o.id = <id>;`
4. **Kod:** `DapperReportingQueries.cs` içinde özet sorgusunun `FROM ... JOIN order_items` bölümü. `git log -p -- backend/src/Tezgah.Infrastructure/Reporting/DapperReportingQueries.cs` eklenen `JOIN` ve `items_sold` satırını gösterir.
5. **Sorgu planı:** `EXPLAIN (ANALYZE)` çıktısında `Hash Join`/`Merge Join` sonrası satır sayısının sipariş sayısından büyük olması aynı çoğalmanın işaretidir.

**Araç ipucu:** DBeaver ya da psql'de sorguyu `count(*)` ile çalıştırmak, sorgunun tabloyu kaç satıra çıkardığını doğrudan gösterir; toplam yerine satır sayısına bakmak çoğu "yanlış toplam" hatasını bir adımda çözer.

## Düzeltme

Kalem adedini ayrı bir alt sorguda hesaplayın; ana sorgu yalnızca `orders` üzerinde kalsın:

```diff
-        SELECT count(DISTINCT o.id)::int                      AS order_count,
-               coalesce(sum(o.grand_total), 0)                AS revenue,
-               coalesce(avg(o.grand_total), 0)                AS average_order_value,
-               coalesce(sum(oi.quantity), 0)::int             AS items_sold,
+        SELECT count(*)::int                                  AS order_count,
+               coalesce(sum(o.grand_total), 0)                AS revenue,
+               coalesce(avg(o.grand_total), 0)                AS average_order_value,
+               coalesce((SELECT sum(oi.quantity)
+                           FROM order_items oi JOIN orders o2 ON o2.id = oi.order_id
+                          WHERE o2.status IN {SoldStatuses} AND {rangeFor("o2")}), 0)::int AS items_sold,
                coalesce((SELECT sum(round(oi2.line_total * oi2.commission_rate / 100, 2)) ...), 0) AS commission
           FROM orders o
-          JOIN order_items oi ON oi.order_id = o.id
          WHERE o.status IN {SoldStatuses} AND {range}
```

(`rangeFor` gösterimlik; mevcut `range` sabitine `o2` takma adıyla uyarlayın.) Yapı, komisyonun zaten böyle hesaplanmasıyla aynıdır: kalem düzeyindeki toplam, kendi alt sorgusunda yapılır.

Alternatif olarak kalemleri **önce toplayıp** sonra birleştirebilirsiniz; bu durumda birleşen tarafın her sipariş için tek satır üretmesi garanti olmalıdır:

```sql
FROM orders o
LEFT JOIN (SELECT order_id, sum(quantity) AS qty FROM order_items GROUP BY order_id) q ON q.order_id = o.id
```

## Neden böyle düzeltiyoruz

- **`sum(DISTINCT o.grand_total)`.** Aynı tutarlı iki siparişten biri toplamdan düşer; yanlış sonucu başka bir yanlışla değiştirir.
- **`DISTINCT ON (o.id)` ya da `GROUP BY o.id` ile iç sorgu yazıp dışarıda toplamak.** Doğru sonuç verir ama gereksiz iş yapar: önce çoğaltıp sonra tekilleştirir. Çoğalmaya hiç izin vermemek daha basit ve hızlıdır.
- **Uygulamada (C#) bölmek / düzeltmek.** Çoğalma katsayısını sonradan tahmin etmeye çalışmak kırılgandır; doğru yer sorgunun kendisi.
- **`items_sold`'u hiç eklememek.** İş gereksinimi gerçek; mesele onun nasıl hesaplandığı.
- **Pencere işlevi (`sum(...) FILTER`).** Gereksiz karmaşıklık; ayrı alt sorgu aynı işi okunaklı yapar.

## Bir daha olmaması için

- **Test:** Bilinen bir veriyle sorgu testi: çok kalemli bir sipariş (örneğin 300 TL, 3 kalem) ve tek kalemli bir sipariş (100 TL) ekleyin; ciro `400`, sipariş `2`, ortalama `200`, kalem adedi toplamı beklenir. Veri seti **çok kalemli** sipariş içermezse bu hata test edilemez. Bölümün kontrolü, panel ile muhasebe rakamını karşılaştırarak bunu sınar.
- **Tutarlılık testi:** Aynı aralık için paneldeki ciro ile ham `orders` toplamını karşılaştıran bir mutabakat sorgusu/testi, raporlama değişikliklerinde otomatik çalışsın.
- **Kod inceleme maddesi:** Bir sorguya bire-çok `JOIN` eklendiğinde şu soru sorulsun: "Üst tablonun sütunlarını toplayan/ortalayan her ifade hâlâ doğru mu?" `COUNT(DISTINCT ...)` varsa bir uyarı işareti olarak okunsun: yazan kişi çoğalmayı fark etmiş ama başka ifadeleri gözden kaçırmış olabilir.
- **Kuraldır:** Kalem düzeyi ölçüler (adet, satır toplamı) kalem tablosundan, sipariş düzeyi ölçüler (ciro, ortalama) sipariş tablosundan, **ayrı** hesaplanır ve gerekirse sonra birleştirilir.
- **İzleme:** Rapor rakamları ile muhasebe kapanışı arasındaki farkı her ay otomatik karşılaştırıp eşik aşılırsa uyarı verin.

## İlgili

HATALAR.md · **DB-07 JOIN ile çoğalan toplam**

## English summary

**Root cause:** the summary query in `DapperReportingQueries.cs` was changed to also report "items sold", by joining `order_items` to `orders` (`JOIN order_items oi ON oi.order_id = o.id`) and adding `sum(oi.quantity)`. The relationship is one-to-many, so each order row is repeated once per item (fan-out). The order count was protected with `count(DISTINCT o.id)`, so it stays right, but `sum(o.grand_total)` and `avg(o.grand_total)` add up every repeated row: revenue and average basket are inflated, and multi-item orders weigh more. The developer (commit "Göstergeye 'satılan ürün adedi' eklendi") fixed the count but not the sums. `DISTINCT` fixes a count, not a sum.

**How to find it:** compare the panel's figure with an independent query on `orders` alone, run the summary query in psql for one order and count the rows it produces (rows = number of items), check the `FROM`/`JOIN`, and `git log -p`.

**Fix:** compute the item quantity in a separate subquery (or pre-aggregate `order_items` per order) so the main query stays on `orders` only, one row per order.

**Prevent:** a query test with a multi-item order and known totals, a reconciliation test between the report and raw order totals, review of any one-to-many join added to an aggregate query, and monthly automatic comparison with accounting.
