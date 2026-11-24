> **Cevap anahtarı.** Bölümü kendiniz çözmeden okumayın; önce `ipucu` komutunu deneyin.

# Çözüm · Bölüm 32 (GÜV-02) · Paylaşılan oturum durumu ve yenileme anahtarı yarışı

## Kök neden

Sunucu, oturumu yenilerken **anahtar döndürme** (refresh token rotation) uygular: `AuthService.RefreshAsync` gelen yenileme anahtarını doğrular, **eskisini iptal eder** ve yenisini verir (`IssueAsync`). Aynı anahtar ikinci kez gelirse `invalid_refresh_token` hatası döner. Bu güvenlik açısından doğru bir tasarımdır: çalınmış bir anahtar bir kez kullanıldığında fark edilir.

Bunun istemci tarafında bir sonucu var: aynı oturumu paylaşan birden çok sekme **aynı anda** yenilemeye kalkarsa yalnızca biri başarılı olur. Müşteri sitesinde oturum `localStorage`'da (`tezgah.session`) durur ve her sekme kendi belleğinde bir kopya tutar (`tokenStore.ts`, `current`). Önceki sürüm bunu iki yerde yönetiyordu; hatalı commit ikisini de kaldırmış:

```diff
 // frontend/apps/storefront/src/auth/tokenStore.ts
-// Başka bir sekme oturumu yenileyince ya da çıkış yapınca bu sekme de aynı oturumu görür.
-if (typeof window !== 'undefined') {
-  window.addEventListener('storage', (event) => {
-    if (event.key !== KEY && event.key !== null) return
-    current = read()
-    listeners.forEach((l) => l(current))
-  })
-}
...
-  /** Depodaki son oturumu yeniden okur (başka bir sekmenin yazdığı dahil). */
-  reload() {
-    current = read()
-    return current
-  },
```

```diff
 // frontend/apps/storefront/src/api/client.ts  (refreshTokens)
       if (!response.ok) {
-        // Başka bir sekme aynı anda yenilemiş olabilir: bu anahtar artık geçersizdir ama depoda yenisi vardır.
-        const latest = tokenStore.reload()
-        if (latest && latest.refreshToken !== refreshToken) return true
         tokenStore.clear()
         return false
       }
```

Hatalı sürümde olay şöyle gelişir (erişim süresi dolmuş, iki sekme açık):

1. Sekme A bir istek atar, `401` alır ve `/api/auth/refresh` ile yeniler: sunucu eski anahtarı **iptal eder**, yeni oturumu verir; A bunu `localStorage`'a yazar.
2. Sekme B'nin belleğindeki `current` hâlâ **eski** oturumu taşır (artık `storage` olayını dinlemiyor). B bir istek atar, `401` alır ve eski yenileme anahtarıyla yenilemeyi dener.
3. Sunucu anahtarı iptal edilmiş olarak görür ve `401` döner.
4. `refreshTokens` yenileme başarısız olunca doğrudan `tokenStore.clear()` çağırır: bu, `localStorage`'daki **A'nın yazdığı geçerli yeni oturumu da siler**.
5. B giriş sayfasına yönlenir; A'nın bir sonraki işleminde depo boş olduğundan A da oturumunu yitirir.

Yani hata, "sekmeler arası paylaşılan bir durum üzerinde, sunucuyla senkron olmayan yerel kopyalar" yüzünden doğan bir **yarış**tır. Tek sekmede çalışmaz çünkü tek bir kopya vardır.

Değişikliği yapan geliştirici şunu düşünmüş (commit `abc1234`):

> Oturum deposu sadeleştirildi
> storage olayı her yazımda diğer sekmelerdeki tüm aboneleri tetikleyip sayfayı yeniden çiziyordu. Yenileme başarısızsa oturum kapatılır; geçersiz anahtarla devam etmenin yolu bırakılmadı.

"Gereksiz yeniden çizim" kaygısı ölçülebilir bir şikâyet olabilir, ama kaldırılan kod bir **işlev** taşıyordu: sekmeleri eşitlemek. Yenileme başarısız olduğunda "oturumu kapat" demek de yanlış varsayımdır: başarısızlık her zaman anahtarın gerçekten geçersiz olduğu anlamına gelmez; başka bir sekmenin yeni oturum yazmış olması da mümkündür.

## Nasıl bulunur

1. **Belirti:** Yalnızca iki sekmeyle, erişim süresi dolduktan sonra, her iki sekmenin birden oturum kaybı. "Başka sekme" ve "süre" ayrıntıları paylaşılan durum ve yenileme yarışını gösterir.
2. **Network sekmesi:** İki sekmede `/api/auth/refresh` istekleri: A'nın isteği `200`, B'ninki `401` (`invalid_refresh_token`). B'nin isteğindeki `refreshToken`, A'nın yeni aldığı değerden farklıdır.
3. **Application sekmesi:** `tezgah.session` değeri B'nin `401`inden sonra silinir; A'nın yazdığı oturum yok olur.
4. **Kod:** `tokenStore.ts` içinde `storage` dinleyicisi ve `reload()` yok; `client.ts` içindeki `refreshTokens` doğrudan `clear()` çağırıyor. `git log -p` kaldırılan satırları gösterir.
5. **Süreyi kısaltmak:** API'yi `Jwt__AccessTokenMinutes=1` ile başlatmak (sınırlı gecikme: sunucu 30 sn saat toleransı uygular, süre yaklaşık 1,5 dakika sonra dolar) sorunu dakikalar içinde tekrar edilebilir kılar.

**Araç ipucu:** Chrome DevTools'ta iki ayrı pencere ya da sekmenin Network kayıtlarını yan yana tutmak, "hangi sekme hangi anahtarla yeniledi" sorusunu görünür yapar. *Preserve log* kutusunu işaretlemeyi unutmayın; yönlendirme günlüğü sıfırlar.

## Düzeltme

Sekmeler arası eşitlemeyi ve başarısızlıkta depoyu yeniden okumayı geri getirin:

```diff
 // tokenStore.ts
+// Başka bir sekme oturumu yenileyince ya da çıkış yapınca bu sekme de aynı oturumu görür.
+if (typeof window !== 'undefined') {
+  window.addEventListener('storage', (event) => {
+    if (event.key !== KEY && event.key !== null) return
+    current = read()
+    listeners.forEach((l) => l(current))
+  })
+}
 ...
 export const tokenStore = {
   get: () => current,
+  /** Depodaki son oturumu yeniden okur (başka bir sekmenin yazdığı dahil). */
+  reload() {
+    current = read()
+    return current
+  },
```

```diff
 // client.ts (refreshTokens)
       if (!response.ok) {
+        // Başka bir sekme aynı anda yenilemiş olabilir: bu anahtar artık geçersizdir ama depoda yenisi vardır.
+        const latest = tokenStore.reload()
+        if (latest && latest.refreshToken !== refreshToken) return true
         tokenStore.clear()
         return false
       }
```

Böylece B, `401` aldığında depoyu yeniden okur, A'nın yazdığı yeni oturumu görür ve onunla devam eder; `clear()` yalnızca gerçekten geçersiz bir oturumda çalışır. `storage` dinleyicisi de A'nın yazdığı oturumu B'nin belleğine **önceden** taşır, böylece B çoğu zaman hiç `401` almaz.

## Neden böyle düzeltiyoruz

- **Sunucuda anahtar döndürmeyi bırakmak.** Sorunu gizler ama güvenlik kazanımını (çalınan anahtarın tespiti, kullanım sonrası iptali) yok eder. Sorun sunucunun doğru davranışında değil, istemcinin eşitlenmemesinde.
- **Eski anahtara birkaç saniyelik "tolerans penceresi" tanımak (sunucu tarafı).** Ek bir savunma olarak düşünülebilir, ancak anahtar döndürmenin güvenlik garantisini zayıflatır ve tek başına yarışı bitirmez; istemci yine de eşitlenmelidir.
- **Başarısız yenilemede hiç çıkış yapmamak.** Gerçekten süresi dolmuş ya da iptal edilmiş bir oturumda kullanıcı sonsuza kadar hatalı isteklerle baş başa kalır; `clear()` doğru, yeter ki önce depo yeniden okunsun.
- **Her sekmede periyodik yenileme zamanlayıcısı.** Yarışı azaltmaz, çoğaltır: ne kadar çok sekme o kadar çok eşzamanlı yenileme.

Daha sağlam bir çözüm (isteğe bağlı): **tek yenileme** için `navigator.locks.request('tezgah-refresh', ...)` kullanın. Kilidi alan sekme yenileme yapar, diğerleri beklerken kilit açılınca depodaki taze oturumu okur. `BroadcastChannel` ile sonucun diğer sekmelere duyurulması da benzer sonucu verir ve `storage` olayına bağımlılığı azaltır.

## Bir daha olmaması için

- **Test:** İki "sekme"yi (iki ayrı `tokenStore` örneğini ya da iki sayfa bağlamını) simüle eden bir arayüz testi: A yeniler, B eski anahtarla `401` alır; beklenen B'nin A'nın oturumuyla devam etmesi ve depoda oturumun silinmemesidir. Bölümün kontrolü tam olarak bunu sınar.
- **Kod inceleme maddesi:** Paylaşılan bir depoya (`localStorage`, çerez, `IndexedDB`) yazan ya da oradan okuyan kodu silen PR'larda "diğer sekmeler / diğer cihazlar bundan nasıl haberdar olacak?" sorusu. "Sadeleştirme" gerekçesiyle kaldırılan olay dinleyicilerinin neyi koruduğunu yazın.
- **Tasarım ilkesi:** Dönen anahtar kullanan her istemci, aynı anahtarla eşzamanlı yenilemeyi **tekilleştirmeli** (tek uçuştaki istek, kilit) ve yenileme başarısız olduğunda önce durumun gerçekten geçersiz olduğunu doğrulamalıdır.
- **İzleme:** `invalid_refresh_token` oranı: sıradan kullanımda çok düşük olmalıdır. Ani artış ya istemci yarışı ya da anahtar hırsızlığı işaretidir.
- **Yeniden çizim kaygısı:** Eşitleme olayı yalnızca değer **değiştiyse** aboneleri tetiklesin (`next.refreshToken !== current?.refreshToken`); böylece ilk şikâyet de ölçülü biçimde çözülür.

## İlgili

HATALAR.md · **GÜV-02 Yenileme token'ı yarışı**

## English summary

**Root cause:** the server rotates refresh keys: `AuthService.RefreshAsync` revokes the key it receives and issues a new one, so the same key cannot be used twice. The storefront keeps the session in `localStorage` and each tab holds an in-memory copy (`tokenStore.ts`). The faulty commit removed the `storage` event listener and `tokenStore.reload()`, and removed from `refreshTokens` in `client.ts` the check "did another tab already write a newer session?". Now, when tab A refreshes and writes the new session, tab B still holds the old key, refreshes with it, gets `401`, and `tokenStore.clear()` wipes the valid session A just wrote: both tabs are logged out. The developer (commit "Oturum deposu sadeleştirildi") wanted to avoid needless re-rendering, but the removed code kept tabs in sync.

**How to find it:** the problem shows only with two tabs after the access time runs out; Network shows A's `/api/auth/refresh` is `200` and B's is `401`; the stored session disappears after B's `401`; `git log -p` shows the removed listener, `reload()` and check. Starting the API with `Jwt__AccessTokenMinutes=1` makes it reproducible in minutes.

**Fix:** restore the `storage` listener and `reload()`, and in `refreshTokens` re-read the store on failure and continue if a newer session is there; more robustly, use `navigator.locks` / `BroadcastChannel` so only one tab refreshes.

**Prevent:** a two-tab UI test, review of removed event listeners on shared state, de-duplicating concurrent refreshes, and monitoring the `invalid_refresh_token` rate.
