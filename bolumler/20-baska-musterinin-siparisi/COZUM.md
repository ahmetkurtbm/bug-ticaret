> **Cevap anahtarı.** Bölümü kendiniz çözmeden okumayın; önce `ipucu` komutunu deneyin.

# Çözüm · Bölüm 20 (GÜV-04) · IDOR: başkasının kaydına erişim

## Kök neden

`backend/src/Tezgah.Application/Orders/OrderService.cs` içindeki `GetAsync` metodu siparişi yalnızca sipariş numarasıyla arıyor:

```csharp
var order = await db.Orders.AsNoTracking()
    .Include(o => o.Items)
    .Include(o => o.StatusHistory)
    .SingleOrDefaultAsync(o => o.OrderNumber == orderNumber, ct)
    ?? throw new NotFoundException("Sipariş bulunamadı.");
```

Metot `userId` parametresini alıyor, ama sorgu onu kullanmıyor. Oturum açmış herhangi bir müşteri, sistemde var olan herhangi bir sipariş numarasını yazarak o siparişi okuyabiliyor: ad, telefon, adres, kalemler, durum geçmişi. Buna **IDOR** (Insecure Direct Object Reference) denir; OWASP Top 10'da **A01 Broken Access Control** başlığı altındadır.

Önceki sürümde filtre şöyleydi: `o => o.UserId == userId && o.OrderNumber == orderNumber`. Değişikliği yapan geliştirici şunu düşünmüş (commit `abc1234`):

> Sipariş detayı sorgusu sadeleştirildi
> Uç nokta zaten [Authorize] arkasında, kullanıcı kontrolü gereksiz.

Gerekçe yarı doğru: `[Authorize]` gerçekten, ama yalnızca **kim olduğunuzu** doğrular (kimlik doğrulama). **Neye erişebileceğinizi** (yetkilendirme, sahiplik) söylemez. Giriş yapmış herkes `[Authorize]`'ı geçer. Sahiplik denetimi nesne düzeyinde, yani her sorguda yapılmalıdır. Eski filtre "kullanıcıya ait değilse bulunamadı" davranışını zaten sağlıyordu; "gereksiz" denen satır tam olarak bu korumaydı.

## Nasıl bulunur

1. **İki hesapla deneyin.** IDOR tek hesapla görünmez; kendi kaydınıza erişmek her zaman çalışır. Hesap A'nın bir kaydının adresini hesap B oturumundan açmak, bu sınıf hataların standart testidir. `kontrol 20` aracı da bunu yapar.
2. **Network sekmesi.** B hesabıyla `GET /api/orders/TZ-2026-...` isteği `200` döner ve gövdede A'nın verisi vardır. Doğru davranış `404`'tür.
3. **Kodda iz sürün.** `OrdersController.Get` → `OrderService.GetAsync(userId, orderNumber, ct)`. Kullanıcı kimliği servise iletilmiş ama sorguya girmemiş. Aynı dosyada `RetryPaymentAsync` ve benzeri metotlar `o.UserId == userId` ile filtreliyor; farkı karşılaştırınca fark edilir.
4. **Geçmiş.** `git log -p -- backend/src/Tezgah.Application/Orders/OrderService.cs` kaldırılan `o.UserId == userId &&` parçasını gösterir.

**Visual Studio / VS Code:** `GetAsync` içine kesme noktası koyup B oturumuyla isteği gönderin; `userId` B'nin kimliğidir, `order.UserId` ise başkasınındır. Sorgunun ürettiği SQL'i (EF Core günlüğü) açarsanız `WHERE` içinde `user_id` koşulunu göremezsiniz.

## Düzeltme

```diff
         var order = await db.Orders.AsNoTracking()
             .Include(o => o.Items)
             .Include(o => o.StatusHistory)
-            .SingleOrDefaultAsync(o => o.OrderNumber == orderNumber, ct)
+            .SingleOrDefaultAsync(o => o.UserId == userId && o.OrderNumber == orderNumber, ct)
             ?? throw new NotFoundException("Sipariş bulunamadı.");
```

Başkasının siparişi için `NotFoundException` (HTTP `404`) fırlar; müşteri sitesi "Sipariş bulunamadı" gösterir.

## Neden böyle düzeltiyoruz

- **`403 Forbidden` dönmek.** "Bu numara var ama sana ait değil" demek, siparişin varlığını sızdırır. Saldırgan numaraları denerken hangilerinin dolu olduğunu `403` ile `404` farkından çıkarır. Sahiplik filtresini sorguya koymak "yok" ile "senin değil"i ayırt edilemez yapar: ikisi de `404`.
- **Sorgudan sonra kontrol etmek** (`if (order.UserId != userId) throw ...`). Çalışır, ama veriyi gereksiz yere çekmiş olursunuz ve kontrolü unutmak kolaydır; filtre sorgunun kendisinde olunca unutulacak ayrı bir adım kalmaz.
- **Sipariş numarasını tahmin edilemez yapmak.** Rastgele/uzun kimlik saldırıyı zorlaştırır ama çözüm değildir: numara bir yerden sızarsa (e-posta, günlük, paylaşılan bağlantı) erişim açık kalır. Gizlilik, yetkilendirmenin yerini tutmaz.
- **Arayüzde gizlemek.** İstemciye güvenilmez; API doğrudan çağrılabilir.

## Bir daha olmaması için

- **Test:** "Başka kullanıcıyla erişim" testi. A için sipariş oluşturun, B oturumuyla `GET /api/orders/{A'nın numarası}` isteyin ve `404` bekleyin. Bu testi sahip tabanlı **her** uç nokta (sipariş, adres, sepet, satıcı kayıtları) için yazın; bölümün kontrolü tam olarak bunu yapıyor.
- **Merkezî sahiplik filtresi:** EF Core global query filter ya da kaynak tabanlı yetkilendirme (`IAuthorizationService` + gereksinim) ile sahiplik tek yerde tanımlansın; geliştirici her sorguda ayrıca hatırlamak zorunda kalmasın.
- **Kod inceleme maddesi:** Bir sorgudan kimlik/sahiplik koşulu kaldıran her PR'da "bunu artık kim denetliyor?" sorusu sorulsun. "Zaten `[Authorize]` var" gerekçesi kabul edilmesin: kimlik doğrulama ile yetkilendirme aynı şey değildir.
- **İzleme:** Aynı kullanıcının kısa sürede çok sayıda farklı sipariş numarasını denemesi (numara tarama) alarm üretsin; başarısız `404` oranını izleyin.

## İlgili

HATALAR.md · **GÜV-04 IDOR (başkasının kaydına erişim)**

## English summary

**Root cause:** `OrderService.GetAsync` looks up the order by order number only; the `userId` parameter is no longer part of the query. Any logged-in customer can read any order by typing its number: this is an IDOR (OWASP A01 Broken Access Control). The developer (commit "Sipariş detayı sorgusu sadeleştirildi") removed `o.UserId == userId &&` because "the endpoint is already behind [Authorize]". `[Authorize]` authenticates the caller (who you are); it says nothing about ownership (what you may access).

**How to find it:** test with two accounts (A's order opened from B's session returns 200 with A's data), read the code path (`userId` is passed but unused in the query), compare with other methods that filter by `UserId`, and `git log -p`.

**Fix:** restore the ownership filter: `SingleOrDefaultAsync(o => o.UserId == userId && o.OrderNumber == orderNumber, ct)`. Someone else's order now yields `NotFoundException` (404). Returning 404 rather than 403 also avoids revealing that the order exists.

**Prevent:** a "different user" access test for every owner-scoped endpoint, a central ownership rule (global query filter or resource-based authorization), review attention to removed ownership conditions, and monitoring of enumeration attempts.
