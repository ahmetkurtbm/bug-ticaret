# İpuçları · Bölüm 14 (NET-11)

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Kuponun gerçekte hangi ana kaydedildiğine bakın. Yeni bir kupon oluştururken Network sekmesinde yanıttaki `startsAt` ve `endsAt` değerlerini okuyun (UTC gelir). Veritabanında da aynısını görün: `docker exec -it tezgah-postgres-1 psql -U tezgah -c "select code, starts_at, ends_at from coupons order by id desc limit 3"`. Sonra beklenen değeri elle hesaplayın: "yarın 00:00 İstanbul" UTC'de kaç saat eder?

*EN:* Look at which instant the coupon is really stored at. When you create a new coupon, read `startsAt` and `endsAt` in the response in the Network tab (they arrive in UTC). See the same in the database: `docker exec -it tezgah-postgres-1 psql -U tezgah -c "select code, starts_at, ends_at from coupons order by id desc limit 3"`. Then work out the expected value by hand: how many hours is "tomorrow 00:00 Istanbul" in UTC?

## İpucu 2

Fark tam 3 saat; bu bir saat dilimi (zaman dilimi) farkıdır. Yönetim paneli saatleri İstanbul saatiyle gösteriyor, yani panel yalan söylemiyor. Veriyi kuran katmanı bulun: kullanıcı "18 Kasım" gibi bir takvim günü seçiyor, sunucu bunu bir ana (`DateTimeOffset`) çeviriyor. Zamanları UTC saklamak doğru; ama "bu günün başlangıcı hangi andır?" sorusunun cevabı hangi takvime bağlı?

*EN:* The difference is exactly 3 hours; this is a time zone difference. The admin panel shows times in Istanbul time, so the panel is not lying. Find the layer that creates the data: the user picks a calendar day such as "18 November" and the server converts it to an instant (`DateTimeOffset`). Storing times in UTC is right; but which calendar decides the answer to "which instant is the start of this day"?

## İpucu 3

`backend/src/Tezgah.Application/Admin/AdminService.cs` içindeki `StartOfDay` günün başını `TimeSpan.Zero` ofsetiyle, yani UTC 00:00 olarak kuruyor. Aynı dosyada `Istanbul` saat dilimi alanı duruyor ama hiçbir yerde kullanılmıyor. Kampanya günleri İstanbul takvimine göredir; `StartOfDay` o günün İstanbul'daki 00:00'ının UTC karşılığını üretmeli. Başlangıç da bitiş de aynı fonksiyondan geçtiği için iki uç birlikte kayıyor.

*EN:* `StartOfDay` in `backend/src/Tezgah.Application/Admin/AdminService.cs` builds the start of a day with a `TimeSpan.Zero` offset, i.e. 00:00 UTC. The file still contains an `Istanbul` time zone field that is not used anywhere. Campaign days follow the Istanbul calendar; `StartOfDay` should produce the UTC equivalent of 00:00 in Istanbul on that day. Since both start and end go through the same function, both ends shift together.
