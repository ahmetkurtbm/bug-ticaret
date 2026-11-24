> **Cevap anahtarı.** Bölümü kendiniz çözmeden okumayın; önce `ipucu` komutunu deneyin.

# Çözüm · Bölüm 30 (GÜV-05) · Depolanan XSS: onay süreci güvenlik kontrolü değildir

## Kök neden

`frontend/apps/storefront/src/pages/ProductPage.tsx` içindeki `ProductView` bileşeni ürün açıklamasını ham HTML olarak basmaya başlamış:

```tsx
<div className="text-sm leading-relaxed text-muted [&_li]:ml-5 [&_ul]:list-disc"
     dangerouslySetInnerHTML={{ __html: product.description }} />
```

React bir değeri normalde **metin olarak** yerleştirir ve `<`, `>` gibi karakterleri kaçışlar; satıcının yazdığı `<img ...>` ekranda yazı olarak görünür. `dangerouslySetInnerHTML` ise bu korumayı bilerek kapatır: değer tarayıcının HTML ayrıştırıcısına verilir ve içindeki etiketler sayfanın gerçek parçası olur. Bir `<img src=x onerror="...">` yüklenemeyen görüntü için olay işleyicisini çalıştırır; yani satıcının yazdığı **herhangi bir JavaScript** müşterinin tarayıcısında, müşteri sitesinin kimliğiyle (aynı origin) çalışır. Buna **depolanan XSS** (stored cross-site scripting) denir; OWASP Top 10'da **A05 Injection** başlığı altındadır.

Etki yalnızca bir uyarı penceresi değildir. Müşteri oturumu (`tezgah.session`: erişim ve yenileme anahtarları) tarayıcıda `localStorage` içinde durur ve aynı origin'de çalışan her kod onu okuyabilir. Saldırgan satıcı, ürün sayfasını açan her müşterinin oturumunu çalan, sepetini değiştiren ya da müşteri adına istek gönderen bir kod yerleştirebilir.

Değişikliği yapan geliştirici şunu düşünmüş (commit `abc1234`):

> Ürün açıklamasında satıcının biçimlendirmesi görünüyor
> Satıcılar `<b>` ve `<ul>` kullanıyor; düz metne çevirince açıklamalar tek paragrafa yığılıyordu. Ürün değişiklikleri zaten yönetici onayından geçiyor.

İhtiyaç gerçek (biçimlendirme), ama gerekçe yanlış: **onay süreci bir güvenlik kontrolü değildir.** Onaylayan kişi ekranda görünen metne bakar; görünmeyen bir etiketi, kodlanmış bir değeri ya da onay sonrası değiştirilen bir açıklamayı fark etmesi beklenemez. Güvenlik, insanların dikkatine değil, koda dayanmalıdır. Önceki sürümün yorumu da aynısını söylüyordu: "satıcı HTML'i sunucuda temizlenene kadar HTML olarak basılmaz."

## Nasıl bulunur

1. **Belirti:** Yalnızca belirli bir üründe, sayfa açılır açılmaz beklenmedik bir pencere. Kaynak bir satıcı içeriği; kullanıcı girdisi yeniden basılıyor demektir.
2. **Elements sekmesi:** "Ürün açıklaması" kutusunda açıklama bir metin düğümü değil, gerçek bir `<img src="x" onerror="alert('Tezgah')">` öğesi olarak görünür.
3. **Kod:** `ProductPage.tsx` içinde `dangerouslySetInnerHTML` aratın (`git grep dangerouslySetInnerHTML -- frontend`). Adındaki "dangerously" bir uyarıdır; her kullanımı kullanıcı kaynaklı veri açısından incelenmelidir. `git log -p -- frontend/apps/storefront/src/pages/ProductPage.tsx` eklenen satırı ve gerekçesini gösterir.
4. **Veri akışı:** Açıklama satıcı panelinden gelir, ürün onayından geçer ve müşteri sitesine API üzerinden aynen ulaşır; yolun hiçbir yerinde temizlenmez.

**Araç ipucu:** Konsolda `JSON.parse(localStorage.getItem('tezgah.session'))` yazarak oturum bilgisinin aynı origin'deki kod için ne kadar ulaşılabilir olduğunu görün; uyarı penceresi yerine bu değeri okuyan bir kod olsaydı ne olacağını düşünün.

## Düzeltme

En basit ve güvenli yol: açıklamayı yeniden **düz metin** olarak göstermek.

```diff
-          {/* Satıcılar açıklamada kalın yazı ve liste kullanıyor; biçim korunur. */}
-          <div className="text-sm leading-relaxed text-muted [&_li]:ml-5 [&_ul]:list-disc" dangerouslySetInnerHTML={{ __html: product.description }} />
+          {/* Açıklama düz metin olarak gösterilir; satıcı HTML'i sunucuda temizlenene kadar HTML olarak basılmaz. */}
+          <p className="whitespace-pre-line text-sm leading-relaxed text-muted">{product.description.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim()}</p>
```

Biçimlendirme gerçekten isteniyorsa iki katmanlı bir çözüm gerekir:

1. **Sunucuda, kaydederken temizlemek** (asıl çözüm). API açıklamayı kaydetmeden önce izinli bir etiket listesine (`b`, `strong`, `ul`, `li`, `br`, `p`) göre temizler; öznitelikleri (`onerror`, `style`, `href="javascript:..."`) tamamen atar. Böylece veritabanındaki veri her istemci için güvenlidir.
2. **İstemcide bir temizleyiciyle basmak** (ikinci savunma hattı): `DOMPurify.sanitize(product.description, { ALLOWED_TAGS: ['b', 'strong', 'ul', 'li', 'br', 'p'], ALLOWED_ATTR: [] })` çıktısını `dangerouslySetInnerHTML`'e vermek.
3. **İçerik güvenlik politikası (CSP):** Müşteri sitesine `script-src 'self'` (satır içi olay işleyicilerine izin vermeyen) bir CSP başlığı ekleyin; temizleme atlansa bile saldırı çalışmaz.

## Neden böyle düzeltiyoruz

- **Yönetici onayına güvenmek.** Onay, içeriğin uygunluğunu denetler; kod enjeksiyonunu denetlemez. Görünmeyen etiket, onaydan sonra düzenlenen metin ya da yönetici hesabının ele geçirilmesi bu korumayı geçersiz kılar.
- **Düzenli ifadeyle etiketleri ayıklamak** (`replace(/<[^>]+>/g, '')`). Yalnızca düz metin olarak basıldığında yeterlidir, çünkü React zaten kaçışlar. HTML olarak basılacak veriyi temizlemek için yetersizdir: HTML ayrıştırıcıları düzenli ifadelerin öngöremediği biçimlerde bozuk girdileri bile yorumlar.
- **Kara liste (`<script>` ve `onerror` yasağı).** Saldırgan başka etiket ve öznitelikle aynı sonucu alır; her zaman **izin verilen listesi** (allowlist) kullanın.
- **Yalnızca istemcide temizlemek.** API'yi kullanan başka istemciler (mobil uygulama, e-posta şablonu, yönetim paneli) aynı veriyi ham alır; temizleme kaynağa yakın, sunucuda yapılmalıdır.
- **Oturum bilgisini `localStorage`'dan taşımak** (`HttpOnly` çerez) iyi bir ek savunmadır ama XSS'i çözmez: saldırgan kod kullanıcı adına yine istek gönderebilir.

## Bir daha olmaması için

- **Test:** Ürün sayfası için bir arayüz testi: açıklamaya `<img src=x onerror="...">` yazılan ürün açılınca hiçbir `alert` ya da `onerror` çalışmasın. Bölümün kontrolü tam olarak bunu yapar. Sunucu temizliği için de API testi: kaydedilen açıklamada etiket/öznitelik kalmadığını doğrulayın.
- **Kod inceleme maddesi:** `dangerouslySetInnerHTML`, `innerHTML` ya da `v-html` gören yoruma şu soruyu sorun: "Bu veri nereden geliyor, kim yazabiliyor, nerede temizleniyor?" Cevap "onay süreci" ise PR kabul edilmesin.
- **Lint kuralı:** `react/no-danger` kuralını açın; kullanım gerekiyorsa gerekçeli bir istisna yazılsın.
- **CSP ve çerez ayarları:** Üretimde sıkı bir `Content-Security-Policy`, oturum için `HttpOnly` + `SameSite` çerez ve kısa ömürlü erişim anahtarları.
- **İzleme:** CSP ihlal raporları (`report-uri`) bu sınıf saldırıları erken gösterir.

## İlgili

HATALAR.md · **GÜV-05 XSS: ürün açıklamasında script**

## English summary

**Root cause:** `ProductView` in `frontend/apps/storefront/src/pages/ProductPage.tsx` now prints the product description with `dangerouslySetInnerHTML={{ __html: product.description }}`. React normally escapes values, so a tag a seller writes shows up as text; this prop turns that off, so a seller-written `<img src=x onerror="alert('Tezgah')">` becomes a real element and its handler runs on the storefront origin: stored XSS (OWASP A05 Injection). Because the customer session is kept in `localStorage`, the same code could steal it. The developer (commit "Ürün açıklamasında satıcının biçimlendirmesi görünüyor") wanted bold text and lists, and relied on "product changes already go through admin approval". An approval process is not a security control: the approver sees the rendered text, not invisible tags.

**How to find it:** the Elements tab shows a real `<img onerror=...>` element in the description box; `git grep dangerouslySetInnerHTML` and `git log -p` on `ProductPage.tsx` show the change and its reasoning; the data goes from seller panel to storefront unsanitized.

**Fix:** go back to showing the description as plain text; if formatting is really needed, sanitize on the server when saving (allowlist of tags, no attributes), optionally sanitize again with DOMPurify on the client, and add a Content-Security-Policy that blocks inline handlers.

**Prevent:** a UI test with an `onerror` payload, API tests for server-side sanitizing, review attention and a lint rule (`react/no-danger`) for raw-HTML rendering, strict CSP, and CSP violation reporting.
