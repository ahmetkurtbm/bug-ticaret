# İpuçları · Bölüm 18 (EŞ-01)

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Önce `kontrol 18` çıktısını okuyun: kaç sipariş başarılı, kaç tanesi 409 aldı, stok sonunda ne oldu? Kontrol temizlik yaptığı için kanıt silinir; durumu canlı yakalamak için sipariş oluşturma koduna (`OrderService.cs`) durmadan log yazan bir kesme noktası (Visual Studio: *Actions*; VS Code: *Logpoint*) koyun ve stok satırının okunan değerini yazdırın. Ayrıca EF günlüğünü (`Microsoft.EntityFrameworkCore.Database.Command` = `Information`) açıp stok tablosuna giden SQL'lere bakın: önce hangi `SELECT`, sonra hangi `UPDATE`?

*EN:* First read the output of `kontrol 18`: how many orders succeeded, how many got 409, what happened to the stock at the end? The check cleans up, so the evidence disappears; to catch the state live, put a breakpoint that logs without stopping (Visual Studio: *Actions*; VS Code: *Logpoint*) in the order creation code (`OrderService.cs`) and print the value read from the stock row. Also turn on the EF log (`Microsoft.EntityFrameworkCore.Database.Command` = `Information`) and look at the SQL going to the stock table: which `SELECT` first, then which `UPDATE`?

## İpucu 2

Bu bir yarış durumu (race condition): bir değer okunuyor, kontrol ediliyor ve ardından yeni değer yazılıyor; aradaki boşlukta başka bir istek aynı değeri okuyabiliyor. Yedi istek aynı eski değeri okursa hepsi "yeterli stok var" der. Stok güncellemesinin SQL'ine bakın: `UPDATE ... SET reserved = <sabit bir sayı>` mi, yoksa `SET reserved = reserved + <adet>` mi? İlki "ben okuduğum değere göre hesapladım" der, ikincisi "veritabanındaki güncel değere ekle" der. Kontrol ile düşüm veritabanında tek adımda olmalı.

*EN:* This is a race condition: a value is read, checked and then a new value is written; in the gap another request can read the same value. If seven requests read the same old value, all of them say "there is enough stock". Look at the SQL of the stock update: is it `UPDATE ... SET reserved = <a fixed number>` or `SET reserved = reserved + <quantity>`? The first says "I computed from the value I read", the second says "add to the current value in the database". The check and the deduction should be a single step in the database.

## İpucu 3

`backend/src/Tezgah.Application/Orders/OrderService.cs` içindeki `ReserveStockAsync`, `StockItem` satırını izlenen bir nesne olarak yükleyip C#'ta `stock.Quantity - stock.Reserved < quantity` kontrolü yapıyor, sonra `stock.Reserved += quantity` ile değiştiriyor; EF bunu `UPDATE stock_items SET reserved = @yeniDeğer WHERE id = @id` olarak gönderiyor. Aynı anda gelen siparişler aynı `reserved` değerini okuyup hepsi aynı yeni değeri yazıyor (kayıp güncelleme) ve hepsi kontrolü geçiyor. Düzeltme: kontrol ile artırmayı tek bir koşullu UPDATE'te birleştirin (`WHERE quantity - reserved >= @adet`) ve etkilenen satır sayısına bakın: 0 ise stok yoktur.

*EN:* `ReserveStockAsync` in `backend/src/Tezgah.Application/Orders/OrderService.cs` loads the `StockItem` row as a tracked object, checks `stock.Quantity - stock.Reserved < quantity` in C#, and then changes it with `stock.Reserved += quantity`; EF sends this as `UPDATE stock_items SET reserved = @newValue WHERE id = @id`. Orders arriving at the same time read the same `reserved` value and all write the same new value (a lost update), and all pass the check. The fix: combine the check and the increment into a single conditional UPDATE (`WHERE quantity - reserved >= @quantity`) and look at the affected row count: if it is 0, there is no stock.
