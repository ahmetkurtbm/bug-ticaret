# Çözüm · Bölüm 16 (ARAÇ-01) · "Dün çalışıyordu": hatayı getiren commit

## Kök neden

`backend/src/Tezgah.Application/Admin/AdminService.cs` içindeki `CampaignHistoryAsync` sorgusunun sıralaması değişti:

```csharp
await db.AuditLogs.AsNoTracking()
    .Where(a => a.EntityType == "coupon")
    .OrderBy(a => a.Id)                                  // eskiden: OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
    .Take(Math.Clamp(take, 1, 200))
    ...
```

`OrderBy(a => a.Id)` eskiden yeniye sıralar. Ardından gelen `Take(n)` ilk n kaydı alır; yani **en eski** n kaydı. Panel 50 kayıt ister (`take=50`). Denetim kaydı (`audit_logs`) 50'yi geçtiğinde en yeni işlemler listeye hiç girmez. 50'den azken hepsi gelir ama en yeni en altta durur. Sorgu hata vermez, kayıtlar doğrudur; yalnızca hangilerinin ve hangi sırayla geldiği yanlıştır. Sessiz hata budur.

Değişikliği yapan geliştirici şunu düşünmüş (commit `99bca3f`, Emre Şahin, 31 Ekim):

> Kampanya geçmişi sorgusu sadeleştirildi
> Kupon kodu ve işlemi yapan kişi tek projeksiyonda; sıralama birincil anahtara göre.

İki anahtarlı sıralama (`CreatedAt` ve `Id`, ikisi de azalan) gereksiz görünmüş: `Id` zaten zamanla artıyor, tek anahtar yeter. Bu varsayım çoğu zaman doğrudur. Ama "sadeleştirirken" `Descending` düşmüş: sıra tersine dönmüş. Küçük bir sorguda ve az kayıtla bunu fark etmek zordur; kayıt sayısı `take`'i geçince belirgin olur. Commit mesajı da bir davranış değişikliğinden bahsetmez.

Aynı dalda bu commit'in çevresinde başka, zararsız değişiklikler de var (açıklama satırları, bir ternary ifadesinin satırlara bölünmesi, kupon listesinde eşit başlangıç günlerinde koda göre sıralama). Bu yüzden "son commit'lere bakıp tahmin etmek" yanıltıcıdır; `git bisect` tahmini testle değiştirir.

## Nasıl bulunur

Bu bölümde yöntem hatayı koda bakarak değil, **testle commit aralığını yarılayarak** bulmaktır. Önkoşul: elinizde hatayı sınayan, tekrarlanabilir bir komut olmalı. Burada: `dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 16` (API çalışırken). Çıkış kodu 0 ise iyi, değilse kötü.

### 1. Önce çalışma dalını açın ve temizlik yapın

```bash
dotnet run --project backend/tools/Tezgah.Bolum -- baslat 16     # calisma/16-arac-01 dalını açar
git status                                                        # çalışma ağacı temiz olmalı
git log --oneline main..HEAD                                      # 12 commit: aday listesi
```

Önce `kontrol 16` komutunu bu uçta çalıştırın (API'yi başlatıp): başarısız olmalı. Sonra `main` için de deneyin: geçmeli. Bisect'in iki ucu böylece doğrulanır; "iyi" dediğiniz commit gerçekten iyi değilse bisect yanlış commit'i bulur.

### 2. Elle bisect

```bash
git bisect start
git bisect bad            # şu anki commit (dalın ucu) bozuk
git bisect good main      # main doğru
```

Git ortadaki bir commit'e geçer ve ne kadar işi kaldığını söyler (örnek çıktı):

```text
Bisecting: 5 revisions left to test after this (roughly 3 steps)
[<commit>] <mesaj>
```

Her adımda:

1. Çalışan API'yi durdurun (Ctrl+C). Visual Studio'da hata ayıklamayı durdurun; IDE eski derlemeyi tutabilir.
2. API'yi yeniden başlatın: `dotnet run --project backend/src/Tezgah.Api --launch-profile http`
3. Başka bir terminalde: `dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 16`
4. Sonuca göre: `git bisect good` (kontrol geçti) ya da `git bisect bad` (kontrol kaldı).
5. Derlenemeyen bir commit gelirse `git bisect skip` deyin.

12 aday için en fazla 4 adım gerekir (log₂ 12 ≈ 3,6). Sonunda git şunu yazar:

```text
99bca3f... is the first bad commit
```

Bitirince mutlaka `git bisect reset` çalıştırıp dalınıza dönün. Gidişatı `git bisect log` ile görebilir, yanlış bir yanıt verdiyseniz `git bisect log > kayit.txt`, dosyayı düzenleyip `git bisect replay kayit.txt` ile tekrar oynatabilirsiniz.

### 3. Otomatik bisect: `git bisect run`

Elle adım atmak yerine bir komutun çıkış koduna güvenin: `0` iyi, `1-127` kötü (`125` hariç), `125` "bu commit test edilemiyor, atla". Bu klasördeki yardımcı betikler (`bolumler/16-arac-01/bisect-kontrol.sh` ve `bisect-kontrol.ps1`) bunu yapar: her adımda API'yi ve kontrol aracını derler, API'yi başlatıp hazır olmasını bekler, `kontrol 16`'yı çalıştırır, API'yi durdurur ve çıkış kodunu bisect'e döndürür. Derleme ya da başlatma başarısızsa 125 döner (commit atlanır).

**Betiği önce depo dışına kopyalayın.** Bisect çalışma ağacını her adımda başka bir commit'e çevirir; betik de depodaysa, hata ayıklarken başka bir sürüme dönüşebilir ya da hiç var olmayabilir.

Linux, macOS, Windows'ta Git Bash:

```bash
cp bolumler/16-arac-01/bisect-kontrol.sh /tmp/
git bisect start bolum/16-arac-01 main        # önce kötü uç, sonra iyi uç
git bisect run /tmp/bisect-kontrol.sh
git bisect reset
```

Windows PowerShell:

```powershell
Copy-Item bolumler\16-arac-01\bisect-kontrol.ps1 $env:TEMP\
git bisect start bolum/16-arac-01 main
git bisect run powershell -NoProfile -ExecutionPolicy Bypass -File $env:TEMP\bisect-kontrol.ps1
git bisect reset
```

Çalıştırmadan önce 5080 portunu kullanan başka bir API örneği (IDE'den başlattığınız dahil) kalmasın, Docker altyapısı açık olsun. Her adım derleme + açılış nedeniyle yarım dakika civarı sürer; toplamda birkaç dakika. Sonuç yine "is the first bad commit" satırıdır. Bisect çalışma ağacınızı commit'ten commit'e değiştirdiği için başlamadan önce kendi değişikliklerinizi commit'leyin ya da kaldırın.

### 4. Bulunan commit'i okuyun

```bash
git show 99bca3f
```

Diff tek satırdır: `OrderByDescending(CreatedAt).ThenByDescending(Id)` → `OrderBy(Id)`. Commit mesajının "sıralama birincil anahtara göre" cümlesi niyeti anlatır ama yönü söylemez. Bisect size "hangi commit" cevabını verir, "neden" cevabını siz okuyarak bulursunuz. Hash'ler dal yeniden kurulursa değişir; bu belgedeki hash'i değil bisect'in bulduğunu esas alın.

### Alternatif yollar ve ne zaman hangisi

| Yol | Ne zaman |
| --- | --- |
| `git bisect` | Neyin bozulduğunu bilmiyorsunuz, yalnızca "şu test geçiyordu, şimdi geçmiyor" diyebiliyorsunuz. En genel yöntem. |
| `git log -S"OrderByDescending(a => a.CreatedAt)" -- backend/src/Tezgah.Application/Admin/AdminService.cs` | Koddan kaybolan ya da eklenen bir metni biliyorsunuz (pickaxe). Bu metnin sayısını değiştiren commit'leri listeler: yukarıdaki arama `99bca3f`'yi verir. |
| `git log -L :CampaignHistoryAsync:backend/src/Tezgah.Application/Admin/AdminService.cs` | Bir fonksiyonun tüm geçmişini diff'leriyle görmek istiyorsunuz. |
| `git blame -L 205,220 backend/src/Tezgah.Application/Admin/AdminService.cs` | Şüpheli satırı biliyorsunuz; her satırı en son değiştiren commit'i gösterir. `.OrderBy(a => a.Id)` satırı `99bca3f`'yi gösterir. Satır sonradan başka bir commit'le (biçimlendirme) değişmişse yanlış kişiyi gösterebilir; `-w` ve `-M` ile boşluk ve taşıma değişikliklerini yok sayın. |
| `git log -- <dosya>` | Dosya biliniyorsa en hızlı ilk adım. Burada `AdminService.cs` için üç commit çıkar (`67a730b`, `99bca3f`, `d3c0887`); hangisinin bozduğunu yine test söyler. |

Blame ve `-S` "nerede" sorusunu bilen içindir; bisect "nerede" bilinmediğinde de çalışır.

## Düzeltme

```diff
         await db.AuditLogs.AsNoTracking()
             .Where(a => a.EntityType == "coupon")
-            .OrderBy(a => a.Id)
+            .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
             .Take(Math.Clamp(take, 1, 200))
```

## Neden böyle düzeltiyoruz

- **Sadece `Descending` eklemek (`OrderByDescending(a => a.Id)`).** Çoğu durumda aynı sonucu verir ve gerçekten daha basittir. Ama `Id` ile `CreatedAt` sırası ayrışabilir (başka sistemden içe aktarılmış, sonradan eklenmiş, saat sapmalı kayıtlar). Ekran "ne zaman" gösteriyor; sıralamanın kaynağı da zaman olmalı. Tek anahtarı seçerseniz bunu bilinçli yapın ve test edin; burada asıl hata sıralama anahtarı değil, yön ve testsizlikti.
- **Sıralamayı bellekte, `Take`'ten sonra tersine çevirmek.** Sorun `Take`'in yanlış kayıtları seçmesi; sıralama sonradan düzeltilemez. Doğru kayıtlar önce seçilmeli.
- **`Take`'i kaldırmak.** Her açılışta tüm denetim kayıtlarını çeker; tablo büyüdükçe yavaşlar ve bellek tüketir. Sınır doğru bir tasarım kararıdır.
- **Arayüzde `reverse()` yapmak.** Veri sorunu arayüzde saklanır, diğer istemciler yine yanlış alır ve en yeni kayıtlar yine eksik kalır.
- **Commit'i `git revert` ile geri almak.** Burada commit tek amaçlı olduğu için güvenli ve geçerli bir yoldur; ekipler çoğu zaman önce `revert` ile yangını söndürür. Yalnızca dosyadaki diğer değişiklikler karışıksa elle düzeltmek gerekir.

## Bir daha olmaması için

- **Test:** "Kayıtlar yeniden eskiye sıralı ve `take`'i aşan veride en yeniler döner" testi (`take`'ten fazla kayıt ekleyip ilk sırayı doğrulayın). `Take`, `First`, `Skip` kullanan her sorgunun yanında sıralama testi olmalı. Bu bölümün `kontrol 16` komutu aynı davranışı sınar; bir CI işine bağlanabilir.
- **Kod inceleme:** "Sadeleştirme" ve "temizlik" commit'lerinde davranışın değişmediğini kanıtlayan bir test isteyin. Sıralama, sayfalama, yuvarlama gibi sessiz sonuç değiştiren yerlerde kısa bir yorum ("en yeniden eskiye") niyeti korur.
- **Commit düzeni:** Küçük, tek amaçlı, her biri derlenen ve testleri geçen commit'ler bisect'i çalışır tutar. "Aramak için" derlenmeyen ara commit'ler bırakmayın. Commit mesajı yaptığı değişikliği doğru anlatsın: "sadeleştirildi" bir sıralama değişikliğini gizler.
- **İzleme:** Yönetim ekranlarında işlevsel (uçtan uca) bir duman testi; "geçmişin ilk satırı son işlem mi?" gibi basit bir sağlık kontrolü bu tür sessiz hataları iki hafta beklemeden yakalar. İki hafta sonra fark edilmesinin nedeni, hiçbir otomatik kontrolün bu davranışa bakmamasıydı.

## İlgili

HATALAR.md · **ARAÇ-01 "Dün çalışıyordu": hatayı getiren commit**

## English summary

**Root cause:** `CampaignHistoryAsync` in `AdminService.cs` was changed from `OrderByDescending(CreatedAt).ThenByDescending(Id)` to `OrderBy(Id)`. Combined with `Take(n)` this returns the oldest n audit records instead of the newest, and in oldest-first order. The commit "Kampanya geçmişi sorgusu sadeleştirildi" (99bca3f) assumed the two sort keys were redundant and dropped the direction. Other commits on the branch (comments, a split ternary, a tie-break on the coupon list) are harmless.

**How to find it:** with a repeatable test (`kontrol 16`), use `git bisect`: `git bisect start`, `git bisect bad`, `git bisect good main`, restart the API and run the check at each step, answer good/bad; at most 4 steps for 12 commits. Automate with `git bisect run` and the helper scripts `bisect-kontrol.sh` / `bisect-kontrol.ps1` (copy them outside the repository first, because the working tree changes at each step). Alternatives: `git log -S`, `git log -L`, `git blame` when you already know where to look.

**Fix:** restore `OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)` before `Take`.

**Prevent:** a test with more records than `take`, review that demands proof for "simplification" commits, small commits that always build (so bisect works), and an end-to-end smoke check of the admin screens.
