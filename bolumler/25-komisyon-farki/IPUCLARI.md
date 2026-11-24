# İpuçları · Bölüm 25

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Önce farkın nerede doğduğunu daraltın. `kontrol 25` çıktısındaki iki rakamı (ekstre ve panel) not alın. Sonra veritabanından (`docker exec -it tezgah-postgres-1 psql -U tezgah`) satıcının son 30 gündeki ödenmiş sipariş kalemlerini, tutarlarını ve komisyon oranlarını çekin ve komisyonu kendiniz hesaplayın. Hesabı birden çok yoldan deneyin (örneğin kalem kalem ve toplu) ve sonuçları iki rakamla karşılaştırın. Hangi yol ekstreyle, hangisi panelle tutuyor? Ekstre ile panelin aynı kalemlerden aynı sonuca varmak için farklı ne yaptığını sorun.

*EN:* First narrow down where the difference arises. Note the two figures from the `kontrol 25` output (statement and panel). Then pull the seller's paid order items of the last 30 days, with their amounts and commission rates, from the database (`docker exec -it tezgah-postgres-1 psql -U tezgah`) and calculate the commission yourself. Try the calculation in more than one way (for example item by item and in bulk) and compare the results with the two figures. Which way matches the statement, and which matches the panel? Ask what the statement and the panel do differently to reach a result from the same items.

## İpucu 2

Fark küçük (kuruş mertebesinde) ve para hesabıyla ilgili; bu tür farklar genellikle tutarın türünden ya da yuvarlamanın nerede ve nasıl yapıldığından doğar. Satıcı panelinin özetini üreten kod `backend/src/Tezgah.Application/Sellers/SellerOrderService.cs` içindeki gösterge (dashboard) metodunda. Komisyonun hesaplandığı satıra bakın ve şunları sorun: hangi veri türü (`decimal`, `double`) kullanılıyor, yuvarlama toplamdan önce mi sonra mı yapılıyor, yuvarlama için projedeki ortak bir yardımcı kullanılıyor mu?

*EN:* The difference is small (in kuruş) and tied to a money calculation; such differences usually come from the type of the amount or from where and how rounding is done. The code that produces the seller panel's summary is in the dashboard method in `backend/src/Tezgah.Application/Sellers/SellerOrderService.cs`. Look at the line that calculates the commission and ask: which data type is used (`decimal`, `double`), is rounding done before or after the sum, and is a shared helper of the project used for rounding?

## İpucu 3

Gösterge metodunda komisyon `(decimal)Math.Round(sales.Sum(s => (double)s.LineTotal * (double)s.CommissionRate / 100), 2)` biçimine çevrilmiş: tutarlar `double`'a çevriliyor, kalemler **toplanıyor**, toplam bir kez `Math.Round(..., 2)` ile yuvarlanıyor. Ekstre ise her kalemin komisyonunu `Pricing.Money.Round(...)` ile kuruşa yuvarlayıp **sonra** topluyor. Üç ayrı fark var: yuvarlama sırası, `double` ikili kayan nokta türü ve `Math.Round`'un varsayılan "yarım değeri çifte yuvarla" (banker's rounding) kuralı; `Money.Round` yarım değeri yukarı yuvarlar. Önceki hâli için `git log -p -- backend/src/Tezgah.Application/Sellers/SellerOrderService.cs`.

*EN:* In the dashboard method the commission was changed to the form `(decimal)Math.Round(sales.Sum(s => (double)s.LineTotal * (double)s.CommissionRate / 100), 2)`: the amounts are converted to `double`, the items are **summed**, and the total is rounded once with `Math.Round(..., 2)`. The statement, on the other hand, rounds each item's commission to the kuruş with `Pricing.Money.Round(...)` and **then** sums. There are three separate differences: the order of rounding, the binary floating-point `double` type, and the default "round half to even" (banker's rounding) rule of `Math.Round`; `Money.Round` rounds half up. For the previous version, see `git log -p -- backend/src/Tezgah.Application/Sellers/SellerOrderService.cs`.
