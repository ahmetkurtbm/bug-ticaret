> **Cevap anahtarı.** Bölümü kendiniz çözmeden okumayın; önce `ipucu` komutunu deneyin.

# Çözüm · Bölüm 25 (NET-17) · decimal / double karışıklığı ve yuvarlama sırası

## Kök neden

Satıcı panelinin Özet sayfasındaki 30 günlük komisyonu `backend/src/Tezgah.Application/Sellers/SellerOrderService.cs` içindeki gösterge metodu hesaplar. Hatalı sürümde satır şöyle:

```csharp
var commission = (decimal)Math.Round(sales.Sum(s => (double)s.LineTotal * (double)s.CommissionRate / 100), 2);
```

Hakediş ekstresi ve önceki sürüm ise komisyonu şöyle hesaplar:

```csharp
var commission = sales.Sum(s => Pricing.Money.Round(s.LineTotal * s.CommissionRate / 100m));
```

Birbirinden bağımsız **üç** fark var, üçü de kuruş farkı üretebilir:

1. **Yuvarlama sırası.** Ekstre her **kalemin** komisyonunu kuruşa yuvarlar, sonra yuvarlanmış kuruşları toplar. Hatalı kod ham değerleri toplar ve **toplamı** bir kez yuvarlar. Örnek: üç kalemin komisyonu `1,004`, `1,004`, `1,004` TL ise ekstre `1,00 + 1,00 + 1,00 = 3,00` yazar; hatalı kod `3,012 → 3,01` bulur. Kalem sayısı arttıkça iki yöntem arasındaki fark açılır. Hangi seviyede yuvarlama yapıldığı bir **iş kuralıdır** (ekstre kalem düzeyinde yuvarlar); kod bu kurala uymak zorundadır.
2. **`double` ikili kayan nokta türü.** `0,1` ya da `19,99 * 7,5 / 100` gibi ondalık değerler `double` ile tam gösterilemez (`0,1` ikilide sonsuz kesirdir). Bu, yarım kuruşa denk gelen değerlerde `x,xx5`'in `x,xx4999...` ya da `x,xx5000...1` olmasına yol açar; yuvarlama yanlış tarafa düşebilir. Para için `decimal` kullanılır: ondalık değerleri on tabanında tam gösterir.
3. **`Math.Round`'un varsayılan kuralı.** `Math.Round(x, 2)` yarım değerde **en yakın çifte** yuvarlar (`MidpointRounding.ToEven`, "banker's rounding"): `2,665 → 2,66` ama `2,675 → 2,68`. Ekstre (`Money.Round`, `MidpointRounding.AwayFromZero`) ise yarım değeri her zaman yukarı yuvarlar: `2,665 → 2,67`. Türk vergi uygulaması da böyledir.

Değişikliği yapan geliştirici şunu düşünmüş (commit `abc1234`):

> Gösterge: komisyon toplamı tek seferde yuvarlanıyor
> Her kalemi ayrı yuvarlamak gereksiz iş ve hata payı; topla, sonunda bir kez yuvarla.

Mantık matematik açısından "daha doğru" görünebilir (ara yuvarlama hatası birikmez). Ama burada doğruluk ölçütü matematiksel hassasiyet değil, **hakediş ekstresiyle aynı rakam**dır: satıcıya ekstre kalem kalem yuvarlanmış tutarları öder. Para hesabında "hangi seviyede yuvarlanır" bir iş kuralıdır; kod, kuralı değiştiremez.

## Nasıl bulunur

1. **İki rakamı alın.** `kontrol 25` ekstre tutarını ve panelin rakamını yan yana yazar. Fark birkaç kuruş.
2. **Farkın niteliği ipucudur.** Küçük, tutarlı, kalem sayısıyla büyüyen fark yuvarlama ve kayan nokta işaretidir; bir iş mantığı hatası (yanlış oran, eksik sipariş) çok daha büyük ya da rastgele fark verirdi. Satış tutarı ekstreyle tutuyorsa kalem ve sipariş kümesi doğrudur.
3. **Elle hesaplayın.** Satıcının 30 günlük kalemlerini çekin ve iki yolla toplayın:

   ```sql
   SELECT sum(round(oi.line_total * oi.commission_rate / 100, 2)) AS per_item_rounded,
          round(sum(oi.line_total * oi.commission_rate / 100), 2) AS total_rounded
     FROM order_items oi
     JOIN orders o ON o.id = oi.order_id
    WHERE oi.seller_id = <satıcı id> AND o.status NOT IN (<iptal, iade>) AND o.paid_at >= now() - interval '30 days';
   ```

   `per_item_rounded` ekstreyle, `total_rounded` panelle tutar (PostgreSQL'in `round(numeric, 2)` işlevi yarım değeri yukarı yuvarlar, `Money.Round` ile uyumludur).
4. **Kod:** Gösterge metodunda komisyon satırı. `(double)` dönüşümleri, `Math.Round` ve `Pricing.Money.Round` yokluğu gözle görülür. Aynı dosyanın başka yerlerinde ve projenin geri kalanında para için `decimal` ve `Money.Round` kullanılıyor; kıyaslayın. `git log -p -- backend/src/Tezgah.Application/Sellers/SellerOrderService.cs` değişikliği gösterir.
5. **Birim testiyle izole edin:** Kuruşun yarısına denk gelen basit bir girdi (örneğin komisyonu `2,665` çıkan bir kalem) bulup iki formülü yan yana çalıştırın. Hata ayıklayıcıda `(double)s.LineTotal * (double)s.CommissionRate / 100` ifadesini *Watch* penceresinde değerlendirip `decimal` karşılığıyla karşılaştırın.

## Düzeltme

`decimal` kullanın ve her kalemi `Money.Round` ile yuvarlayıp toplayın (tek kural, tek yer):

```diff
-        var commission = (decimal)Math.Round(sales.Sum(s => (double)s.LineTotal * (double)s.CommissionRate / 100), 2);
+        var commission = sales.Sum(s => Pricing.Money.Round(s.LineTotal * s.CommissionRate / 100m));
```

`Money.Round` (`backend/src/Tezgah.Application/Pricing/Money.cs`) `Math.Round(amount, 2, MidpointRounding.AwayFromZero)` çağırır; yuvarlama kuralı ve kuruş hassasiyeti tek yerde tanımlıdır. Panel, ekstre ve sepet fiyatlaması aynı işlevi kullandığı için rakamlar tutar.

## Neden böyle düzeltiyoruz

- **Yalnızca `Math.Round(..., 2, MidpointRounding.AwayFromZero)` eklemek.** Banker's rounding sorununu giderir ama yuvarlama sırası ve `double` hâlâ yanlış kalır; fark küçülür ama kaybolmaz.
- **`double`'ı `decimal`'e çevirip toplamı yuvarlamak.** `double` kaynaklı hatayı ve belki yarım değer sapmasını giderir; ama sıra farkı sürer (ekstre kalem kalem yuvarlıyor).
- **Ekstreyi panele uydurmak.** Ekstre, satıcıya yapılan ödemenin kaynağıdır ve mevcut sözleşmedir; değiştirmek geçmiş ekstreleri tutarsız yapar. Doğru olan, yeni kodu mevcut kurala uydurmaktır.
- **Yuvarlamayı yerelde yeniden yazmak** (`Math.Round(x, 2, MidpointRounding.AwayFromZero)` her yerde). Kural dağılır ve bir gün biri farklı yazar. Tek bir yardımcı (`Money.Round`) kuralı bir yerde tutar.
- **Veritabanında toplamak** (`SUM(ROUND(...))` SQL'de). Doğru sonuç verir, ama komisyon iş kuralı iki yerde (SQL ve C#) yaşar. Uygulama katmanında tek yerde tutmak daha güvenli.

## Bir daha olmaması için

- **Test:** Yarım değere denk gelen, birden çok kalemli örneklerle birim testi: ekstre formülü ile gösterge formülünün aynı sonucu verdiğini doğrulayın. Rastgele üretilmiş kalem kümeleriyle (özellik tabanlı test) iki hesap yolunun her zaman eşit çıktığını sınayın. Bölümün kontrolü, ekstre tutarıyla panelin rakamını karşılaştırarak bunu yapar.
- **Aynı kod yolu:** Panelin ve ekstrenin komisyonu **aynı** işlevden hesaplanmalı (örneğin `CommissionCalculator.ForLines(...)`); iki ayrı uygulama zamanla ayrışır.
- **Tür kuralı:** Para için `decimal`; `double`/`float` kullanımını analizörle uyarıya çevirin. Kod incelemede para hesabında `(double)` dönüşümü gören herkes duraksasın.
- **Kod inceleme maddesi:** "Basitleştirme" ya da "daha kesin" gerekçesiyle yuvarlama yerini değiştiren PR iş kuralı değiştiriyordur; muhasebe/finans onayı gereksin. Ekstre, fatura ve rapor gibi birbirinden bağımsız çıktılarda aynı kural yerleşik olmalı.
- **İzleme:** Panel ile ekstre arasındaki farkı dönem kapanışında otomatik karşılaştırın; sıfırdan farklıysa uyarı verin.

## İlgili

HATALAR.md · **NET-17 decimal / double karışıklığı**

## English summary

**Root cause:** the seller dashboard's 30-day commission was changed to `(decimal)Math.Round(sales.Sum(s => (double)s.LineTotal * (double)s.CommissionRate / 100), 2)`. The payout statement (and the previous code) rounds each item's commission with `Pricing.Money.Round(...)` and then sums. Three separate differences: (1) the rounding level: the statement rounds per item, the changed code sums raw values and rounds once; (2) `double` binary floating point instead of `decimal`, which cannot represent many decimal values exactly; (3) `Math.Round`'s default rounding is half to even (banker's rounding), while `Money.Round` rounds half away from zero. The developer (commit "Gösterge: komisyon toplamı tek seferde yuvarlanıyor") thought a single final rounding was more precise, but at which level money is rounded is a business rule: the panel must show the same number as the statement.

**How to find it:** a small, consistent difference that grows with the number of items points to rounding or floating point; recompute from the items both per item and in total and see which matches which figure; read the commission line (`(double)`, no `Money.Round`); `git log -p`.

**Fix:** `sales.Sum(s => Pricing.Money.Round(s.LineTotal * s.CommissionRate / 100m))`: `decimal`, per-item `Money.Round`, one rule in one place.

**Prevent:** tests with half-way values and several items (ideally property-based), one shared calculation for panel and statement, `decimal` for money, review of any change to where rounding happens, and an automatic comparison at period close.
