# Şirkette karşılaşılan hatalar listesi

Bu liste projenin bölüm kataloğudur. Her hata, pazar yeri kod tabanında bir ayrı git dalı olarak
kurulur. Hepsi gerçek şirket projelerinde sık görülen hatalardır.

Her kayıtta:
- **Belirti:** Öğrencinin ya da müşterinin gördüğü şey (hata mesajı, yanlış ekran, şikâyet).
- **Kök neden:** Hatanın asıl sebebi.
- **Nasıl bulunur:** Debug yolu; hangi araçla, hangi sırayla.
- **Çözüm:** Doğru düzeltme ve tekrar etmemesi için ne yapılır.
- **Projede:** Pazar yerinde hangi ekranda/modülde kurulacak.
- **Seviye:** 1 (ilk hafta) → 5 (canlıda nöbet). **Video:** Çözüm anlatımına ileride video eklensin mi.

Kodlar: **FE** frontend (React) · **API** HTTP/API sözleşmesi · **NET** .NET backend · **EF** Entity
Framework Core · **DB** PostgreSQL · **EŞ** eşzamanlılık ve dağıtık sistemler · **GÜV** kimlik ve
güvenlik · **ORT** ortam, Docker, ayar · **MS** mikroservis geçişi · **PERF** performans ve bellek ·
**ARAÇ** git, bağımlılık, test.

---

## FE — Frontend (React)

### FE-01 Koşullu hook çağrısı
- **Belirti:** "Rendered more hooks than during the previous render" ya da "Rendered fewer hooks than expected" ve sayfa beyaz.
- **Kök neden:** `useState`/`useEffect` bir `if` içinde ya da erken `return`'den sonra çağrılmış. React hook'ları çağrılma sırasıyla eşleştirir; sıra değişince çöker.
- **Nasıl bulunur:** Konsoldaki bileşen yığınından (component stack) hangi bileşen olduğunu bul. O bileşende `return` ve `if`'lerin hook'lardan önce olup olmadığına bak. `eslint-plugin-react-hooks` bunu zaten yakalar; kapatılmış mı kontrol et.
- **Çözüm:** Tüm hook'ları bileşenin en üstüne, koşulsuz al; koşulu hook'un içine taşı. ESLint kuralını CI'da zorunlu yap.
- **Projede:** Ürün detay sayfası: "ürün yoksa erken return" sonra `useEffect`.
- **Seviye:** 1 · **Video:** hayır

### FE-02 Sonsuz render döngüsü (useEffect bağımlılığı)
- **Belirti:** Sayfa donuyor, Network sekmesinde aynı API isteği saniyede onlarca kez gidiyor.
- **Kök neden:** `useEffect` her render'da yeni oluşan bir nesne/dizi/fonksiyona bağımlı; ya da efekt içinde, bağımlılığı olan state'i değiştiriyor.
- **Nasıl bulunur:** Network sekmesinde tekrar eden isteği bul, "Initiator" ile hangi bileşenden geldiğine bak. React DevTools Profiler ile "neden render oldu" seçeneğini aç.
- **Çözüm:** Bağımlılığı ilkel değere indir (ör. `filters.categoryId`), nesneyi `useMemo` ile sabitle ya da veri çekmeyi TanStack Query gibi bir kütüphaneye bırak.
- **Projede:** Kategori sayfasında filtre nesnesi her render'da yeniden oluşuyor.
- **Seviye:** 1 · **Video:** hayır

### FE-03 Bayat kapanış (stale closure)
- **Belirti:** Sepet sayacı "+" butonuna hızlı basınca 1 artıyor, 5 değil. Ya da `setInterval` içinde hep ilk değer görülüyor.
- **Kök neden:** Fonksiyon, oluşturulduğu render'daki eski state'i yakalamış. `setCount(count + 1)` art arda çağrılınca hepsi aynı `count`'u görür.
- **Nasıl bulunur:** Fonksiyonun içine breakpoint koy, `count` değerine bak; beklenenden eski olduğunu gör.
- **Çözüm:** Fonksiyonel güncelleme: `setCount(c => c + 1)`. Interval/efektlerde `useRef` ya da doğru bağımlılık.
- **Projede:** Sepette adet artırma butonu.
- **Seviye:** 2 · **Video:** evet

### FE-04 Temizlenmeyen efekt (bellek sızıntısı)
- **Belirti:** Sayfadan çıkıp dönünce istekler iki katına çıkıyor; WebSocket/interval kapanmıyor; uzun kullanımda tarayıcı yavaşlıyor.
- **Kök neden:** `useEffect` içinde açılan abonelik, interval ya da event listener için temizleme (cleanup) fonksiyonu yazılmamış.
- **Nasıl bulunur:** Memory sekmesinde heap snapshot al, sayfaya birkaç kez gir-çık, nesne sayısının arttığını gör. Event Listeners panelinde birikimi kontrol et.
- **Çözüm:** Efektten temizleme fonksiyonu döndür (`return () => clearInterval(id)`); fetch için `AbortController`.
- **Projede:** Satıcı panelinde canlı sipariş bildirimi.
- **Seviye:** 2 · **Video:** hayır

### FE-05 Eşzamanlı isteklerde yanlış sonuç (race condition)
- **Belirti:** Aramaya hızlı yazınca sonuçlar bazen eski kelimeye ait çıkıyor.
- **Kök neden:** "tel" ve "telefon" istekleri sırayla gidiyor ama "tel" daha geç dönüyor ve ekranı eziyor.
- **Nasıl bulunur:** Network sekmesinde Throttling ile "Slow 4G" seç, istekleri ve dönüş sırasını izle.
- **Çözüm:** Önceki isteği `AbortController` ile iptal et ya da sadece son isteğin sonucunu kabul et; debounce ekle.
- **Projede:** Ürün arama kutusu.
- **Seviye:** 2 · **Video:** evet

### FE-06 Liste anahtarı (key) olarak index kullanmak
- **Belirti:** Sepetten bir ürün silinince yanlış satırın adet kutusu boşalıyor ya da içerik kayıyor.
- **Kök neden:** `key={index}` kullanılmış; silmede index'ler kayınca React state'i yanlış satırla eşleştiriyor.
- **Nasıl bulunur:** React DevTools'ta satırların key'ine bak; silme sonrası hangi bileşenin korunduğunu izle.
- **Çözüm:** Kararlı ve benzersiz kimlik kullan (`key={item.id}`).
- **Projede:** Sepet kalemleri listesi.
- **Seviye:** 1 · **Video:** hayır

### FE-07 Kontrollü/kontrolsüz input uyarısı
- **Belirti:** "A component is changing an uncontrolled input to be controlled" uyarısı; form sıfırlanmıyor.
- **Kök neden:** `value` başta `undefined`, veri gelince doluyor.
- **Nasıl bulunur:** Uyarıdaki bileşeni bul, ilk render'da `value`'nun ne olduğunu logla.
- **Çözüm:** Başlangıç değerini boş string ver (`value={name ?? ""}`), ya da form kütüphanesinde `defaultValues`.
- **Projede:** Adres düzenleme formu.
- **Seviye:** 1 · **Video:** hayır

### FE-08 State'i doğrudan değiştirmek (mutation)
- **Belirti:** Butona basılıyor, veri değişiyor ama ekran güncellenmiyor.
- **Kök neden:** `items.push(x); setItems(items)` — aynı dizi referansı; React değişikliği görmez.
- **Nasıl bulunur:** Breakpoint ile `prev === next` olduğunu gör.
- **Çözüm:** Yeni dizi/nesne oluştur: `setItems([...items, x])`.
- **Projede:** Favorilere ekleme.
- **Seviye:** 1 · **Video:** hayır

### FE-09 StrictMode'da efektin iki kez çalışması
- **Belirti:** Geliştirmede "sipariş oluşturuldu" isteği iki kez gidiyor; canlıda bir kez.
- **Kök neden:** React StrictMode geliştirmede efektleri bilerek iki kez çalıştırır. Efekt yan etkili ve tekrarlanamaz (idempotent değil) yazılmış.
- **Nasıl bulunur:** Sadece geliştirmede olduğunu fark et; `main.tsx`'te `StrictMode`'u gör.
- **Çözüm:** Yan etkili işlemleri (sipariş oluşturma) efektten çıkarıp kullanıcı eylemine bağla; efektleri temizlenebilir yaz. StrictMode'u kapatmak çözüm değildir.
- **Projede:** Ödeme sonrası "sipariş tamamlandı" sayfası.
- **Seviye:** 2 · **Video:** evet

### FE-10 Çift tıklama ile çift gönderim
- **Belirti:** Müşteri "Siparişi tamamla"ya iki kez basınca iki sipariş oluşuyor.
- **Kök neden:** Buton istek sürerken pasif değil; backend de aynı isteği iki kez kabul ediyor.
- **Nasıl bulunur:** Network'te iki POST gör; backend logunda iki sipariş kimliği.
- **Çözüm:** Frontend'de gönderim sırasında butonu kilitle; asıl çözüm backend'de idempotency anahtarı (bkz. EŞ-02).
- **Projede:** Ödeme adımı.
- **Seviye:** 2 · **Video:** hayır

### FE-11 Hata sınırı (error boundary) yok, beyaz ekran
- **Belirti:** Canlıda tek bir ürünün verisi bozuk olunca tüm site beyaz ekran.
- **Kök neden:** Bir bileşende render hatası var ve onu yakalayacak Error Boundary yok; React tüm ağacı kaldırır.
- **Nasıl bulunur:** Konsolda hata, kaynak haritası (source map) ile gerçek satıra git.
- **Çözüm:** Sayfa/bölüm bazında Error Boundary; hatayı izleme sistemine gönder; veriyi render etmeden önce doğrula.
- **Projede:** Ürün kartında `price` alanı `null` gelen eski ürün.
- **Seviye:** 2 · **Video:** hayır

### FE-12 SPA sayfasında yenileyince 404
- **Belirti:** `/urun/123` adresinde F5'e basınca sunucu 404 veriyor; ana sayfadan tıklayınca çalışıyor.
- **Kök neden:** Sunucu (Nginx) her yolu `index.html`'e yönlendirmiyor; istemci tarafı yönlendirme sunucuda tanımsız.
- **Nasıl bulunur:** Network'te ilk isteğin 404 olduğunu ve Nginx'ten geldiğini gör.
- **Çözüm:** Nginx'te `try_files $uri /index.html;` (SPA fallback).
- **Projede:** Docker'daki Nginx ayarı.
- **Seviye:** 1 · **Video:** hayır

### FE-13 Ortam değişkeni okunmuyor
- **Belirti:** `import.meta.env.API_URL` `undefined`; istekler `undefined/api/...` adresine gidiyor.
- **Kök neden:** Vite sadece `VITE_` ile başlayan değişkenleri tarayıcıya açar; ayrıca değişkenler build anında gömülür.
- **Nasıl bulunur:** Network'te bozuk URL'yi gör; `.env` dosyasını ve değişken adını kontrol et.
- **Çözüm:** `VITE_API_URL` kullan; canlı ortam için build anında değeri ver ya da çalışma anında yüklenen bir `config.json` kullan.
- **Projede:** Yönetim paneli.
- **Seviye:** 1 · **Video:** hayır

### FE-14 Ondalık sayı ve para biçimi (Türkçe virgül)
- **Belirti:** Satıcı fiyata "149,90" yazıyor, ürün 14990 TL ya da NaN kaydediliyor.
- **Kök neden:** `parseFloat("149,90")` 149 döner; binlik nokta ve ondalık virgül farkı dikkate alınmamış.
- **Nasıl bulunur:** Formun gönderdiği JSON'u Network'te incele.
- **Çözüm:** Kullanıcı girdisini yerel biçime göre ayrıştır (`Intl.NumberFormat` mantığıyla), API'ye her zaman nokta ondalıklı sayı ya da kuruş cinsinden tamsayı gönder.
- **Projede:** Satıcı panelinde ürün fiyatı girişi.
- **Seviye:** 2 · **Video:** hayır

### FE-15 Tarih bir gün kayıyor
- **Belirti:** Doğum tarihi 15 Mart seçiliyor, profilde 14 Mart görünüyor.
- **Kök neden:** `new Date("2026-03-15")` UTC gece yarısı olarak yorumlanıyor, yerel saate çevrilince güncel saat dilimine göre bir gün kayabiliyor; ya da tarih `toISOString()` ile UTC'ye çevrilip gönderiliyor.
- **Nasıl bulunur:** Gönderilen ve dönen değeri karşılaştır; saat dilimini değiştirip (DevTools → Sensors) tekrar dene.
- **Çözüm:** Sadece tarih olan alanları tarih olarak (`YYYY-MM-DD`) taşı, saat bilgisi ekleme; backend'de `DateOnly`.
- **Projede:** Müşteri profili, kampanya bitiş tarihi.
- **Seviye:** 2 · **Video:** evet

### FE-16 Önbellekte kalan eski veri
- **Belirti:** Satıcı stok güncelliyor, liste sayfasına dönünce eski stok görünüyor; F5 ile düzeliyor.
- **Kök neden:** Veri kütüphanesi (TanStack Query) güncellemeden sonra ilgili sorguyu geçersiz kılmıyor.
- **Nasıl bulunur:** Query DevTools'ta sorgunun "stale/fresh" durumuna ve anahtarına bak.
- **Çözüm:** Mutation sonrası `invalidateQueries` ile doğru anahtarı geçersiz kıl; anahtarları tutarlı tasarla.
- **Projede:** Satıcı paneli stok listesi.
- **Seviye:** 2 · **Video:** hayır

### FE-17 Gereksiz yeniden render ve yavaş liste
- **Belirti:** 2.000 siparişlik yönetim tablosunda yazmak gecikmeli.
- **Kök neden:** Üstteki state değişince tüm satırlar yeniden render oluyor; Context değeri her render'da yeni nesne.
- **Nasıl bulunur:** React DevTools Profiler ile kayıt al, "flamegraph"ta pahalı bileşeni bul.
- **Çözüm:** State'i aşağı indir, `React.memo`, Context değerini `useMemo` ile sabitle, uzun listelerde sanallaştırma (virtualization).
- **Projede:** Yönetim panelindeki sipariş tablosu.
- **Seviye:** 3 · **Video:** evet

### FE-18 Tarayıcıda görünen ama aslında backend kaynaklı CORS hatası
- **Belirti:** Konsolda "blocked by CORS policy", ama Postman'de istek çalışıyor.
- **Kök neden:** Tarayıcı güvenlik kuralı; sunucu doğru CORS başlıklarını dönmüyor. Çoğu zaman sunucu aslında 500 veriyor ve hata cevabında CORS başlığı olmadığı için tarayıcı onu CORS hatası gibi gösteriyor.
- **Nasıl bulunur:** Network'te OPTIONS (preflight) isteğini ve asıl isteğin durum kodunu ayrı ayrı incele; backend logunda 500 var mı bak.
- **Çözüm:** Backend tarafı: bkz. NET-01. Frontend'de "mode: no-cors" çözüm değildir.
- **Projede:** Yeni Katalog servisine giden istekler.
- **Seviye:** 2 · **Video:** evet

### FE-19 Kimlik bilgili isteklerde çerez gitmiyor
- **Belirti:** Giriş başarılı ama sonraki isteklerde 401.
- **Kök neden:** `fetch`'te `credentials: "include"` yok; ya da çerez `SameSite`/`Secure` ayarı farklı alan adına gönderilmesini engelliyor.
- **Nasıl bulunur:** Application → Cookies'te çerezin olduğunu, Network'te isteğin Cookie başlığını taşımadığını gör.
- **Çözüm:** `credentials: "include"`, backend'de `AllowCredentials` ve açık köken (origin) listesi; çerez ayarlarını ortamla uyumlu yap.
- **Projede:** Müşteri sitesi → API (farklı alt alan adları).
- **Seviye:** 3 · **Video:** evet

---

## API — HTTP ve API sözleşmesi hataları

### API-01 400 Bad Request: model doğrulama
- **Belirti:** Form gönderiliyor, 400 dönüyor, ekranda sadece "Bir hata oluştu".
- **Kök neden:** Zorunlu alan eksik ya da tip yanlış; ASP.NET Core `[ApiController]` otomatik 400 ve `ProblemDetails` döner, frontend bu gövdeyi okumuyor.
- **Nasıl bulunur:** Network → Response'ta `errors` alanını oku.
- **Çözüm:** Frontend hata gövdesini alanlara eşlesin; doğrulama kuralları iki tarafta tutarlı olsun.
- **Projede:** Satıcı başvuru formu.
- **Seviye:** 1 · **Video:** hayır

### API-02 400: JSON alan adı uyuşmazlığı (camelCase / PascalCase)
- **Belirti:** İstek 200 dönüyor ama kayıtta bazı alanlar boş.
- **Kök neden:** Frontend `discountCode`, API modeli `CouponCode` bekliyor; eşleşmeyen alan sessizce yok sayılıyor.
- **Nasıl bulunur:** Gönderilen JSON ile C# modelini yan yana koy; controller'a breakpoint koyup gelen nesneye bak.
- **Çözüm:** Sözleşmeyi tek kaynaktan üret (OpenAPI → TypeScript tipleri); bilinmeyen alanları reddeden ayar düşün.
- **Projede:** Sepette kupon uygulama.
- **Seviye:** 1 · **Video:** hayır

### API-03 401 ve 403 karışıklığı
- **Belirti:** Satıcı başka satıcının siparişini açınca "Giriş yapın" ekranına atılıyor.
- **Kök neden:** 401 (kimliğin yok/geçersiz) ile 403 (kimliğin var ama yetkin yok) karıştırılmış; frontend her ikisinde de çıkış yaptırıyor.
- **Nasıl bulunur:** Durum kodunu ve `WWW-Authenticate` başlığını incele.
- **Çözüm:** Backend doğru kodu dönsün; frontend 401'de oturumu yenilesin, 403'te "yetkiniz yok" göstersin.
- **Projede:** Satıcı paneli sipariş detayı.
- **Seviye:** 1 · **Video:** hayır

### API-04 404: route eşleşmiyor
- **Belirti:** Yeni endpoint yazıldı, çağrılınca 404.
- **Kök neden:** Route şablonu farklı (`api/orders/{id}` vs `api/order/{id}`), sürüm öneki eksik, ya da route kısıtı (`{id:int}`) GUID ile eşleşmiyor.
- **Nasıl bulunur:** Uygulamadaki tüm endpoint'leri listele (Swagger), istek URL'siyle karşılaştır.
- **Çözüm:** Route'ları tutarlı adlandır; entegrasyon testinde endpoint'in varlığını test et.
- **Projede:** İade talebi endpoint'i.
- **Seviye:** 1 · **Video:** hayır

### API-05 405 Method Not Allowed
- **Belirti:** Silme butonu 405 veriyor.
- **Kök neden:** Frontend `DELETE` gönderiyor, endpoint `[HttpPost("delete")]`; ya da IIS/ters vekil `PUT`/`DELETE`'i engelliyor.
- **Nasıl bulunur:** Yanıttaki `Allow` başlığına bak.
- **Çözüm:** HTTP metodunu sözleşmeyle eşle; ters vekil ayarını kontrol et.
- **Projede:** Favoriden çıkarma.
- **Seviye:** 1 · **Video:** hayır

### API-06 409 Conflict ve iyimser kilitleme
- **Belirti:** İki yönetici aynı ürünü düzenliyor, ikincisinin kaydı birincinin değişikliğini siliyor (ya da 409 alıyor ve ne yapacağını bilmiyor).
- **Kök neden:** Eşzamanlılık kontrolü yok ya da 409 frontend'de ele alınmıyor.
- **Nasıl bulunur:** İki sekmede aynı kaydı aç, sırayla kaydet.
- **Çözüm:** EF Core'da concurrency token (`xmin`/RowVersion), 409'da kullanıcıya güncel veriyi göster.
- **Projede:** Yönetim panelinde ürün düzenleme.
- **Seviye:** 3 · **Video:** evet

### API-07 413 Payload Too Large
- **Belirti:** Satıcı 12 MB'lık ürün görseli yükleyince istek düşüyor.
- **Kök neden:** Kestrel/IIS/Nginx gövde boyutu sınırı (Nginx varsayılanı 1 MB).
- **Nasıl bulunur:** Yanıtın hangi katmandan geldiğine bak (Nginx HTML sayfası mı, API JSON mu).
- **Çözüm:** Sınırları katman katman bilinçli ayarla; büyük dosyalar için doğrudan depolamaya ön imzalı (pre-signed) yükleme.
- **Projede:** Satıcı ürün görseli yükleme.
- **Seviye:** 2 · **Video:** hayır

### API-08 415 Unsupported Media Type
- **Belirti:** Form verisi gönderiliyor, API 415 dönüyor.
- **Kök neden:** `Content-Type` yanlış (JSON beklenirken `text/plain` ya da form-data); `[FromBody]` ile `[FromForm]` karışmış.
- **Nasıl bulunur:** İstek başlıklarında `Content-Type`'ı kontrol et.
- **Çözüm:** Doğru başlık ve doğru model bağlama özniteliği.
- **Projede:** Satıcı belge yükleme.
- **Seviye:** 1 · **Video:** hayır

### API-09 429 Too Many Requests ve geri çekilme
- **Belirti:** Kampanya saatinde müşteriler "çok fazla istek" alıyor; frontend sonsuz tekrar deneyerek durumu kötüleştiriyor.
- **Kök neden:** Hız sınırı var ama istemci `Retry-After`'a uymadan hemen tekrar deniyor.
- **Nasıl bulunur:** Network'te tekrar eden 429'lar ve başlıkları.
- **Çözüm:** Üstel geri çekilme (exponential backoff) + jitter; `Retry-After`'a uy.
- **Projede:** Flaş indirim sayfası.
- **Seviye:** 3 · **Video:** hayır

### API-10 500 ama asıl hata yutulmuş
- **Belirti:** API 500 dönüyor, logda sadece "An error occurred".
- **Kök neden:** `catch (Exception) { return StatusCode(500); }` — istisna loglanmadan yutulmuş.
- **Nasıl bulunur:** Logda iz yok; debugger'da "Break on all exceptions" aç.
- **Çözüm:** Merkezi istisna işleyici (`UseExceptionHandler` / `IExceptionHandler`), `ProblemDetails`, istisnayı yığınıyla logla; gereksiz try-catch'leri kaldır.
- **Projede:** Sipariş özeti endpoint'i.
- **Seviye:** 2 · **Video:** hayır

### API-11 Hata olduğu hâlde 200 dönmek
- **Belirti:** Ödeme başarısız ama frontend "başarılı" gösteriyor.
- **Kök neden:** API her durumda 200 ve `{ success: false }` dönüyor; frontend sadece durum koduna bakıyor.
- **Nasıl bulunur:** Yanıt gövdesini oku.
- **Çözüm:** HTTP durum kodlarını doğru kullan; sözleşmeyi belgelendir.
- **Projede:** Legacy ödeme sonucu endpoint'i.
- **Seviye:** 2 · **Video:** hayır

### API-12 502 / 503 / 504
- **Belirti:** Canlıda ara sıra "Bad Gateway" ya da "Gateway Timeout".
- **Kök neden:** 502: arka uç çöktü ya da bağlantıyı kapattı; 503: servis ayakta değil/aşırı yük; 504: arka uç ters vekilin zaman aşımından uzun sürdü.
- **Nasıl bulunur:** Hangi katmanın döndüğünü bul (Nginx logu vs uygulama logu); iz (trace) ile yavaş adımı bul.
- **Çözüm:** Yavaş işi arka plana al, zaman aşımlarını katmanlar boyunca tutarlı ayarla, sağlık kontrolü ekle.
- **Projede:** Legacy rapor endpoint'i.
- **Seviye:** 4 · **Video:** evet

### API-13 Enum sayı olarak gidiyor
- **Belirti:** Sipariş durumu ekranda "3" yazıyor; yeni durum eklenince eski kayıtlar yanlış etikete kayıyor.
- **Kök neden:** Enum JSON'da tamsayı olarak serileştiriliyor; araya yeni değer eklenince numaralar kaydı.
- **Nasıl bulunur:** Yanıt JSON'unda değeri gör; enum tanımının geçmişine `git log -p` ile bak.
- **Çözüm:** `JsonStringEnumConverter`, enum değerlerine açık numara ver, veritabanında da metin ya da sabit numara.
- **Projede:** Sipariş durumu.
- **Seviye:** 2 · **Video:** hayır

### API-14 Sayfalama kayması
- **Belirti:** Siparişler listesinde sayfa değiştirince bazı kayıtlar iki kez görünüyor, bazıları hiç.
- **Kök neden:** `ORDER BY created_at` benzersiz değil; aynı zamanlı kayıtların sırası sorgudan sorguya değişiyor. Ya da sayfalar arasında yeni kayıt eklenmiş.
- **Nasıl bulunur:** Aynı sorguyu iki kez çalıştırıp sırayı karşılaştır.
- **Çözüm:** Sıralamaya benzersiz anahtar ekle (`created_at, id`); büyük tablolarda keyset sayfalama (bkz. DB-08).
- **Projede:** Müşteri sipariş geçmişi.
- **Seviye:** 3 · **Video:** hayır

### API-15 Sözleşme değişikliği eski istemciyi kırıyor
- **Belirti:** Backend dağıtımından sonra mobil/eski panel çöküyor.
- **Kök neden:** Bir alanın adı değişti ya da tipi değişti; sürümleme yok.
- **Nasıl bulunur:** Dağıtım zamanını hata başlangıcıyla eşleştir; değişen DTO'yu `git diff` ile bul.
- **Çözüm:** Geriye uyumlu değişiklik (önce ekle, sonra kaldır), API sürümleme, sözleşme testleri.
- **Projede:** Ürün DTO'sunda `price` → `priceAmount`.
- **Seviye:** 3 · **Video:** hayır

---

## NET — .NET backend

### NET-01 CORS politikası çalışmıyor
- **Belirti:** Frontend'den istekler CORS hatası veriyor, `AddCors` yazılı olduğu hâlde.
- **Kök neden:** `UseCors` yanlış sırada (ör. `UseRouting`'den önce ya da endpoint eşlemesinden sonra), politika adı uygulanmamış, ya da `AllowAnyOrigin` ile `AllowCredentials` birlikte kullanılmış (izin verilmez).
- **Nasıl bulunur:** Preflight (OPTIONS) yanıtında `Access-Control-Allow-Origin` başlığı var mı bak; `Program.cs`'te middleware sırasını oku.
- **Çözüm:** Doğru sıra, açık origin listesi (ortam ayarından), kimlik bilgisi gerekiyorsa `WithOrigins(...).AllowCredentials()`.
- **Projede:** Yeni Katalog servisinin `Program.cs`'i.
- **Seviye:** 1 · **Video:** evet

### NET-02 Middleware sırası (kimlik doğrulama)
- **Belirti:** Token doğru ama `[Authorize]` her zaman 401.
- **Kök neden:** `UseAuthorization` `UseAuthentication`'dan önce, ya da `UseAuthentication` hiç yok.
- **Nasıl bulunur:** Controller'da `User.Identity.IsAuthenticated` değerine bak; middleware sırasını oku.
- **Çözüm:** `UseRouting → UseCors → UseAuthentication → UseAuthorization → Map...`.
- **Projede:** Bildirim servisi.
- **Seviye:** 1 · **Video:** hayır

### NET-03 NullReferenceException
- **Belirti:** "Object reference not set to an instance of an object" — sadece bazı siparişlerde.
- **Kök neden:** Eski kayıtlarda kargo adresi null; kod her zaman dolu varsaymış.
- **Nasıl bulunur:** Stack trace'te satırı bul; hangi kayıtta olduğunu logdaki sipariş kimliğinden bul; o kaydı veritabanında incele.
- **Çözüm:** Nullable reference types'ı aç ve uyarıları gerçekten çöz; veri sözleşmesini netleştir; eski veriyi düzelt.
- **Projede:** Legacy fatura oluşturma.
- **Seviye:** 1 · **Video:** hayır

### NET-04 Captive dependency (yaşam süresi hatası)
- **Belirti:** Rastgele "A second operation was started on this context" ya da farklı kullanıcıların verisi karışıyor.
- **Kök neden:** Singleton servis içine Scoped bir servis (DbContext) enjekte edilmiş; tüm istekler aynı örneği paylaşıyor.
- **Nasıl bulunur:** DI kayıtlarını oku; geliştirmede `ValidateScopes` hatasına bak.
- **Çözüm:** Yaşam sürelerini düzelt; singleton içinde scoped gerekiyorsa `IServiceScopeFactory` ile kapsam aç. `ValidateScopes` ve `ValidateOnBuild`'i aç.
- **Projede:** Fiyat önbellek servisi.
- **Seviye:** 3 · **Video:** evet

### NET-05 DbContext eşzamanlı kullanım
- **Belirti:** "A second operation was started on this context instance before a previous operation completed."
- **Kök neden:** Aynı DbContext ile `Task.WhenAll` içinde paralel sorgu ya da `await` unutulmuş.
- **Nasıl bulunur:** Stack trace'te hangi metottan olduğunu bul, `await`'siz çağrıları ara.
- **Çözüm:** Sorguları sırayla `await` et ya da her paralel iş için ayrı context (`IDbContextFactory`).
- **Projede:** Ürün detay sayfası için paralel veri yükleme.
- **Seviye:** 2 · **Video:** hayır

### NET-06 .Result / .Wait() ile kilitlenme ve thread tükenmesi
- **Belirti:** Yük altında API yanıt vermez hâle geliyor, CPU düşük, istekler kuyrukta.
- **Kök neden:** Async kod senkron olarak bekleniyor (`.Result`); thread pool tükeniyor (sync-over-async).
- **Nasıl bulunur:** `dotnet-counters` ile thread pool kuyruğunu izle; `dotnet-dump` ile thread'lerin nerede beklediğine bak.
- **Çözüm:** Baştan sona async; `.Result`/`.Wait()` yok.
- **Projede:** Legacy kargo fiyatı hesaplama.
- **Seviye:** 4 · **Video:** evet

### NET-07 async void
- **Belirti:** Arka plandaki hata uygulamayı tamamen çökertiyor ya da hiç loglanmıyor.
- **Kök neden:** `async void` metottaki istisna yakalanamaz.
- **Nasıl bulunur:** Çöküş anındaki stack trace; `async void` ara.
- **Çözüm:** `async Task` kullan; olay işleyicileri hariç `async void` yok.
- **Projede:** Sipariş sonrası e-posta tetikleyici.
- **Seviye:** 2 · **Video:** hayır

### NET-08 HttpClient yanlış kullanımı (soket tükenmesi)
- **Belirti:** Bir süre sonra dış servise istekler "address already in use" / zaman aşımı veriyor.
- **Kök neden:** Her istekte `new HttpClient()`; soketler TIME_WAIT'te birikiyor. Tersi de hata: tek statik HttpClient DNS değişikliğini görmüyor.
- **Nasıl bulunur:** `netstat` ile açık bağlantı sayısı; kodda `new HttpClient` ara.
- **Çözüm:** `IHttpClientFactory` ya da typed client.
- **Projede:** Kargo firması entegrasyonu.
- **Seviye:** 3 · **Video:** evet

### NET-09 Türkçe "i" problemi (kültür)
- **Belirti:** Linux sunucuda/Türkçe kültürde `"title".ToUpper()` "TİTLE" oluyor; e-posta karşılaştırması ya da rol kontrolü tutmuyor.
- **Kök neden:** Kültüre duyarlı string işlemleri; Türkçede "i" büyük harfi "İ".
- **Nasıl bulunur:** Sunucunun kültürünü logla; karşılaştırmayı breakpoint'te incele.
- **Çözüm:** Sabit metin işlemlerinde `ToUpperInvariant`, `StringComparison.OrdinalIgnoreCase`; kullanıcıya gösterilen metinde doğru kültür.
- **Projede:** Rol adı kontrolü (`"admin"` vs `"ADMIN"`).
- **Seviye:** 2 · **Video:** evet

### NET-10 Kültüre bağlı sayı ve tarih ayrıştırma
- **Belirti:** Aynı CSV içe aktarma yerelde çalışıyor, sunucuda fiyatlar 100 kat büyük.
- **Kök neden:** `decimal.Parse("149.90")` tr-TR kültüründe noktayı binlik ayırıcı sayıyor.
- **Nasıl bulunur:** Sunucunun `CultureInfo.CurrentCulture` değerini logla.
- **Çözüm:** Makine verisini `CultureInfo.InvariantCulture` ile ayrıştır.
- **Projede:** Satıcı toplu ürün yükleme (CSV/Excel).
- **Seviye:** 2 · **Video:** hayır

### NET-11 DateTime.Now ve saat dilimi
- **Belirti:** Gece 00:00'da bitmesi gereken kampanya 21:00'de bitiyor (ya da 03:00'te).
- **Kök neden:** Sunucu UTC, kod `DateTime.Now`; veritabanında `DateTimeKind` karışık.
- **Nasıl bulunur:** Kampanya bitiş değerini veritabanında, API yanıtında ve ekranda karşılaştır.
- **Çözüm:** Sunucuda her şey UTC (`DateTimeOffset`/`UtcNow`), saat dilimi dönüşümü sadece gösterimde; test için `TimeProvider`.
- **Projede:** Kampanya modülü.
- **Seviye:** 2 · **Video:** evet

### NET-12 [FromBody] / model bağlama sessizce null
- **Belirti:** Controller'a gelen nesne null ya da tüm alanlar varsayılan.
- **Kök neden:** JSON gövdesi geçersiz, tip uyuşmuyor (string sayıya), ya da birden fazla `[FromBody]` parametre.
- **Nasıl bulunur:** `ModelState`'e breakpoint ile bak; logda JSON hata detayı.
- **Çözüm:** Tek gövde modeli, `[ApiController]` ile otomatik 400, JSON seçeneklerini netleştir.
- **Projede:** Adres ekleme.
- **Seviye:** 1 · **Video:** hayır

### NET-13 JSON döngüsel referans
- **Belirti:** "A possible object cycle was detected" ya da yanıt devasa.
- **Kök neden:** EF entity'si doğrudan dönülüyor; Order → Customer → Orders → ...
- **Nasıl bulunur:** Hata mesajındaki yol (path).
- **Çözüm:** Entity yerine DTO dön; projeksiyon (`Select`) kullan.
- **Projede:** Sipariş detay API'si.
- **Seviye:** 1 · **Video:** hayır

### NET-14 Ayar dosyası ortamda ezilmiyor
- **Belirti:** Canlı, test veritabanına bağlanıyor.
- **Kök neden:** `appsettings.Production.json` yüklenmiyor çünkü `ASPNETCORE_ENVIRONMENT` ayarlı değil; ya da ortam değişkeni adı `ConnectionStrings__Default` yerine yanlış yazılmış.
- **Nasıl bulunur:** Açılışta etkin ortamı ve ayar kaynaklarını logla (sırrı loglamadan).
- **Çözüm:** Ortam değişkenlerini Docker/CI'da açıkça ver; açılışta zorunlu ayarları doğrula (`ValidateOnStart`).
- **Projede:** Ödeme servisinin Docker ayarı.
- **Seviye:** 2 · **Video:** hayır

### NET-15 BackgroundService sessizce ölüyor
- **Belirti:** Bir süre sonra e-postalar gitmiyor, uygulama ayakta görünüyor.
- **Kök neden:** `ExecuteAsync` içindeki yakalanmayan istisna servisi durdurdu (sürüme ve `BackgroundServiceExceptionBehavior` ayarına göre ya tüm host durur ya servis sessizce biter).
- **Nasıl bulunur:** Logda tek seferlik istisna; servisin son çalışma zamanını metrikle izle.
- **Çözüm:** Döngü içinde hata yakala ve logla, sağlık kontrolüne bağla, davranışı bilinçli seç.
- **Projede:** Bildirim servisi kuyruk tüketicisi.
- **Seviye:** 3 · **Video:** hayır

### NET-16 CancellationToken yok sayılıyor
- **Belirti:** Kullanıcı sayfayı kapatsa da ağır rapor sorgusu dakikalarca veritabanını meşgul ediyor.
- **Kök neden:** `HttpContext.RequestAborted` aşağı katmanlara geçirilmiyor.
- **Nasıl bulunur:** Veritabanında çalışan sorgulara (`pg_stat_activity`) bak; istek iptal olduğu hâlde sorgu sürüyor.
- **Çözüm:** CancellationToken'ı tüm async zincire geçir.
- **Projede:** Satıcı satış raporu.
- **Seviye:** 3 · **Video:** hayır

### NET-17 decimal / double karışıklığı
- **Belirti:** Hakediş toplamı kuruş kuruş tutmuyor; 0.1 + 0.2 != 0.3.
- **Kök neden:** Para `double` ile hesaplanmış; yuvarlama her adımda yapılmış.
- **Nasıl bulunur:** Ara değerleri logla; Excel'deki doğru hesapla karşılaştır.
- **Çözüm:** Para her zaman `decimal`; yuvarlama kuralı tek yerde ve açıkça (`MidpointRounding`).
- **Projede:** Satıcı komisyon/hakediş hesaplama.
- **Seviye:** 2 · **Video:** evet

### NET-18 Statik olay aboneliği ile bellek sızıntısı
- **Belirti:** API'nin belleği günler içinde büyüyor, yeniden başlatınca düzeliyor.
- **Kök neden:** Kısa ömürlü nesneler statik bir olaya abone olup hiç çıkmıyor; ya da statik sözlük önbelleği sınırsız büyüyor.
- **Nasıl bulunur:** `dotnet-counters` ile GC heap boyutu; `dotnet-gcdump` ile hangi nesnelerin biriktiğini gör.
- **Çözüm:** Aboneliği kaldır, `IMemoryCache` ile süre ve boyut sınırı.
- **Projede:** Legacy fiyat önbelleği.
- **Seviye:** 4 · **Video:** evet

---

## EF — Entity Framework Core

### EF-01 N+1 sorgu
- **Belirti:** Sipariş listesi 50 kayıtta 51 sorgu atıyor; büyük veride sayfa 10 saniye.
- **Kök neden:** Döngü içinde ilişkili veri yükleniyor (lazy loading ya da döngüde sorgu).
- **Nasıl bulunur:** EF Core loglarında SQL'leri say; MiniProfiler/izlerde tekrar eden sorgu.
- **Çözüm:** `Include` ya da `Select` ile projeksiyon; lazy loading kapalı.
- **Projede:** Yönetim paneli sipariş listesi.
- **Seviye:** 2 · **Video:** evet

### EF-02 Kartezyen patlama (çoklu Include)
- **Belirti:** Ürün detayı sorgusu milyonlarca satır dönüyor, bellek dolup taşıyor.
- **Kök neden:** Bir ürünün varyantları, görselleri ve yorumları aynı sorguda `Include` edildi; satırlar çarpılıyor.
- **Nasıl bulunur:** Üretilen SQL'i ve dönen satır sayısını incele.
- **Çözüm:** `AsSplitQuery()` ya da ayrı projeksiyonlar.
- **Projede:** Ürün detay API'si.
- **Seviye:** 3 · **Video:** evet

### EF-03 Tracking ile gereksiz bellek ve yanlış güncelleme
- **Belirti:** Salt okunur rapor yavaş; ya da bir yerde değiştirilen nesne beklenmedik şekilde kaydediliyor.
- **Kök neden:** Okuma sorgularında change tracking açık; uzun yaşayan context'te varlıklar takip ediliyor.
- **Nasıl bulunur:** `ChangeTracker.Entries()` sayısına bak.
- **Çözüm:** Okumada `AsNoTracking()`, context ömrünü kısa tut.
- **Projede:** Satıcı rapor ekranı.
- **Seviye:** 2 · **Video:** hayır

### EF-04 Eksik Include, boş ilişki
- **Belirti:** Sipariş kalemleri bazen boş geliyor.
- **Kök neden:** İlişki yüklenmemiş; bazen dolu çünkü aynı context daha önce o veriyi takip etmiş.
- **Nasıl bulunur:** Üretilen SQL'de JOIN olmadığını gör.
- **Çözüm:** Gereken veriyi açıkça yükle; davranışın context durumuna bağlı olmasından kaçın.
- **Projede:** Sipariş onay e-postası.
- **Seviye:** 2 · **Video:** hayır

### EF-05 Bellekte filtreleme (AsEnumerable / ToList erken)
- **Belirti:** Arama sorgusu 2 milyon ürünü belleğe çekip sonra filtreliyor.
- **Kök neden:** `ToList()` ya da `AsEnumerable()` `Where`'den önce; ya da SQL'e çevrilemeyen metot kullanılmış.
- **Nasıl bulunur:** Üretilen SQL'de `WHERE` yok; bellek kullanımı fırlıyor.
- **Çözüm:** Filtreyi `IQueryable` üzerinde tut; çevrilemeyen ifadeyi değiştir.
- **Projede:** Katalog arama.
- **Seviye:** 2 · **Video:** hayır

### EF-06 Migration'lar ve veritabanı uyumsuz
- **Belirti:** Yerelde çalışıyor, test ortamında "column does not exist".
- **Kök neden:** Migration oluşturulmuş ama uygulanmamış; iki dalda aynı anda migration eklenip birleştirilince model snapshot karışmış.
- **Nasıl bulunur:** `__EFMigrationsHistory` tablosunu migration listesiyle karşılaştır.
- **Çözüm:** Dağıtımda migration adımı; birleştirmede snapshot çakışmasını doğru çöz; migration'ları gözden geçir.
- **Projede:** İade modülüne yeni kolon.
- **Seviye:** 2 · **Video:** evet

### EF-07 decimal hassasiyet kaybı
- **Belirti:** 149,999 TL kaydedilip 150,00 TL okunuyor; komisyonda kuruş farkı.
- **Kök neden:** Kolon tipi `numeric(18,2)`, ara hesap 4 hane gerektiriyor; ya da hassasiyet hiç tanımlanmamış.
- **Nasıl bulunur:** Veritabanındaki kolon tipine ve migration'a bak.
- **Çözüm:** `HasPrecision` ile bilinçli hassasiyet; yuvarlamayı uygulamada açıkça yap.
- **Projede:** Komisyon tablosu.
- **Seviye:** 2 · **Video:** hayır

### EF-08 DbUpdateConcurrencyException ele alınmıyor
- **Belirti:** Yoğun saatte stok güncellemesi 500 veriyor.
- **Kök neden:** Concurrency token var ama çakışmada yeniden deneme ya da kullanıcıya bilgi yok.
- **Nasıl bulunur:** Logda istisna tipine bak.
- **Çözüm:** Çakışmada güncel değeri yükleyip yeniden dene ya da 409 dön; stokta atomik SQL güncellemesi (bkz. EŞ-01).
- **Projede:** Stok düşümü.
- **Seviye:** 3 · **Video:** hayır

---

## DB — PostgreSQL ve veri

### DB-01 Eksik index
- **Belirti:** Küçük veride hızlı olan "müşterinin siparişleri" sorgusu büyük veride 8 saniye.
- **Kök neden:** `orders.customer_id` üzerinde index yok; tam tablo taraması.
- **Nasıl bulunur:** `EXPLAIN (ANALYZE, BUFFERS)` ile "Seq Scan"ı gör; `pg_stat_statements` ile en pahalı sorguları bul.
- **Çözüm:** Doğru (gerekirse bileşik) index; büyük tabloda `CREATE INDEX CONCURRENTLY`.
- **Projede:** Müşteri sipariş geçmişi (büyük veri profili).
- **Seviye:** 3 · **Video:** evet

### DB-02 Index var ama kullanılmıyor
- **Belirti:** Index eklendi, sorgu hâlâ yavaş.
- **Kök neden:** Sorgu kolona fonksiyon uyguluyor (`lower(email)`), tip uyuşmazlığı var, bileşik index'in sırası yanlış ya da `LIKE '%kelime%'`.
- **Nasıl bulunur:** `EXPLAIN`'de index kullanılmadığını gör.
- **Çözüm:** İfade index'i, doğru sıralı bileşik index, tam metin arama ya da trigram.
- **Projede:** E-postayla müşteri arama, ürün adı arama.
- **Seviye:** 4 · **Video:** evet

### DB-03 Kilitlenme (deadlock)
- **Belirti:** Ara sıra "deadlock detected" ve sipariş başarısız.
- **Kök neden:** İki işlem aynı satırları farklı sırayla kilitliyor (biri önce stok sonra sipariş, diğeri tersi).
- **Nasıl bulunur:** PostgreSQL logundaki deadlock detayı (hangi sorgular, hangi kilitler).
- **Çözüm:** Kilit sırasını tutarlı yap, transaction'ları kısa tut, gerekirse yeniden dene.
- **Projede:** Sipariş oluşturma ve iade işlemi.
- **Seviye:** 4 · **Video:** evet

### DB-04 Kayıp güncelleme (lost update)
- **Belirti:** Satıcı bakiyesi bazen yanlış; iki hakediş aynı anda yazılınca biri kayboluyor.
- **Kök neden:** Oku-hesapla-yaz deseni; iki işlem aynı eski değeri okuyor.
- **Nasıl bulunur:** İki eşzamanlı istekle tekrar et (yük testi aracıyla).
- **Çözüm:** Atomik güncelleme (`SET balance = balance + @x`), `SELECT ... FOR UPDATE` ya da iyimser kilit.
- **Projede:** Satıcı bakiye hesabı.
- **Seviye:** 4 · **Video:** evet

### DB-05 Bağlantı havuzu tükendi
- **Belirti:** Yük altında "The connection pool has been exhausted" / zaman aşımı.
- **Kök neden:** Bağlantılar geri bırakılmıyor (dispose edilmeyen bağlantı), uzun transaction'lar ya da havuz boyutu sunucu kapasitesiyle uyumsuz; mikroservislerin toplam bağlantısı `max_connections`'ı aşıyor.
- **Nasıl bulunur:** `pg_stat_activity`'de "idle in transaction" bağlantılar; Npgsql metrikleri.
- **Çözüm:** `using`/`await using`, transaction'ları kısalt, havuz boyutlarını hesapla, gerekirse PgBouncer.
- **Projede:** Legacy rapor modülü (Dapper).
- **Seviye:** 4 · **Video:** evet

### DB-06 NULL karşılaştırma
- **Belirti:** "Kuponsuz siparişler" raporu boş geliyor.
- **Kök neden:** `WHERE coupon_id = NULL` her zaman boş; `NOT IN` alt sorgusunda tek bir NULL tüm sonucu boşaltır.
- **Nasıl bulunur:** Sorguyu parça parça çalıştır.
- **Çözüm:** `IS NULL`, `NOT EXISTS`.
- **Projede:** Legacy kampanya raporu.
- **Seviye:** 2 · **Video:** hayır

### DB-07 JOIN ile çoğalan toplam
- **Belirti:** Satıcı ciro raporu gerçek cironun iki katı.
- **Kök neden:** Siparişler hem kalemlerle hem ödemelerle JOIN edilip toplanmış; satırlar çarpılmış.
- **Nasıl bulunur:** Tek siparişi sorguda izle; satır sayısını kontrol et.
- **Çözüm:** Önce alt sorguda topla sonra birleştir; gerçek değerle test.
- **Projede:** Legacy satıcı raporu.
- **Seviye:** 3 · **Video:** evet

### DB-08 OFFSET ile derin sayfalama
- **Belirti:** Yönetim panelinde 5.000. sayfa 20 saniye.
- **Kök neden:** `OFFSET 100000` öncekileri okuyup atıyor.
- **Nasıl bulunur:** `EXPLAIN ANALYZE` ile okunan satır sayısı.
- **Çözüm:** Keyset (seek) sayfalama: `WHERE (created_at, id) < (@lastCreated, @lastId)`.
- **Projede:** Yönetim paneli tüm siparişler (büyük veri).
- **Seviye:** 3 · **Video:** hayır

### DB-09 timestamp ve timestamptz
- **Belirti:** Yaz/kış saati ya da sunucu taşınmasından sonra tarihler 3 saat kaymış.
- **Kök neden:** Kolon `timestamp without time zone`; uygulama farklı varsayımlarla yazıyor.
- **Nasıl bulunur:** Kolon tipine ve yazılan değerlere bak; Npgsql'in `DateTimeKind` kurallarını incele.
- **Çözüm:** Anlık zaman için `timestamptz`, UTC yaz; sadece tarih için `date`.
- **Projede:** Eski sipariş tabloları.
- **Seviye:** 3 · **Video:** hayır

### DB-10 Türkçe sıralama (collation)
- **Belirti:** Marka listesinde "Çelik" Z'den sonra, "İnci" en sonda.
- **Kök neden:** Veritabanı/kolon collation'ı Türkçe değil.
- **Nasıl bulunur:** `ORDER BY` sonucunu ve collation'ı kontrol et.
- **Çözüm:** ICU Türkçe collation (`tr-TR-x-icu`) ilgili kolonlarda ya da sorguda.
- **Projede:** Marka ve kategori listeleri.
- **Seviye:** 2 · **Video:** hayır

### DB-11 Uzun süren migration tabloyu kilitliyor
- **Belirti:** Dağıtım sırasında site 4 dakika yanıt vermedi.
- **Kök neden:** Büyük tabloya varsayılan değerli kolon ekleme / index oluşturma / tip değişikliği tabloyu kilitledi.
- **Nasıl bulunur:** Dağıtım zamanı ile `pg_locks` ve yavaşlama eşleşiyor.
- **Çözüm:** Genişlet-daralt (expand/contract) deseni, `CONCURRENTLY`, parçalı veri taşıma, `lock_timeout`.
- **Projede:** `orders` tablosuna kolon ekleme.
- **Seviye:** 4 · **Video:** evet

### DB-12 Benzersizlik ihlali yarışta
- **Belirti:** Aynı e-postayla iki hesap açılmış; ya da kayıt sırasında rastgele "duplicate key" 500.
- **Kök neden:** "Var mı?" kontrolü ile ekleme arasında başka istek araya girdi; unique index yok ya da ihlal ele alınmıyor.
- **Nasıl bulunur:** İki eşzamanlı kayıt isteği.
- **Çözüm:** Unique index (gerekirse `lower(email)` üzerinde), ihlali yakalayıp anlamlı hataya çevir.
- **Projede:** Müşteri kaydı.
- **Seviye:** 3 · **Video:** hayır

### DB-13 Kirli veri
- **Belirti:** Rapor çöküyor: bazı telefonlar "+90 5xx", bazıları "05xx", bazıları boş; silinmiş ürüne bağlı sipariş kalemi.
- **Kök neden:** Yıllar içinde farklı kurallarla girilmiş veri; yabancı anahtar sonradan eklenmiş.
- **Nasıl bulunur:** Veriyi profille (`GROUP BY` ile biçimleri say), sorunlu kayıtları ayıkla.
- **Çözüm:** Veri temizleme migration'ı, giriş anında normalizasyon, kısıtlar.
- **Projede:** Eski müşteri ve sipariş verisi (veri üreticideki kirli kayıtlar).
- **Seviye:** 3 · **Video:** hayır

---

## EŞ — Eşzamanlılık ve dağıtık sistemler

### EŞ-01 Son stoğu iki kişi alıyor (overselling)
- **Belirti:** Stokta 1 adet varken 2 sipariş onaylandı; stok -1.
- **Kök neden:** "Stok > 0 mı?" kontrolü ve düşüm ayrı adımlarda; iki istek aynı anda kontrolü geçti.
- **Nasıl bulunur:** Yük testiyle (k6/bombardier) aynı ürüne eşzamanlı istek; loglarda zaman damgaları.
- **Çözüm:** Atomik koşullu güncelleme (`UPDATE ... SET qty = qty - 1 WHERE qty >= 1`) ve etkilenen satır sayısını kontrol; stok rezervasyonu.
- **Projede:** Flaş indirim ürünü.
- **Seviye:** 3 · **Video:** evet

### EŞ-02 Ödeme bildirimi iki kez geliyor (idempotency)
- **Belirti:** Bir sipariş iki kez onaylanmış, satıcıya iki kez hakediş yazılmış.
- **Kök neden:** Ödeme sağlayıcı webhook'u yeniden gönderiyor (at-least-once); işleme tekrar edilebilir değil.
- **Nasıl bulunur:** Webhook kayıtlarında aynı olay kimliği iki kez.
- **Çözüm:** Olay kimliği üzerinde unique kayıt, işlenmiş olayları atla; idempotency anahtarı.
- **Projede:** Ödeme servisi webhook endpoint'i.
- **Seviye:** 3 · **Video:** evet

### EŞ-03 Mesaj kayboldu (outbox yok)
- **Belirti:** Sipariş oluştu ama müşteriye e-posta gitmedi, kargo kaydı açılmadı; nadiren.
- **Kök neden:** Veritabanına kayıt ve kuyruğa mesaj atma ayrı işlemler; kayıttan sonra uygulama çökünce mesaj hiç gitmedi.
- **Nasıl bulunur:** Siparişin izinde (trace) mesaj yayınlama adımı yok; logda çökme zamanı.
- **Çözüm:** Transactional outbox deseni.
- **Projede:** Sipariş → Bildirim servisi.
- **Seviye:** 4 · **Video:** evet

### EŞ-04 Aynı mesaj iki kez işleniyor
- **Belirti:** Müşteriye aynı SMS iki kez gitti.
- **Kök neden:** Tüketici mesajı işledi ama onaylamadan (ack) çöktü; kuyruk yeniden teslim etti.
- **Nasıl bulunur:** Bildirim logunda aynı mesaj kimliği.
- **Çözüm:** İdempotent tüketici (işlenen mesaj kimliklerini sakla).
- **Projede:** Bildirim servisi.
- **Seviye:** 3 · **Video:** hayır

### EŞ-05 Olaylar yanlış sırada
- **Belirti:** Sipariş "kargoda" iken durum "hazırlanıyor"a geri döndü.
- **Kök neden:** Olaylar farklı tüketicilerde paralel işlendi; eski olay yeniden sonra geldi.
- **Nasıl bulunur:** Olay zaman damgalarını ve işlenme sırasını karşılaştır.
- **Çözüm:** Sürüm numarası ile eski olayı reddet, durum makinesinde geçerli geçişleri zorla.
- **Projede:** Kargo durum güncellemeleri.
- **Seviye:** 4 · **Video:** evet

### EŞ-06 Yeniden deneme fırtınası (retry storm)
- **Belirti:** Kargo servisi 1 dakika yavaşladı, ardından tüm sistem 20 dakika çöktü.
- **Kök neden:** Her katman ayrı ayrı 3 kez yeniden deniyor; yük katlanarak arttı.
- **Nasıl bulunur:** İzlerde aynı isteğin onlarca kopyası; metriklerde istek sayısı patlaması.
- **Çözüm:** Tek katmanda, sınırlı ve üstel geri çekilmeli yeniden deneme; devre kesici (circuit breaker, Polly/`Microsoft.Extensions.Http.Resilience`).
- **Projede:** Kargo entegrasyonu.
- **Seviye:** 5 · **Video:** evet

### EŞ-07 Önbellek geçersiz kılınmıyor
- **Belirti:** Satıcı fiyatı düşürdü; site 1 saat boyunca eski fiyatı gösterdi, sepette yeni fiyat çıktı.
- **Kök neden:** Fiyat güncellemesi Redis'teki ürün önbelleğini silmiyor; farklı ekranlar farklı kaynaktan okuyor.
- **Nasıl bulunur:** Redis'teki anahtarı ve TTL'yi incele; veritabanındaki değerle karşılaştır.
- **Çözüm:** Yazma sonrası geçersiz kılma, tutarlı anahtar adları, ödeme anında fiyatı her zaman kaynaktan doğrula.
- **Projede:** Ürün fiyatı.
- **Seviye:** 3 · **Video:** hayır

### EŞ-08 Önbellek izdihamı (cache stampede)
- **Belirti:** Ana sayfa önbelleği her saat başı sona erince veritabanı CPU'su yüzde 100.
- **Kök neden:** Önbellek süresi dolunca yüzlerce istek aynı anda pahalı sorguyu çalıştırıyor.
- **Nasıl bulunur:** Metriklerde saat başı tekrar eden ani yük.
- **Çözüm:** Tek istekle yenileme (lock/`HybridCache`), süreye rastgelelik ekle, arka planda yenile.
- **Projede:** Ana sayfa "çok satanlar".
- **Seviye:** 4 · **Video:** hayır

---

## GÜV — Kimlik doğrulama ve güvenlik

### GÜV-01 Token süresi ve saat kayması
- **Belirti:** Bazı kullanıcılar girişten hemen sonra 401 alıyor.
- **Kök neden:** Sunucular arasında saat farkı; token "henüz geçerli değil" ya da süresi dolmuş görünüyor. Ya da `ClockSkew` sıfırlanmış.
- **Nasıl bulunur:** Token'ı çöz (`exp`, `nbf`), sunucu saatleriyle karşılaştır; doğrulama logunu aç.
- **Çözüm:** NTP ile saat senkronu, makul `ClockSkew`.
- **Projede:** Ödeme servisi token doğrulaması.
- **Seviye:** 3 · **Video:** hayır

### GÜV-02 Yenileme token'ı yarışı
- **Belirti:** İki sekme açıkken kullanıcı rastgele çıkış yapıyor.
- **Kök neden:** Aynı anda iki istek refresh token'ı yeniliyor; tek kullanımlık token ikincide reddediliyor.
- **Nasıl bulunur:** Network'te eşzamanlı iki refresh isteği.
- **Çözüm:** Frontend'de tek uçuşta yenileme (tek promise), backend'de kısa tolerans penceresi.
- **Projede:** Müşteri sitesi oturum yönetimi.
- **Seviye:** 4 · **Video:** evet

### GÜV-03 Rol/claim eşleşmiyor
- **Belirti:** Yönetici kullanıcı `[Authorize(Roles = "Admin")]` endpoint'te 403 alıyor.
- **Kök neden:** Token'daki claim adı `role`, .NET `ClaimTypes.Role` (uzun URI) bekliyor; claim eşleme ayarı.
- **Nasıl bulunur:** `User.Claims`'i logla/breakpoint ile incele.
- **Çözüm:** `RoleClaimType` ayarı ya da claim eşlemeyi düzelt.
- **Projede:** Yönetim paneli.
- **Seviye:** 2 · **Video:** hayır

### GÜV-04 IDOR (başkasının kaydına erişim)
- **Belirti:** Satıcı URL'deki sipariş numarasını değiştirince başka satıcının siparişini görüyor.
- **Kök neden:** Endpoint sadece giriş yapılmış mı diye bakıyor, kaydın sahibini kontrol etmiyor.
- **Nasıl bulunur:** İki farklı satıcı hesabıyla aynı kaydı iste.
- **Çözüm:** Her sorguda sahiplik filtresi (global query filter / politika tabanlı yetkilendirme); test.
- **Projede:** Satıcı sipariş detayı, fatura indirme.
- **Seviye:** 3 · **Video:** evet

### GÜV-05 XSS: ürün açıklamasında script
- **Belirti:** Bir ürün sayfasını açan herkesin oturumu çalınıyor.
- **Kök neden:** Satıcının HTML açıklaması `dangerouslySetInnerHTML` ile temizlenmeden basılıyor.
- **Nasıl bulunur:** Açıklama alanına `<img src=x onerror=alert(1)>` dene.
- **Çözüm:** HTML'i sunucuda izinli etiket listesiyle temizle (sanitize), CSP başlığı.
- **Projede:** Ürün açıklaması.
- **Seviye:** 3 · **Video:** hayır

### GÜV-06 Sır frontend paketinde
- **Belirti:** Ödeme sağlayıcının gizli anahtarı tarayıcıdaki JS dosyasında görünüyor.
- **Kök neden:** Gizli değer `VITE_` değişkeni olarak eklenmiş; build'e gömüldü.
- **Nasıl bulunur:** Build çıktısında anahtarı ara.
- **Çözüm:** Sırlar sadece sunucuda; anahtarı hemen döndür (rotate); CI'da sır taraması.
- **Projede:** Yönetim paneli build'i.
- **Seviye:** 2 · **Video:** hayır

---

## ORT — Ortam, Docker ve ayarlar

### ORT-01 Container içinde localhost
- **Belirti:** API Docker'da çalışınca veritabanına bağlanamıyor; yerelde bağlanıyor.
- **Kök neden:** Bağlantı dizesi `localhost`; container içinde localhost container'ın kendisidir.
- **Nasıl bulunur:** Container loguna bak; `docker exec` ile içeriden bağlantıyı dene.
- **Çözüm:** Servis adını kullan (`Host=postgres`); ayarı ortam değişkeninden ver.
- **Projede:** `docker-compose.yml`.
- **Seviye:** 1 · **Video:** hayır

### ORT-02 Servis hazır olmadan bağlanma
- **Belirti:** `docker compose up`'ta API ilk açılışta çöküyor, ikinci denemede çalışıyor.
- **Kök neden:** `depends_on` sadece başlatma sırasını belirler, veritabanının hazır olmasını beklemez.
- **Nasıl bulunur:** Logda zaman sırası.
- **Çözüm:** Sağlık kontrolü + `condition: service_healthy`, açılışta yeniden deneme.
- **Projede:** `docker-compose.yml`.
- **Seviye:** 1 · **Video:** hayır

### ORT-03 Dosya yolu büyük/küçük harf (Linux)
- **Belirti:** Windows'ta çalışan görsel/şablon Linux container'da bulunamıyor.
- **Kök neden:** Linux dosya sistemi büyük/küçük harfe duyarlı; kodda `Templates/Email.html`, diskte `templates/email.html`.
- **Nasıl bulunur:** Hata mesajındaki yolu container içinde `ls` ile kontrol et.
- **Çözüm:** Tutarlı adlandırma; CI'ı Linux'ta çalıştır.
- **Projede:** E-posta şablonları.
- **Seviye:** 2 · **Video:** hayır

### ORT-04 Satır sonu (CRLF) betiği bozuyor
- **Belirti:** Container başlarken `/bin/sh^M: bad interpreter` ya da "no such file".
- **Kök neden:** Windows'ta düzenlenen betik CRLF satır sonlu.
- **Nasıl bulunur:** `cat -A` ile `^M` karakterlerini gör.
- **Çözüm:** `.gitattributes` ile `*.sh text eol=lf`.
- **Projede:** Veritabanı başlatma betiği.
- **Seviye:** 2 · **Video:** hayır

### ORT-05 Ters vekil arkasında yönlendirme döngüsü
- **Belirti:** Canlıda giriş sayfası sonsuz yönlendirme; HTTPS linkleri HTTP üretiyor.
- **Kök neden:** Nginx HTTPS'i sonlandırıyor, uygulama isteği HTTP sanıyor; `X-Forwarded-Proto` işlenmiyor.
- **Nasıl bulunur:** Yanıt başlıklarındaki `Location`'ı izle.
- **Çözüm:** `UseForwardedHeaders` ve güvenilen vekil ayarı.
- **Projede:** Nginx + API.
- **Seviye:** 3 · **Video:** evet

### ORT-06 Container saat dilimi ve kültür
- **Belirti:** Raporlar Docker'da bir gün kaymış, sayılar farklı biçimde.
- **Kök neden:** Container UTC ve invariant kültürde, yerel makine İstanbul ve tr-TR.
- **Nasıl bulunur:** Container'da `date` ve uygulamanın kültürünü logla.
- **Çözüm:** Saat dilimine kodda açıkça bağlı ol (bkz. NET-11), kültürü bilinçli ayarla.
- **Projede:** Günlük satış raporu.
- **Seviye:** 2 · **Video:** hayır

### ORT-07 "Benim makinemde çalışıyor": sürüm farkı
- **Belirti:** CI'da build kırılıyor, geliştiricide çalışıyor.
- **Kök neden:** Farklı .NET SDK ya da Node sürümü; `global.json`/`.nvmrc` yok.
- **Nasıl bulunur:** `dotnet --info` ve `node -v` çıktılarını karşılaştır.
- **Çözüm:** Sürümleri sabitle (`global.json`, `.nvmrc`, Docker imaj etiketi).
- **Projede:** CI ayarı.
- **Seviye:** 1 · **Video:** hayır

---

## MS — Monolitten mikroservise geçiş

### MS-01 İki veri kaynağı, iki gerçek
- **Belirti:** Ürün adı yeni Katalog servisinde güncel, eski monolitin sipariş e-postasında eski.
- **Kök neden:** Veri iki yerde tutuluyor; senkronizasyon tek yönlü ve gecikmeli, bazı güncellemeler eski yoldan yazılıyor.
- **Nasıl bulunur:** Aynı ürünü iki veritabanında karşılaştır; hangi kodun nereye yazdığını ara.
- **Çözüm:** Her veri için tek sahip (source of truth), değişiklikleri olaylarla yay, eski yazma yollarını kapat.
- **Projede:** Katalog servisi ↔ monolit ürün tabloları.
- **Seviye:** 4 · **Video:** evet

### MS-02 Zincirleme zaman aşımı
- **Belirti:** Ödeme servisi yavaşlayınca sepet sayfası da açılmıyor.
- **Kök neden:** Senkron çağrı zinciri; üst servislerin zaman aşımı alttakinden kısa/uzun ve tutarsız; varsayılan HttpClient zaman aşımı 100 saniye.
- **Nasıl bulunur:** Dağıtık izde (Tempo) hangi çağrının beklediğini gör.
- **Çözüm:** Zaman aşımı bütçesi, devre kesici, kritik olmayan çağrıları asenkron yap ya da önbellekle.
- **Projede:** Sepet → Ödeme servisi (taksit seçenekleri).
- **Seviye:** 5 · **Video:** evet

### MS-03 Korelasyon kimliği yok
- **Belirti:** Müşteri şikâyetindeki hatanın üç servisin logunda hangi satırlar olduğu bulunamıyor.
- **Kök neden:** Servisler arası çağrılarda iz/korelasyon kimliği taşınmıyor.
- **Nasıl bulunur:** Loglarda ortak alan yok; izler kopuk.
- **Çözüm:** OpenTelemetry ile `traceparent` yayılımı, logları trace id ile zenginleştir.
- **Projede:** Monolit → Bildirim servisi çağrıları.
- **Seviye:** 4 · **Video:** evet

### MS-04 Dağıtık işlem yarım kaldı
- **Belirti:** Ödeme alındı ama sipariş "ödeme bekliyor"da kaldı.
- **Kök neden:** Ödeme servisi başarılı, monolite dönüş çağrısı zaman aşımına uğradı; telafi (compensation) yok.
- **Nasıl bulunur:** İki servisin kayıtlarını sipariş kimliğiyle karşılaştır.
- **Çözüm:** Saga/süreç yöneticisi, durum uzlaştırma (reconciliation) işi, idempotent geri çağrı.
- **Projede:** Sipariş ↔ Ödeme servisi.
- **Seviye:** 5 · **Video:** evet

### MS-05 Paylaşılan DTO kütüphanesi sürüm uyumsuzluğu
- **Belirti:** Bildirim servisi yeni alanı hep null görüyor.
- **Kök neden:** Paylaşılan sözleşme paketinin eski sürümü kullanılıyor.
- **Nasıl bulunur:** Servislerin paket sürümlerini karşılaştır.
- **Çözüm:** Sözleşmeleri sürümle, geriye uyumlu değişiklik, sözleşme testleri.
- **Projede:** `Contracts` NuGet paketi.
- **Seviye:** 3 · **Video:** hayır

---

## PERF — Performans ve bellek

### PERF-01 Büyük yanıt ve sıkıştırma yok
- **Belirti:** Kategori sayfası 8 MB JSON indiriyor, mobilde dakikalar sürüyor.
- **Kök neden:** Gereksiz alanlar dönülüyor, sayfalama yok, sıkıştırma kapalı.
- **Nasıl bulunur:** Network'te boyut ve süre; yanıtı incele.
- **Çözüm:** Sadece gereken alanlar, sayfalama, `ResponseCompression`/Nginx gzip-brotli.
- **Projede:** Kategori ürün listesi.
- **Seviye:** 2 · **Video:** hayır

### PERF-02 Senkron dosya/rapor üretimi
- **Belirti:** "Excel'e aktar" butonu isteği 2 dakika bekletiyor, sonra 504.
- **Kök neden:** Büyük rapor istek içinde üretiliyor.
- **Nasıl bulunur:** İzde uzun süren rapor adımı.
- **Çözüm:** Arka plan işi + hazır olunca bildirim/indirme linki; akışla (streaming) yazma.
- **Projede:** Satıcı sipariş dışa aktarma.
- **Seviye:** 3 · **Video:** hayır

### PERF-03 Görseller optimize değil
- **Belirti:** Ana sayfa LCP 6 saniye.
- **Kök neden:** 4000 piksellik orijinal görseller küçük kartta gösteriliyor; lazy loading yok.
- **Nasıl bulunur:** Lighthouse ve Network'te görsel boyutları.
- **Çözüm:** Yüklemede yeniden boyutlandırma, modern format (WebP/AVIF), `srcset`, lazy loading.
- **Projede:** Ürün kartları.
- **Seviye:** 2 · **Video:** hayır

### PERF-04 Büyük nesne yığını ve aşırı GC
- **Belirti:** Rapor endpoint'i çalışırken API'nin gecikmesi her şey için artıyor.
- **Kök neden:** Tüm veri belleğe alınıp büyük listeler/string'ler oluşturuluyor; Gen2 GC sıklaşıyor.
- **Nasıl bulunur:** `dotnet-counters` (GC süresi, LOH boyutu), `dotnet-trace`.
- **Çözüm:** Akışla işleme (`IAsyncEnumerable`), sayfalı okuma, gereksiz kopyalardan kaçın.
- **Projede:** Legacy aylık hakediş raporu.
- **Seviye:** 4 · **Video:** evet

---

## ARAÇ — Git, bağımlılık ve testler

### ARAÇ-01 "Dün çalışıyordu": hatayı getiren commit
- **Belirti:** Kupon indirimi yanlış hesaplanıyor; son bir haftada 60 commit var.
- **Kök neden:** Bir refactor yuvarlama sırasını değiştirdi.
- **Nasıl bulunur:** `git bisect` ile çalışan ve bozuk commit arasında ikili arama; testle otomatikleştir (`git bisect run`).
- **Çözüm:** Hatalı değişikliği düzelt, hatayı yakalayan testi ekle.
- **Projede:** Kampanya modülünün git geçmişi.
- **Seviye:** 2 · **Video:** evet

### ARAÇ-02 Çakışma çözümü kodu bozdu
- **Belirti:** Birleştirmeden sonra bir özellik sessizce çalışmıyor.
- **Kök neden:** Merge conflict'te iki taraftan biri yanlışlıkla silindi; derleme hata vermedi.
- **Nasıl bulunur:** `git log --merges`, birleştirme commit'inin diff'i, `git blame`.
- **Çözüm:** Değişikliği geri getir; birleştirme sonrası testleri çalıştırma alışkanlığı.
- **Projede:** Sepet servisi.
- **Seviye:** 2 · **Video:** hayır

### ARAÇ-03 Bağımlılık güncellemesi davranışı değiştirdi
- **Belirti:** Paket güncellemesinden sonra tarih biçimi ya da JSON davranışı değişti.
- **Kök neden:** Küçük görünen sürüm atlaması kırıcı değişiklik içeriyor; kilit dosyası güncellenmemiş/çakışmış.
- **Nasıl bulunur:** `package-lock.json`/`packages.lock.json` diff'i, paketin sürüm notları.
- **Çözüm:** Sürümleri sabitle, güncellemeleri tek tek ve testle yap.
- **Projede:** Frontend tarih kütüphanesi.
- **Seviye:** 2 · **Video:** hayır

### ARAÇ-04 Ara sıra kırılan test (flaky test)
- **Belirti:** CI'da bir test bazen geçiyor bazen kırılıyor.
- **Kök neden:** Test sırasına, saate, rastgele veriye ya da paylaşılan veritabanı durumuna bağlı.
- **Nasıl bulunur:** Testi döngüde tekrar çalıştır, tohumu (seed) ve sırayı sabitle.
- **Çözüm:** Testleri yalıt (her test kendi verisi), `TimeProvider` ile sabit zaman, Testcontainers.
- **Projede:** Sipariş entegrasyon testleri.
- **Seviye:** 3 · **Video:** hayır

### ARAÇ-05 Yanlış şeyi test eden test
- **Belirti:** Tüm testler yeşil ama canlıda komisyon yanlış.
- **Kök neden:** Test, hesaplamanın kendisini kopyalayıp beklenen değeri aynı hatalı formülle üretiyor.
- **Nasıl bulunur:** Testteki beklenen değerin nereden geldiğine bak; elle hesapla.
- **Çözüm:** Beklenen değerleri bağımsız kaynaktan (iş biriminin Excel'i, elle hesap) al.
- **Projede:** Hakediş hesaplama testleri.
- **Seviye:** 2 · **Video:** hayır

---

## Nöbet senaryoları (seviye 5, birden fazla hatanın birleşimi)

Gerçek canlı ortam olayları gibi kurgulanır: alarm gelir, öğrenci Grafana panolarından, Loki
loglarından ve Tempo izlerinden ilerler, kök nedeni bulur, geçici ve kalıcı çözümü yazar, kısa bir
olay sonrası rapor (postmortem) hazırlar.

1. **Kara Cuma:** Flaş indirimde stok eksiye düşüyor ve API yavaşlıyor (EŞ-01 + DB-05 + API-09).
2. **Ödemeler yüzde 20 hata veriyor:** Ödeme servisi saat kayması ve zincirleme zaman aşımı (GÜV-01 + MS-02).
3. **E-postalar gitmiyor:** Bildirim servisi sessizce ölmüş, kuyruk birikmiş (NET-15 + EŞ-04).
4. **Dağıtımdan sonra site 4 dakika yok:** Kilitleyen migration (DB-11).
5. **Hakedişler tutmuyor:** Yuvarlama, çoğalan JOIN ve kayıp güncelleme (NET-17 + DB-07 + DB-04).

---

## Özet

| Grup | Adet |
| --- | --- |
| FE — React | 19 |
| API — HTTP | 15 |
| NET — .NET | 18 |
| EF — EF Core | 8 |
| DB — PostgreSQL | 13 |
| EŞ — Eşzamanlılık | 8 |
| GÜV — Güvenlik | 6 |
| ORT — Ortam | 7 |
| MS — Mikroservis | 5 |
| PERF — Performans | 4 |
| ARAÇ — Git/test | 5 |
| **Toplam** | **108** + 5 nöbet senaryosu |

Seviyelere göre: 1 → 22 · 2 → 40 · 3 → 29 · 4 → 15 · 5 → 3 (+ 5 nöbet senaryosu).

Video önerilen: 41 kayıt (yukarıda "Video: evet").
