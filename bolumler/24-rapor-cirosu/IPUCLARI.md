# İpuçları · Bölüm 24

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Sayıyı panelden çok veritabanından doğrulayın. Rakamı hangi API'nin ürettiğini Network sekmesinde bulun (Gösterge sayfası açılırken giden `report` ile ilgili istek) ve yanıttaki `revenue` ile `orderCount` değerlerini not alın. Sonra veritabanında aynı aralık için `docker exec -it tezgah-postgres-1 psql -U tezgah` ile ödenmiş siparişlerin toplamını kendiniz hesaplayın: `select count(*), sum(grand_total) from orders where ...`. İki sonuç farklıysa fark nereden geliyor? Önce farkın oranına bakın: sabit bir çarpan mı, rastgele mi?

*EN:* Verify the number in the database rather than in the panel. In the Network tab find which API produces the figure (the request related to `report` that goes out when the Dashboard page opens) and note `revenue` and `orderCount` in the response. Then in the database, with `docker exec -it tezgah-postgres-1 psql -U tezgah`, work out the total of paid orders for the same range yourself: `select count(*), sum(grand_total) from orders where ...`. If the two results differ, where does the difference come from? First look at the ratio: is it a constant multiplier or random?

## İpucu 2

Sipariş sayısı doğru, tutar fazla ve fark rastgele değil: bazı siparişler birden fazla kez toplanıyor demektir. Panelin özet kutularını dolduran sorgu `backend/src/Tezgah.Infrastructure/Reporting/DapperReportingQueries.cs` içindeki özet (`summary`) sorgusudur. Sorgunun `FROM` ve `JOIN` kısmına bakın: bir siparişin birden çok satırı olan bir tabloyla birleştirildiği yer var mı? Sorguyu psql'de kendiniz çalıştırın ve tek bir siparişin kaç satır ürettiğine bakın.

*EN:* The order count is right, the amount is too high, and the difference is not random: some orders are being summed more than once. The query that fills the panel's summary boxes is the `summary` query in `backend/src/Tezgah.Infrastructure/Reporting/DapperReportingQueries.cs`. Look at the `FROM` and `JOIN` part: is there a place where an order is joined with a table that has several rows per order? Run the query yourself in psql and see how many rows a single order produces.

## İpucu 3

Özet sorgusuna "satılan ürün adedi" için `JOIN order_items oi ON oi.order_id = o.id` ve `sum(oi.quantity) AS items_sold` eklenmiş. Bir siparişin birden çok kalemi olduğu için birleştirmeden sonra sipariş satırı kalem sayısı kadar çoğalıyor. Sipariş sayısı `count(DISTINCT o.id)` ile korunmuş, bu yüzden doğru görünüyor; ama `sum(o.grand_total)` ve `avg(o.grand_total)` çoğalan satırların hepsini topluyor. `DISTINCT` sayıyı düzeltir, toplamı düzeltmez. Kalem adedini ayrı bir alt sorguda hesaplayın.

*EN:* A `JOIN order_items oi ON oi.order_id = o.id` and `sum(oi.quantity) AS items_sold` were added to the summary query for "items sold". Since an order has several items, after the join the order row is repeated as many times as it has items. The order count is protected with `count(DISTINCT o.id)`, so it looks right; but `sum(o.grand_total)` and `avg(o.grand_total)` add up all the repeated rows. `DISTINCT` fixes the count, not the sum. Compute the item quantity in a separate subquery.
