> **Cevap anahtarı.** Bölümü kendiniz çözmeden okumayın; önce `ipucu` komutunu deneyin.

# Çözüm · Bölüm 31 (NET-04) · Yaşam süresi hatası: tek örnekte paylaşılan bağlantı

## Kök neden

Gösterge raporunu üreten `DapperReportingQueries` iki değişiklikle bozulmuş.

`backend/src/Tezgah.Infrastructure/DependencyInjection.cs` içinde servis **tekil** (singleton) kaydedilmiş:

```diff
-        services.AddScoped<Tezgah.Application.Reporting.IReportingQueries, Tezgah.Infrastructure.Reporting.DapperReportingQueries>();
+        // Tek örnek: rapor bağlantısı istekler arasında yeniden kullanılır.
+        services.AddSingleton<Tezgah.Application.Reporting.IReportingQueries, Tezgah.Infrastructure.Reporting.DapperReportingQueries>();
```

`backend/src/Tezgah.Infrastructure/Reporting/DapperReportingQueries.cs` içinde bağlantı alanda tutulup bir kez açılıyor:

```csharp
private NpgsqlConnection? _connection;

private async Task<NpgsqlConnection> ConnectionAsync(CancellationToken ct) =>
    _connection ??= await dataSource.OpenConnectionAsync(ct);

public async Task<SalesReport> GetSalesReportAsync(DateOnly from, DateOnly to, CancellationToken ct)
{
    var conn = await ConnectionAsync(ct);
    ...
}
```

Sonuç: uygulama boyunca **tek** bir `DapperReportingQueries` nesnesi ve onun içinde **tek** bir `NpgsqlConnection` vardır; bütün istekler aynı bağlantıyı kullanır. Bir PostgreSQL bağlantısı aynı anda yalnızca tek komut çalıştırabilir. Tek kişi gösterge açınca sorgular art arda çalışır, sorun yok. İki istek aynı anda gelince ikinci sorgu, birincisi bitmeden aynı bağlantıya komut göndermeye çalışır ve Npgsql `NpgsqlOperationInProgressException` ("A command is already in progress") fırlatır; yanıt `500` olur, ön yüz hata kutusu gösterir. Yenileyince çalışması, o anda başka bir isteğin bağlantıyı kullanmıyor olmasındandır; tutarsız ve rastgele görünmesi yarışın doğasıdır. Ayrıca bağlantı bir kez koparsa (veritabanı yeniden başlar, boşta kalma süresi dolar), alan dolu kaldığı için servis **bir daha düzelmez**: uygulama yeniden başlatılana kadar her rapor hata verir.

Değişikliği yapan geliştirici şunu düşünmüş (commit `abc1234`):

> Rapor sorgularında bağlantı yeniden kullanılıyor
> Gösterge her açılışta yeni bağlantı açıyordu (profilde ~40 ms). Rapor servisi tekil kaydedildi; bağlantı bir kez açılıp tutuluyor.

Bu bir **yaşam süresi** (lifetime) hatasıdır: durumu olan, paylaşılamayan bir kaynak (bağlantı), uzun ömürlü bir nesnenin (singleton) içine yerleştirilmiş. Üstelik kazanç varsayımsal: `NpgsqlDataSource` zaten bir **bağlantı havuzu**dur; `OpenConnectionAsync` çoğu zaman havuzdan hazır bir bağlantıyı mikrosaniyeler içinde verir ve `Dispose` bağlantıyı kapatmaz, havuza iade eder. "~40 ms" büyük olasılıkla ilk açılıştaki soğuk başlangıç maliyetidir; her istekte ödenmez.

## Nasıl bulunur

1. **Belirti:** Tek kullanıcıda hata yok, eşzamanlı kullanımda rastgele hata, yenilemede düzelme: bu üçlü paylaşılan değişken durum (yarış koşulu) işaretidir.
2. **Log:** `docker compose logs api` içinde `Npgsql.NpgsqlOperationInProgressException: A command is already in progress: ...` ve altında `DapperReportingQueries` içindeki çağrı görünür.
3. **Tekrar:** `kontrol 31` göstergeyi aynı anda 36 kez açar; çıktıda "…36 kez açıldı, N tanesinde hata sayfası geldi" yazar.
4. **Kod:** `DapperReportingQueries.cs` içinde alan olarak tutulan `_connection`, `DependencyInjection.cs` içinde `AddSingleton`. Diğer servisler (veritabanı bağlamı, uygulama servisleri) `AddScoped`. `git log -p` iki değişikliği birlikte gösterir.
5. **Düşünce testi:** "Bu sınıfın bir örneği kaç isteğe hizmet ediyor ve içindeki her alan o kadar isteğe aynı anda güvenli mi?"

**Araç ipucu:** `kontrol 31` yerine kendiniz denemek için `curl` ile aynı anda çok istek atabilirsiniz (ör. bir `for` döngüsünde arka plana atılan istekler); yarış koşullarını yakalamanın en kısa yolu isteği gerçekten paralel göndermektir.

## Düzeltme

Servisi yeniden `scoped` kaydedin ve bağlantıyı alanda tutmayı bırakın; her işlemde havuzdan alıp işi bitince iade edin:

```diff
-        // Tek örnek: rapor bağlantısı istekler arasında yeniden kullanılır.
-        services.AddSingleton<Tezgah.Application.Reporting.IReportingQueries, Tezgah.Infrastructure.Reporting.DapperReportingQueries>();
+        services.AddScoped<Tezgah.Application.Reporting.IReportingQueries, Tezgah.Infrastructure.Reporting.DapperReportingQueries>();
```

```diff
-public class DapperReportingQueries(NpgsqlDataSource dataSource) : IReportingQueries, IAsyncDisposable
+public class DapperReportingQueries(NpgsqlDataSource dataSource) : IReportingQueries
 {
-    // Rapor birkaç sorguyu art arda çalıştırıyor; bağlantı her seferinde açılmaz, bir kez açılıp tutulur.
-    private NpgsqlConnection? _connection;
-
-    private async Task<NpgsqlConnection> ConnectionAsync(CancellationToken ct) =>
-        _connection ??= await dataSource.OpenConnectionAsync(ct);
-
-    public async ValueTask DisposeAsync()
-    {
-        if (_connection is not null) await _connection.DisposeAsync();
-    }
 ...
     public async Task<SalesReport> GetSalesReportAsync(DateOnly from, DateOnly to, CancellationToken ct)
     {
-        var conn = await ConnectionAsync(ct);
+        await using var conn = await dataSource.OpenConnectionAsync(ct);
```

Bu hâlde her istek kendi bağlantısını havuzdan alır, rapor bitince `await using` bağlantıyı havuza iade eder; istekler birbirine karışmaz ve kopan bağlantı havuzda yenisiyle değiştirilir.

## Neden böyle düzeltiyoruz

- **Singleton kalıp bağlantıyı `lock`/`SemaphoreSlim` ile korumak.** Hata kaybolur ama tüm gösterge istekleri sıraya girer: tek bağlantı darboğaz olur, gecikmeler istek sayısıyla artar ve koparsa yine düzelmez. Havuz tam olarak bunu çözmek için vardır.
- **Singleton kalıp her çağrıda `dataSource.OpenConnectionAsync` kullanmak.** Çalışır (singleton'ın içinde durum kalmaz), ama servisin kullanım ömrü ile bağımlılıkları arasındaki uyumsuzluk (ileride eklenecek scoped bağımlılıklar için "captive dependency") tuzağı olarak durur. Kaydı `scoped` yapmak, geri kalan servislerle tutarlıdır.
- **Havuzu büyütmek (`Maximum Pool Size`).** Sorun havuz boyutu değil, tek bağlantının paylaşılması.
- **"Yavaşlığı" ölçmeden optimize etmek.** Düzeltmenin kendisi de bir ölçüm gerektirir: gerçekten bir sorun varsa nerede, ne kadar olduğunu profil çıkarıp gösterin.

## Bir daha olmaması için

- **Test:** Aynı servisi paralel çağıran bir entegrasyon testi (ör. 20-50 eşzamanlı `GetSalesReportAsync`), hiçbirinin istisna fırlatmadığını doğrular. Bölümün kontrolü de bunu 36 eşzamanlı istekle yapar. Tek isteklik testler bu hatayı yakalayamaz.
- **Kod inceleme maddesi:** Bir sınıf `AddSingleton` ile kaydediliyorsa şu soruyu sorun: "Bu sınıfın değişebilen alanı var mı? İçindeki her şey eşzamanlı erişime güvenli mi?" Bağlantı, `DbContext`, `HttpContext`, kullanıcı/istek bilgisi taşıyan alanlar singleton'a ait değildir.
- **Doğrulama:** `ServiceProviderOptions.ValidateScopes = true` ve `ValidateOnBuild = true` (geliştirme ortamında varsayılandır) scoped bir servisin singleton içine yerleştirilmesini yakalar. Burada tersi (singleton içinde paylaşılan durum) olduğundan araç yakalamaz; bu yüzden inceleme gerekir.
- **Performans disiplini:** Önce ölç, sonra optimize et. Ölçümü commit mesajına ve sonucu PR'a ekleyin; "profilde ~40 ms" bir gözlem değil, bir hipotezdir.
- **İzleme:** `500` oranı ve istisna türüne göre sayaçlar; "A command is already in progress" gibi yarış belirtileri bir alarma bağlansın.

## İlgili

HATALAR.md · **NET-04 Captive dependency (yaşam süresi hatası)**

## English summary

**Root cause:** `DapperReportingQueries` was registered as a singleton in `DependencyInjection.cs` (`AddSingleton` instead of `AddScoped`) and was changed to keep one `NpgsqlConnection` in a field (`_connection ??= await dataSource.OpenConnectionAsync(ct)`). All requests therefore share one connection, and a PostgreSQL connection can run only one command at a time. A single user is fine; when two requests overlap, Npgsql throws `NpgsqlOperationInProgressException` ("A command is already in progress") and the API returns `500`. If the connection ever breaks, the stored field is never replaced, so the service never recovers until restart. The developer (commit "Rapor sorgularında bağlantı yeniden kullanılıyor") wanted to save "about 40 ms", but `NpgsqlDataSource` is already a connection pool, so opening a connection per operation is cheap.

**How to find it:** errors only under overlapping requests and gone on refresh; the API log shows the exception; `kontrol 31` fires 36 parallel requests; the `_connection` field plus `AddSingleton` in the code.

**Fix:** register the service as scoped again and open the connection per operation with `await using var conn = await dataSource.OpenConnectionAsync(ct)`; the pool returns it on dispose.

**Prevent:** parallel integration tests, review of every singleton for mutable non-thread-safe state, measuring before optimizing, and alerts on `500` rates and race-condition exception types.
