# Çözüm · Bölüm 11 (FE-03) · Bayat kapanış (stale closure)

## Kök neden

`frontend/apps/storefront/src/cart/useQuantityStepper.ts` içindeki `change` fonksiyonu `useCallback` ile bir kez oluşturuluyor; bağımlılık listesi yalnızca `[max]`. Fonksiyonun içinde ise state okunuyor:

```ts
const base = pending ?? serverQuantity
```

JavaScript'te bir fonksiyon, oluşturulduğu anda görünen değişkenleri "yakalar" (kapanış, closure). `change` ilk çizimde oluştuğu için ilk çizimdeki `pending` (`null`) ve `serverQuantity` (örneğin 1) değerlerini hep o hâliyle görür. State sonradan değişse bile fonksiyon eski kopyaya bakar. Bu duruma bayat kapanış (stale closure) denir.

Sonuç: her `+` tıklaması `1 + 1 = 2` hesaplar. `setPending(2)` ve zamanlayıcı da 2'yi sunucuya gönderir. 4 tıklamadan sonra adet 2'dir. Ekran güncel `pending` state'inden çizildiği için ekranda da 2 görünür; hata sessizdir. Tıklamalar yavaş olsa da aynı olur: sunucu adedi 2'ye güncelleyince bileşen yeniden çizilir ama `change` hâlâ ilk çizimin kopyasını kullanır.

Değişikliği yapan geliştirici şunu düşünmüş (commit `9e5a6bc`):

> Sepet adet düğmeleri: gösterilen adet doğrudan state'ten okunuyor

Niyeti makul: dosyada `latest`, `serverRef`, `commitRef` gibi üç ref ve bunları güncelleyen bir efekt vardı. Bu "React'in dışında bir tesisat" gibi görünmüş ve state'ten okumak daha sade gelmiş. Ama ref'ler bir tasarım kararıydı: `change` bilerek bir kez oluşturuluyor (düğmeler her tıklamada yeniden çizilmesin) ve güncel değeri ref'ten okuyor. Eski yorum bunu açıkça yazıyordu; commit yorumun bu kısmını da sildi. Sonra `react-hooks/exhaustive-deps` kuralı `pending` ve `serverQuantity` eksik diye uyarı verdi ve uyarı satır sonuna `// eslint-disable-line react-hooks/exhaustive-deps` eklenerek susturuldu.

## Nasıl bulunur

1. **Tekrar edin.** `/sepet` sayfasında `+` düğmesine 4 kez basın. Tarayıcıda Network sekmesinde tek istek gider ve gövdesinde adet `2` yazar. Yani sorun ekranda değil, hesaplamada.
2. **Fonksiyonun gördüğünü yazdırın.** `useQuantityStepper.ts` içinde `change`'in başına geçici olarak ekleyin:

   ```ts
   console.log('change', { delta, pending, serverQuantity })
   ```

   4 tıklamada konsol dört kez `pending: null, serverQuantity: 1` yazar. Oysa ekrandaki adet 2 olmuştur. Fonksiyonun gördüğü değer ile ekranın gösterdiği değer ayrıştı; bayat kapanışın imzası budur.
3. **React DevTools.** *Components* sekmesinde `CartLineRow` bileşenini seçin, *hooks* bölümünde `State` değerinin (`pending`) tıklamayla değiştiğini görün. DevTools her zaman güncel state'i gösterir; konsoldaki `null` ile karşılaştırınca fark ortaya çıkar.
4. **Kesme noktası.** VS Code'da *Müşteri sitesi (Chrome)* yapılandırmasını başlatın (önce `npm run dev:storefront`), `change` içindeki `const base` satırına kesme noktası koyun. Durduğunda *Variables* panelinde *Closure* kapsamını açın: `pending` orada `null`. Visual Studio frontend hata ayıklaması için kullanılmaz; tarayıcı araçları ya da VS Code yeterlidir.
5. **Susturulmuş uyarıya bakın.** `grep -rn "eslint-disable" frontend/apps/storefront/src` çalıştırın. Çıkan satır tam olarak bu fonksiyonu gösterir. `git log -p -- frontend/apps/storefront/src/cart/useQuantityStepper.ts` ise yorumun silindiğini, `pending` okumasının geldiğini gösterir.

## Düzeltme

`change`, güncel adedi state'ten değil, zaten zamanlayıcının kullandığı ref'ten okur:

```diff
 /**
- * `change` bir kez oluşturulur; düğmeler her tıklamada yeniden çizilmesin.
+ * `change` bir kez oluşturulur (düğmeler her tıklamada yeniden çizilmesin) ve güncel adedi ref'ten
+ * okur. State'i kapanıştan (closure) okusaydı hep ilk çizimdeki değeri görürdü.
  */
@@
   const change = useCallback(
     (delta: number) => {
-      const base = pending ?? serverQuantity
+      const base = latest.current ?? serverRef.current
       const next = Math.min(max, Math.max(1, base + delta))
@@
-    [max], // eslint-disable-line react-hooks/exhaustive-deps
+    [max],
   )
```

Ref'ler `.current` üzerinden her zaman güncel değeri verir; kapanış ref nesnesini yakalar, içindeki değeri değil. Bu yüzden bağımlılık listesi `[max]` olarak doğrudur ve lint uyarısı kendiliğinden kalkar.

## Neden böyle düzeltiyoruz

- **Bağımlılıkları eklemek (`[max, pending, serverQuantity]`).** Doğru çalışır ve en basit düzeltmedir. Bedeli: `change` her state değişiminde yeni kimlik alır, adet düğmeleri her tıklamada yeniden çizilir. Burada bu bedel küçüktür; yeni bir hook yazıyor olsaydık tercih edilebilirdi. Ama dosya bilinçli olarak kararlı bir fonksiyon sunuyor ve zamanlayıcı da zaten ref'ten okuyor; ikinci bir doğruluk kaynağı açmak yerine mevcut düzeni korumak daha az risklidir.
- **Fonksiyonel `setState`: `setPending(p => ...)`.** "Önceki değerden hesapla" için doğru araç olduğu için FE-03 kataloğunda ilk önerilir ve sayaç gibi basit durumlarda yeterlidir. Burada yetmez: (1) başlangıç değeri `serverQuantity` hâlâ kapanıştan gelir; (2) zamanlayıcıyı kurmak ve sunucuya giden adedi bilmek bir yan etkidir, güncelleyici fonksiyonun (updater) içine konmaz (React geliştirme modunda onu iki kez çağırır); (3) zamanlayıcı güncel adedi başka bir yoldan zaten bilmek zorunda. Bu yüzden ref gerekir.
- **`eslint-disable` ile bırakıp "çalışıyor" demek.** Yukarıdaki hata tam da bu yorumun sakladığı hatadır. `exhaustive-deps` uyarısı "bu fonksiyon şu değişkenleri okuyor ama listede yok, bayat kalabilir" der. Uyarıyı susturmak yerine ya bağımlılığı eklemeli ya da değeri ref'e taşımalısınız.
- **Her tıklamada hemen istek göndermek (debounce'u kaldırmak).** Sorunu gizler ama 5 tıklama 5 istek olur, yarış durumları ve gereksiz yük doğar. Debounce bilinçli bir tasarımdır.

## Bir daha olmaması için

- **Test:** `src/__bolum__/fe03-sepet-adet.test.ts` zaten hızlı üç tıklamayı sınar. Gerçek kod tabanında bu tür hook'lar için "art arda N çağrı" testi standart olmalı; tek çağrılık testler bayat kapanışı yakalamaz.
- **Lint:** `react-hooks/exhaustive-deps` uyarı değil hata olsun. `eslint-disable` yorumları açıklama zorunlu olsun (`eslint-comments/require-description` kuralı) ve kullanılmayan disable yönergeleri raporlansın (`reportUnusedDisableDirectives`).
- **Kod inceleme:** Diff'te `eslint-disable` gören herkes durup sormalı: "Hangi uyarıyı susturuyor, neden doğru?" `exhaustive-deps` için bu soru neredeyse hiçbir zaman "güvenli" cevabı vermez; bir `eslint-disable` bir tehlike işaretidir. Silinen yorumlara da bakın: önceki yorum tasarım kararını açıklıyorsa silmek kararı geri almak demektir.
- **İzleme:** Sepet adedi değişiklikleri için istemci tarafında "gönderilen adet / son tıklamadaki adet" farkını telemetriye yazmak, bu tür sessiz hataları kullanıcı şikâyetinden önce gösterir.

## İlgili

HATALAR.md · **FE-03 Bayat kapanış (stale closure)**

## English summary

**Root cause:** `change` in `useQuantityStepper.ts` is created once by `useCallback(..., [max])` but reads state (`pending ?? serverQuantity`) inside. It therefore always sees the first render's values (a stale closure), so every click computes `1 + 1`. After 4 clicks the quantity is 2 on screen and on the server. The commit "Sepet adet düğmeleri: gösterilen adet doğrudan state'ten okunuyor" replaced the deliberate ref-based read, deleted the comment explaining it, and silenced the `exhaustive-deps` warning with `eslint-disable-line`.

**How to find it:** one request with quantity 2 in the Network tab; a `console.log` in `change` showing `pending: null` on every click while the screen shows 2; React DevTools and the Closure scope in the debugger; `grep eslint-disable`.

**Fix:** read the current quantity from the refs (`latest.current ?? serverRef.current`) and remove the `eslint-disable`. Adding the dependencies is also correct but makes the function identity change on every click. A functional `setState` alone is not enough because the timer needs the latest value and side effects do not belong in an updater.

**Prevent:** a test with several rapid calls, `exhaustive-deps` as an error, required descriptions on lint-disable comments, and treating any `eslint-disable` in review as a warning sign.
