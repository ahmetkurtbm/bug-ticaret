> **Cevap anahtarı.** Bölümü kendiniz çözmeden okumayın; önce `ipucu` komutunu deneyin.

# Çözüm · Bölüm 23 (FE-10) · Çift tıklama ile çift gönderim

## Kök neden

`POST /api/orders` ucu `Idempotency-Key` başlığını destekler: aynı kullanıcı aynı anahtarla ikinci kez istek yaparsa sunucu yeni sipariş açmaz, ilk siparişin sonucunu döndürür (`OrderService`, `IdempotencyKey` ile aranır). Bu koruma, **istemcinin aynı niyet için aynı anahtarı göndermesine** dayanır.

`frontend/apps/storefront/src/pages/CheckoutPage.tsx` içinde iki koruma birden kaldırılmış. Hatalı hâl yaklaşık şöyle:

```tsx
const place = useMutation({
  // her çağrıda yeni anahtar
  mutationFn: (addressId: number) => orderApi.place(addressId, installments, crypto.randomUUID()),
  ...
})

function onSubmit(e: FormEvent) {
  e.preventDefault()
  if (effectiveId !== null) place.mutate(effectiveId)
}

<button type="submit" disabled={effectiveId === null} ...>
```

1. **Anahtar istek başına üretiliyor.** Her tıklama `crypto.randomUUID()` ile yeni bir anahtar taşıdığı için sunucu iki isteği iki ayrı niyet sayar ve iki sipariş açar. İdempotentlik anahtarı tam olarak bunu önlemek için vardır; ama her istekte değişiyorsa anlamsızdır.
2. **Düğme istek sürerken kilitlenmiyor.** `disabled` koşulundan `place.isPending || place.isSuccess` ve `onSubmit` içindeki `!place.isPending` denetimi kalkmış. Yavaş bağlantıda ilk istek bitmeden ikinci ve üçüncü basış engellenmiyor.

Değişikliği yapan geliştirici şunu düşünmüş (commit `abc1234`):

> Ödeme: Safari'de geri tuşundan sonra sipariş verilemiyor (bfcache)
> Sayfa geri tuşuyla bellekten geldiğinde eski anahtar ve "bekleyen" durum takılı kalıyordu; anahtarı her denemede yeniden üret, düğmeyi kilitleme.

Sorun gerçek: Safari'de geri tuşuna basınca sayfa **bfcache**'ten (back/forward cache) bellek görüntüsüyle geri gelir; React durumu (`isSuccess`) ve `useRef` içindeki anahtar eski kalır. Düğme "Ödeme sayfasına yönlendiriliyor…" diye kilitli, anahtar da eski siparişin anahtarı olduğundan sunucu yeni sipariş açmak yerine eskisini döndürür. Geliştirici bu belirtiyi, korumaları kaldırarak "çözmüş"; asıl sorunu çözmeden iki başka sorunu açmış.

Ders: **idempotentlik anahtarı "aynı niyet" başına üretilir, istek başına değil.** Kullanıcının "bu sepeti ödemek istiyorum" niyeti tek; ağ kaç kez yeniden denerse denesin ya da kullanıcı kaç kez basarsa bassın anahtar aynı kalır. Yeni bir niyet (hata sonrası yeniden deneme, yeni sepet, geri tuşuyla dönüş) yeni anahtar demektir.

## Nasıl bulunur

1. **Belirti:** Yalnızca hızlı art arda basışta ya da yavaş bağlantıda; tek basışta yok. Zamanlama bağımlı hatalar genellikle "ilk istek bitmeden ikincisi gidiyor" demektir.
2. **Araç: Network sekmesi** ("Slow 3G" ile). `POST /api/orders` üç kez gider; her birinin *Request Headers* kısmında `Idempotency-Key` değeri **farklıdır**. Doğru davranışta aynı değer ya da yalnızca bir istek görülürdü.
3. **Düğmeye bakın.** İlk basıştan sonra düğme soluk/kilitli olmalıydı; burada tıklanabilir kalıyor. Elements sekmesinde `disabled` özniteliği görünmez.
4. **Kod:** `CheckoutPage.tsx` içinde `mutationFn`, `onSubmit` ve düğmenin `disabled` koşulu. `git log -p -- frontend/apps/storefront/src/pages/CheckoutPage.tsx` kaldırılan `useRef` anahtarını, `isPending` denetimlerini ve commit mesajını (bfcache gerekçesi) gösterir.
5. **Sipariş verisi:** `SELECT order_number, idempotency_key, status FROM orders WHERE user_id = ... ORDER BY id DESC LIMIT 5;` aynı sepetin her siparişinde farklı `idempotency_key` gösterir.

## Düzeltme

Anahtarı deneme başına sabit tutun, hata olunca yenileyin, düğmeyi istek sürerken kilitleyin; bfcache sorununu ise `pageshow` olayıyla ele alın:

```diff
+  // Aynı ödeme denemesinde anahtar sabit: butona iki kez basılsa da tek sipariş açılır.
+  const idempotencyKey = useRef(crypto.randomUUID())

   const place = useMutation({
-    mutationFn: (addressId: number) => orderApi.place(addressId, installments, crypto.randomUUID()),
+    mutationFn: (addressId: number) => orderApi.place(addressId, installments, idempotencyKey.current),
     onSuccess: ...,
+    onError: () => {
+      // Sipariş açılamadıysa yeni deneme yeni anahtarla gider.
+      idempotencyKey.current = crypto.randomUUID()
+    },
   })

   function onSubmit(e: FormEvent) {
     e.preventDefault()
-    if (effectiveId !== null) place.mutate(effectiveId)
+    if (effectiveId !== null && !place.isPending) place.mutate(effectiveId)
   }

-  <button type="submit" disabled={effectiveId === null} ...>
+  <button type="submit" disabled={effectiveId === null || place.isPending || place.isSuccess} ...>
```

Safari geri tuşu (bfcache) için gerçek çözüm: sayfa bellekten geri geldiğinde (`event.persisted`) durumu sıfırlayıp yeni niyet başlatmak:

```tsx
useEffect(() => {
  const onPageShow = (e: PageTransitionEvent) => {
    if (e.persisted) {
      place.reset()                              // eski "başarılı/bekliyor" durumunu temizle
      idempotencyKey.current = crypto.randomUUID() // geri dönüş yeni bir niyettir
    }
  }
  window.addEventListener('pageshow', onPageShow)
  return () => window.removeEventListener('pageshow', onPageShow)
}, [place])
```

İki katman da gerekli: düğme kilidi çoğu çift basışı istemcide keser (iyi kullanıcı deneyimi), anahtar ise kilidin yetmediği durumları (ağ yeniden denemesi, ikinci sekme, hızlı çift olay) sunucuda keser.

## Neden böyle düzeltiyoruz

- **Yalnızca düğmeyi kilitlemek.** `disabled` durum güncellemesinin yeniden çizimi beklemesi gerektiğinden çok hızlı iki tıklama arasında dar bir pencere bırakabilir; ağ yeniden denemeleri ve ikinci sekme de kilidi aşar. Sunucu tarafı idempotentlik asıl güvencedir.
- **Yalnızca anahtara güvenmek.** Çalışır ama gereksiz istek gönderir ve kullanıcı geri bildirimi vermez (donmuş gibi görünür). Kilit, kullanıcıya "işlem sürüyor" der.
- **Debounce / zamanlayıcıyla sınırlamak.** Süreyi tahmin etmeye dayanır; yavaş bağlantıda süre yetmez. Durum (istek sürüyor mu) süreden daha güvenilirdir.
- **Korumaları kaldırıp bfcache'i devre dışı bırakmak** (`Cache-Control: no-store`, `unload` olayı). Tüm sitenin geri/ileri hızını düşürür; belirtiyi saklar. `pageshow` ile hedefli sıfırlama daha dar ve doğrudur.
- **Sunucuda "son N saniyede aynı sepet" kontrolü.** Anahtarsız istemciler için ek bir emniyettir, ama aynı sepeti bilerek iki kez siparişe çevirmeyi de engelleyebilir; birincil çözüm yerine geçmez.

## Bir daha olmaması için

- **Test:** Arayüz testi: ödeme sayfasını sahte yavaş yanıtla (bitmeyen bir `Promise`) çizin, "Ödemeye geç"e üç kez tıklayın ve sipariş isteğinin **bir kez** gittiğini doğrulayın. Ek olarak, art arda gelen iki isteğin aynı `Idempotency-Key`'i taşıdığını ve hatadan sonra anahtarın değiştiğini sınayın. Bölümün kontrolü bunu yapar.
- **Test (API):** Aynı `Idempotency-Key` ile iki (ve paralel beş) `POST /api/orders`: tek sipariş, aynı yanıt.
- **Kod inceleme maddesi:** Bir hatayı "korumayı kaldırarak" çözen PR'da korumanın neden vardı olduğunu sorun. Mesajda iki ayrı davranışın (idempotentlik ve düğme kilidi) değiştirildiği belirtilmeliydi; "Safari düzeltmesi" başlığı altında bunlar gözden kaçtı.
- **Tarayıcı matrisi:** bfcache davranışı tarayıcıya göre değişir; geri/ileri senaryosunu elle ya da Playwright'ın WebKit motoruyla deneyin.
- **İzleme:** Aynı kullanıcıdan kısa sürede aynı tutarlı birden fazla `PendingPayment` siparişi sayan bir sorgu ya da uyarı.
- **Veri onarımı:** Hatalı sürümle oluşan fazla `PendingPayment` siparişler süre dolumuyla iptal edilir (stok rezervasyonu serbest kalır); müşteri ödemeye yalnızca bir tanesini tamamlarsa diğerleri kendiliğinden düşer. Müşteri hizmetlerine, ödeme alınmadığı için yinelenen bir ücretlendirme olmadığı bildirilmelidir.

## İlgili

HATALAR.md · **FE-10 Çift tıklama ile çift gönderim**

## English summary

**Root cause:** `POST /api/orders` supports an `Idempotency-Key` header: a second request with the same key from the same user does not create another order. The checkout page was changed so that the key is generated on every call (`crypto.randomUUID()` inside `mutationFn`) instead of being kept steady with `useRef`, and the button's `disabled` condition (`place.isPending || place.isSuccess`) and the `!place.isPending` check in `onSubmit` were removed. On a slow connection, repeated presses send several requests with different keys and the server creates several orders. The developer (commit "Ödeme: Safari'de geri tuşundan sonra sipariş verilemiyor (bfcache)") removed protections to work around a back/forward cache problem.

**How to find it:** with "Slow 3G", several `POST /api/orders` requests carry different `Idempotency-Key` values; the button stays enabled; read `mutationFn`, `onSubmit` and `disabled`; `git log -p`.

**Fix:** keep the key in a `useRef` per attempt and renew it on error; disable the button while pending or after success; handle bfcache with a `pageshow` listener (`event.persisted`) that resets the mutation and creates a new key. The idempotency key is created per intent, not per request.

**Prevent:** a UI test with a never-resolving request and several clicks, an API test with parallel same-key requests, review of removed protections, a browser matrix for back/forward behavior, and monitoring of duplicate pending orders.
