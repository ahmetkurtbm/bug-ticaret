# Debug eğitim projesi — plan

> Çalışma adı: **bug-ticaret**. Kodda kurgusal şirket adı olarak **Tezgah** kullanılıyor (namespace: `Tezgah.*`); değişirse toplu yeniden adlandırılır.
>
> Hata listesi: [HATALAR.md](HATALAR.md)

## Amaç

Yeni mezun ve son sınıf öğrencilerine gerçek işin nasıl olduğunu öğretmek: başkasının yazdığı, büyük,
tanımadıkları bir kod tabanında bir hata kaydını alıp hatayı tekrar etmek, katmanlar arasında iz
sürmek, nerede patladığını bulmak, düzeltmek ve bir daha olmaması için test yazmak.

Öğrenci "şirkette ilk haftası" gibi bir deneyim yaşar: proje, hata kayıtları, loglar, git geçmişi ve
çözülecek sorunlar gerçek bir şirketteki gibidir.

## Kararlar

- **Kod tabanını kendimiz yazıyoruz.** Hatalar ve şirket hikâyesi kodla birlikte tasarlanır.
- **Alan: çok satıcılı pazar yeri.**
- **Açık kaynak.** Satış ve gelir modeli sonraya bırakıldı.
- **Dil:** Kod İngilizce; anlatım, hata kayıtları ve ipuçları Türkçe ve İngilizce.
- **Her hata ayrı git dalında.**
- **Mimari: monolitten mikroservise geçiş ortasındaki bir şirket.** Çoğu modül hâlâ monolitte,
  birkaçı ayrı servise taşınmış. Gerçek şirketlerdeki "yarım kalmış göç" durumu ve onun hataları da
  öğretilir.
- **IDE:** Anlatımlar Visual Studio ve VS Code için yazılır.
- **Bölümler:** [HATALAR.md](HATALAR.md) listesine göre. Çok zor olanlara ileride video eklenir.
- **İleride:** Mobil uygulama (React Native) kod tabanına eklenebilir.
- **Backend:** .NET (ASP.NET Core Web API). Sürüm önerisi: .NET 10 LTS.
- **Frontend:** React (Vite + TypeScript).
- **Kod tabanı büyük ve gerçekçi olacak.** Hedef 30-60 bin satır.
- **Veri katmanı gerçekten büyük olacak.** Çok sayıda tablo, gerçekçi ilişkiler, milyonlarca satıra
  kadar ölçeklenebilen örnek veri.

## Neden e-ticaret (pazar yeri)

- Herkes alanı bilir. Öğrenci "sipariş, sepet, kupon" demek için ayrıca iş bilgisi öğrenmez,
  zamanını debug'a ayırır.
- Veri doğal olarak büyük ve ilişkisel: ürün, varyant, stok, sipariş, ödeme, iade, kargo, kampanya,
  satıcı, komisyon, yorum.
- Klasik gerçek hayat hataları bu alanda doğal olarak çıkar: para yuvarlama ve KDV, stok yarışı,
  üst üste binen kuponlar, saat dilimiyle kayan kampanyalar, iki kez gelen ödeme bildirimi, yavaş
  raporlar, önbellekte kalan eski fiyat.
- Pazar yeri olması (müşteri sitesi + satıcı paneli + yönetim paneli + komisyon/hakediş) kurumsal
  bir şirket projesine çok daha çok benzer.

Şirket ve ürün tamamen kurgusal olacak; var olan bir pazar yerinin kopyası olmayacak.

## Mimari (taslak)

Hikâye: şirket yıllardır tek bir monolitle çalışıyor, iki yıl önce mikroservise geçmeye başladı ve
geçiş yarıda. Ödeme ve Bildirim ayrı servislere taşınmış; Katalog'un yeni servisi yazılıyor ama eski
tablolar hâlâ monolitte (strangler fig deseni). Bu, gerçek hayattaki en öğretici karmaşayı üretir:
iki veri kaynağı, sözleşme uyumsuzlukları, zincirleme zaman aşımları.

```
React müşteri sitesi ─┐
React satıcı paneli ──┼──► ASP.NET Core API (modüler monolit)
React yönetim paneli ─┘        │  Katalog (eski) · Sepet · Sipariş · Stok · Kargo
                               │  Kampanya · Satıcı · Hakediş · Kullanıcı · Rapor (legacy)
                               ├──► Ödeme servisi (ayrı, .NET) ──► sahte ödeme sağlayıcı
                               ├──► Bildirim servisi (ayrı, .NET) ──► sahte SMS / e-posta
                               ├──► Katalog servisi (yeni, yarım) ── eski tablolarla senkron
                               ├──► PostgreSQL (ana veri)
                               ├──► Redis (önbellek, oturum, sepet)
                               └──► RabbitMQ ──► Worker servisi (e-posta, kargo, hakediş, raporlar)

Sahte dış servisler: ödeme sağlayıcı, kargo firması, SMS/e-posta (ara sıra gecikir, hata döner)
Gözlemlenebilirlik: OpenTelemetry → Grafana / Loki / Tempo (mevcut observability-stack yeniden kullanılır)
Her şey Docker Compose ile tek komutla ayağa kalkar.
```

- **Veri erişimi:** Yeni modüllerde EF Core, eski "legacy" raporlama modülünde Dapper ve elle yazılmış
  SQL. Gerçek şirketlerdeki gibi iki stil bir arada.
- **Eski modül:** Bir modül bilerek eski tarzda: dokümantasyonu yok, yorumları yanıltıcı, adlandırması
  tutarsız. Öğrenci gerçek hayattaki "kimse dokunmak istemiyor" koduyla karşılaşır.
- **Testler ve CI:** Birim + entegrasyon testleri, GitHub Actions. Bazı testler bilerek eksik ya da
  yanlış şeyi test ediyor.
- **Git geçmişi:** Yüzlerce anlamlı commit. `git blame` ve `git bisect` gerçekten işe yarasın.

## Veri katmanı

### Alanlar ve tablolar (ilk liste)

- **Kullanıcı:** müşteriler, adresler, roller, oturumlar, giriş geçmişi
- **Satıcı:** satıcılar, mağaza bilgileri, belgeler, komisyon oranları, hakediş dönemleri, ödemeler
- **Katalog:** kategoriler (ağaç), markalar, ürünler, varyantlar (renk/beden), özellikler, görseller,
  fiyat geçmişi
- **Stok:** depolar, stok kalemleri, stok hareketleri (salt ekleme), rezervasyonlar
- **Sepet ve sipariş:** sepetler, siparişler, sipariş kalemleri, durum geçmişi, fatura bilgisi
- **Ödeme:** ödeme denemeleri, işlemler, iadeler, sağlayıcı bildirimleri (webhook kayıtları)
- **Kargo:** gönderiler, kargo hareketleri, teslimat denemeleri
- **İade:** iade talepleri, iade kalemleri, iade kargosu
- **Kampanya:** kuponlar, kampanya kuralları, kullanım kayıtları
- **İçerik:** yorumlar, puanlar, soru-cevap, favoriler
- **Sistem:** denetim kaydı (audit log), olay kuyruğu (outbox), zamanlanmış işler, ayarlar

Tahmini 60-90 tablo.

### Örnek veri üretici

- .NET konsol aracı, Bogus kütüphanesiyle Türkçe ve gerçekçi veri (isim, adres, ürün adı, fiyat).
- Aynı tohum değeri (seed) her seferinde aynı veriyi üretir; ipuçları ve testler buna dayanabilir.
- Üç profil:

| Profil | Müşteri | Ürün (varyantla) | Sipariş | Disk (tahmini) | Ne için |
| --- | --- | --- | --- | --- | --- |
| Küçük | 5 bin | 20 bin | 50 bin | < 1 GB | Mantık hataları, hızlı kurulum |
| Orta | 100 bin | 300 bin | 1 milyon | birkaç GB | Çoğu bölüm |
| Büyük | 1 milyon | 2 milyon | 10 milyon | 20 GB+ | Performans, index, rapor bölümleri |

Disk boyutları tahmin; üretici yazılınca ölçülecek.

- Verinin içinde bilerek "kirli" kayıtlar: eksik adres, çift müşteri, eski formatta telefon, silinmiş
  ürüne bağlı sipariş. Gerçek veritabanları da böyledir.

## Hata (bölüm) kataloğu — ilk taslak

Her bölüm: hata kaydı (şikâyet, önem, kim bildirdi, ekran görüntüsü/log), tekrar etme adımları,
kademeli ipuçları, doğrulama testi, "kıdemli geliştirici nasıl buldu" çözüm anlatımı.

| Seviye | Tür | Örnekler |
| --- | --- | --- |
| 1 — İlk hafta | Çöken kod, stack trace okuma | Boş adresli müşteride sipariş özeti patlıyor; null referans |
| 1 | Arayüz hataları | Sepet sayacı güncellenmiyor; form yanlış alanı gönderiyor |
| 2 — Sprint | Sessiz yanlış sonuç | KDV yuvarlaması kuruşu kaydırıyor; indirim iki kez uygulanıyor |
| 2 | Katmanlar arası | React alanı `discountCode`, API `couponCode` bekliyor, sessizce boş geçiyor |
| 2 | Tarih ve saat | Gece yarısı biten kampanya UTC yüzünden 3 saat erken bitiyor |
| 3 | Eşzamanlılık | Son 1 stoklu ürünü iki kişi aynı anda alıyor |
| 3 | Kuyruk ve arka plan | Ödeme bildirimi iki kez gelince sipariş iki kez onaylanıyor |
| 3 | Önbellek | Fiyat değişti ama sitede eski fiyat görünüyor |
| 4 — Veri | Performans | Satıcı raporu büyük veride 40 saniye: eksik index, N+1 sorgu |
| 4 | Transaction | İade yarıda kalınca stok geri eklenmiş ama para iade edilmemiş |
| 4 | Migration | Yeni kolon eklenince eski modülün raporu yanlış sayıyor |
| 5 — Nöbet | Canlı ortam olayları | Loglar ve izlerle: "ödemeler yüzde 20 hata veriyor, 30 dakikan var" |
| 5 | Ortam ve ayar | Yerelde çalışıyor, Docker'da bağlantı dizesi yanlış |
| 5 | Ara sıra çıkan hatalar | Kargo servisi gecikince zaman aşımı zincirleme hata yapıyor |

Hedef: ilk sürümde 15-20 bölüm, sonra 50+.

## Öğretilecek beceriler

- Tanımadığı kodda giriş noktasını bulmak (endpoint → servis → sorgu)
- Stack trace, log ve dağıtık iz (trace) okumak
- Visual Studio / Rider / VS Code debugger: breakpoint, adım adım ilerleme, koşullu breakpoint
- Tarayıcı DevTools: Network, Console, React DevTools
- SQL: sorgu planı (EXPLAIN), index, kilitler
- `git log`, `git blame`, `git bisect`
- Hatayı tekrar eden test yazmak, düzeltmek, PR açıklaması yazmak
- Hata kaydında ve ekiple yazılı iletişim

## Teslim şekli (öneri)

1. **İlk sürüm:** GitHub deposu + komut satırı aracı. `bolum baslat 7` hatalı senaryoyu kurar,
   `bolum kontrol` düzelip düzelmediğine bakar.
2. **Sonra:** Bölümleri, ipuçlarını, ilerlemeyi ve çözüm anlatımlarını gösteren bir web sitesi.
   Kod yine öğrencinin bilgisayarında çalışır.

## Durum (2026-11-11)

İlk sürüm tamamlandı:

- [x] **Temel:** Docker Compose, şema (33 tablo), örnek veri üretici (small/medium/large), kimlik doğrulama.
- [x] **Çekirdek ticaret:** katalog, sepet, kupon, sipariş (idempotent), stok rezervasyonu, sahte ödeme
      sağlayıcısı ve 3D sayfası, HMAC imzalı webhook, ödenmeyen siparişin iptali.
- [x] **Pazar yeri:** satıcı başvurusu ve paneli (ürün, varyant, stok, kargolama, özet), yönetim paneli
      (satıcı ve ürün onayı, siparişler, kampanyalar, satış raporu), komisyon.
- [x] **Yan sistemler:** outbox + RabbitMQ, Bildirim servisi (Mailpit), Dapper ile eski rapor modülü, Nginx.
- [x] **Bölümler:** 19 hata dalı, bölüm aracı (`backend/tools/Tezgah.Bolum`), kontroller, kayıt/ipucu/çözüm.
- [x] **Dokümantasyon:** README, kurulum, mimari, IDE (Visual Studio + VS Code).

Sırada: kargo durumu simülasyonu ve iade, hakediş ekranı, Katalog servisinin ayrılması (strangler fig),
gözlemlenebilirlik (OpenTelemetry), Redis önbellek, ikinci bölüm grubu, mobil uygulama, videolar.

## Aşamalar (taslak)

1. **Temel:** Docker Compose, veritabanı şeması, örnek veri üretici (küçük profil), kimlik doğrulama.
2. **Çekirdek ticaret:** Katalog, sepet, sipariş, ödeme (sahte sağlayıcı), stok.
3. **Pazar yeri:** Satıcı paneli, komisyon, hakediş, yönetim paneli.
4. **Yan sistemler:** Kuyruk, worker, kargo, e-posta, kampanya, iade, legacy rapor modülü.
5. **Gözlemlenebilirlik:** Loglar, metrikler, izler; orta ve büyük veri profilleri.
6. **Bölümler:** İlk 15-20 hata senaryosu, doğrulama testleri, ipuçları, çözüm anlatımları.
7. **CLI ve dokümantasyon:** Kurulum rehberi, bölüm aracı.

## Açık sorular

1. **Ad:** Çalışma adı bug-ticaret. Kesin ad ve kurgusal şirketin adı ne olsun?
2. **Büyük veri (soru 5'ten kalan):** Öneri: küçük ve orta profil öğrencinin bilgisayarında
   üretilir; büyük profil isteğe bağlı, hazır veritabanı dökümü olarak indirilir. En düşük donanım
   önerisi 16 GB RAM, 30 GB boş disk. Karar bekliyor.
3. **Çözüm anlatımları:** İlk sürümde yazılı; hangi hatalara video çekileceği listede "Video: evet"
   ile işaretli, sonraya.

## İlk sürüm bölümleri (2026-11-10)

Seçim ölçütü: her seviyeden örnek, en sık karşılaşılanlar önce, ve ilk aşamada yazılacak modüllerle
(katalog, sepet, sipariş, ödeme servisi, satıcı paneli, kampanya, Docker) kurulabilmesi.

| Sıra | Kod | Hata | Seviye | Gereken modül |
| --- | --- | --- | --- | --- |
| 1 | ORT-01 | Container içinde localhost | 1 | Docker Compose |
| 2 | FE-01 | Koşullu hook çağrısı | 1 | Ürün detay sayfası |
| 3 | FE-12 | SPA sayfasında yenileyince 404 | 1 | Nginx |
| 4 | API-01 | 400: doğrulama hatası okunmuyor | 1 | Satıcı başvuru formu |
| 5 | API-03 | 401 ve 403 karışıklığı | 1 | Satıcı paneli |
| 6 | NET-01 | CORS politikası çalışmıyor | 1 | API · satıcı paneli |
| 7 | NET-02 | Middleware sırası | 1 | Bildirim servisi |
| 8 | NET-03 | NullReferenceException (eski kayıt) | 1 | Fatura/sipariş özeti |
| 9 | FE-02 | Sonsuz useEffect döngüsü | 1 | Kategori sayfası |
| 10 | API-02 | Alan adı uyuşmazlığı | 1 | Sepette kupon |
| 11 | FE-03 | Bayat state (stale closure) | 2 | Sepet adet butonu |
| 12 | FE-14 | Türkçe ondalık virgül | 2 | Satıcı fiyat girişi |
| 13 | NET-09 | Türkçe "i" problemi | 2 | Giriş (e-posta normalleştirme) |
| 14 | NET-11 | DateTime.Now ve saat dilimi | 2 | Kampanya |
| 15 | EF-01 | N+1 sorgu | 2 | Yönetim sipariş listesi |
| 16 | ARAÇ-01 | git bisect ile hatayı bulmak | 2 | Kampanya geçmişi |
| 17 | DB-01 | Eksik index (orta/büyük veri) | 3 | Müşteri sipariş geçmişi |
| 18 | EŞ-01 | Son stoğu iki kişi alıyor | 3 | Stok/sipariş |
| 19 | EŞ-02 | Ödeme bildirimi iki kez geliyor | 3 | Ödeme servisi |

## Verilen kararlar (2026-11-10)

- Alan: pazar yeri · Açık kaynak, satış sonra · Dil: Türkçe + İngilizce · Hata başına ayrı dal ·
  Mimari: monolitten mikroservise yarım geçiş · IDE: Visual Studio + VS Code · Mobil: ileride
