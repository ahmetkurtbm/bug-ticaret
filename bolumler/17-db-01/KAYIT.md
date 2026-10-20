# TZ-4520 · Siparişlerim sayfası saniyelerce bekliyor

| Alan | Değer |
| --- | --- |
| Kayıt no | TZ-4520 |
| Seviye | ●●● (3/5) |
| Modül | Veritabanı · Sipariş geçmişi |
| Bildiren | Burcu Yalçın, müşteri başarı uzmanı |
| Öncelik | Yüksek |
| Ortam | Büyük veri içeren test ortamı (yaklaşık 1 milyon sipariş). Yerelde: `medium` veri profili, API `--launch-profile http`, müşteri sitesi http://localhost:5173 |
| Durum | Açık |

## Ne oldu

Çok sipariş vermiş müşterilerimizden şikâyet geliyor: "Siparişlerim" sayfasını açtıklarında sayfa uzun süre "Yükleniyor" durumunda kalıyor, sipariş listesi saniyeler sonra geliyor. Bizim ekipten biri sipariş geçmişi uzun olan bir test hesabıyla denedi, aynı şeyi gördü. Hata mesajı yok, sonunda doğru liste açılıyor.

Dikkat çeken şey: Az siparişi olan hesaplarda sayfa hızlı. Küçük veriyle çalışan geliştirici ortamlarında da hızlı; orada bir sorun görünmüyor. Yalnızca büyük veride ve çok siparişi olan kullanıcıda yavaş. Bu sayfa geçen hafta hızlıydı.

## Tekrar etme adımları

Komutları `backend` klasöründen çalıştırın (başka bir yer belirtilmedikçe).

1. Altyapıyı başlatın: `docker compose up -d`
2. Orta boy veriyi üretin (3-5 dakika sürer): `dotnet run --project tools/Tezgah.DataGen -- --profile medium --reset`
3. Veritabanı şemasını güncelleyin: `dotnet ef database update -p src/Tezgah.Infrastructure -s src/Tezgah.Api`
4. API'yi başlatın: `dotnet run --project src/Tezgah.Api --launch-profile http`
5. Başka bir terminalde: `cd frontend && npm run dev:storefront`
6. En çok siparişi olan müşterileri bulun:

   ```bash
   docker exec -it tezgah-postgres-1 psql -U tezgah -c "select u.email, count(*) from orders o join users u on u.id = o.user_id group by u.email order by 2 desc limit 5"
   ```
7. O müşteriyle (ya da `musteri@tezgah.test`; şifre `Tezgah123!`) http://localhost:5173 adresinde giriş yapıp **Siparişlerim** sayfasını açın. Tarayıcıda Network sekmesinde sipariş isteğinin süresine bakın.

`small` profilde (50 bin sipariş) fark milisaniye düzeyinde kalır; yine de sorgunun ne yaptığına bakabilirsiniz.

## Beklenen

Sipariş geçmişi kaç sipariş olursa olsun hızlı açılır (onlarca milisaniye).

## Gerçekleşen

Çok siparişi olan müşteride, orta veri profilinde sayfa saniyeler sürüyor.

## Ek bilgi

- Sayfa geçen hafta bu veriyle hızlıydı.
- Veritabanı sunucusunun işlemcisi ve belleği normal; başka sayfalar etkilenmiyor. Sadece müşterinin sipariş geçmişi.
- Son sürümde şema değişikliği (migration) vardı.

## Bitirdiğinizde

Kontrol, en çok siparişi olan müşterinin "Siparişlerim" sorgusunu `EXPLAIN (ANALYZE, BUFFERS)` ile çalıştırır ve planda `orders` tablosunun baştan sona taranıp taranmadığına bakar. Migration'ınızı uyguladıktan sonra çalıştırın:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 17
```

---

## English

# TZ-4520 · My Orders page takes seconds to load

| Field | Value |
| --- | --- |
| Ticket | TZ-4520 |
| Level | ●●● (3/5) |
| Module | Database · Order history |
| Reporter | Burcu Yalçın, customer success specialist |
| Priority | High |
| Environment | Test environment with a lot of data (about 1 million orders). Locally: the `medium` data profile, API `--launch-profile http`, storefront at http://localhost:5173 |
| Status | Open |

### What happened

Customers who have placed many orders are complaining: when they open the "Siparişlerim" (My Orders) page it stays in the "loading" state for a long time and the order list arrives seconds later. Someone on our team tried with a test account with a long order history and saw the same thing. There is no error message; the correct list eventually opens.

The notable thing: for accounts with few orders the page is fast. It is also fast in developer environments that run with small data; nothing looks wrong there. It is slow only with large data and for users with many orders. The page was fast last week.

### Steps to reproduce

Run the commands from the `backend` folder (unless stated otherwise).

1. Start the infrastructure: `docker compose up -d`
2. Generate medium-size data (takes 3-5 minutes): `dotnet run --project tools/Tezgah.DataGen -- --profile medium --reset`
3. Update the database schema: `dotnet ef database update -p src/Tezgah.Infrastructure -s src/Tezgah.Api`
4. Start the API: `dotnet run --project src/Tezgah.Api --launch-profile http`
5. In another terminal: `cd frontend && npm run dev:storefront`
6. Find the customers with the most orders:

   ```bash
   docker exec -it tezgah-postgres-1 psql -U tezgah -c "select u.email, count(*) from orders o join users u on u.id = o.user_id group by u.email order by 2 desc limit 5"
   ```
7. Sign in at http://localhost:5173 with that customer (or `musteri@tezgah.test`; password `Tezgah123!`) and open the **Siparişlerim** page. In the browser Network tab look at the duration of the orders request.

With the `small` profile (50 thousand orders) the difference stays at the millisecond level; you can still look at what the query does.

### Expected

The order history opens quickly (tens of milliseconds) however many orders there are.

### Actual

For a customer with many orders, on the medium data profile, the page takes seconds.

### Additional information

- The page was fast with this data last week.
- The database server's CPU and memory are normal; other pages are not affected. Only the customer's order history.
- The last release included a schema change (migration).

### When you are done

The check runs the "My Orders" query of the customer with the most orders under `EXPLAIN (ANALYZE, BUFFERS)` and looks at whether the plan scans the whole `orders` table. Run it after applying your migration:

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 17
```
