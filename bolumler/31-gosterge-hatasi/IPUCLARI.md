# İpuçları · Bölüm 31

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Hata kutusu yalnızca ön yüzün yakaladığı bir belirtidir. F12 → Network sekmesinde başarısız olan isteğin durum kodunu görün (büyük olasılıkla `500`). Asıl nedeni API'nin logu yazar: `kontrol 31` çalıştırdıktan hemen sonra `docker compose logs api` çıktısına bakın ve işlenmemiş istisnaları (exception) okuyun. İstisnanın türü ve mesajı ne diyor?

*EN:* The error box is only a symptom caught by the front end. In F12 → Network, see the status code of the failing request (most likely `500`). The real reason is in the API log: right after running `kontrol 31`, look at the output of `docker compose logs api` and read the unhandled exceptions. What do the type and the message of the exception say?

## İpucu 2

Hata tek istekte değil, birden çok istek aynı anda geldiğinde çıkıyor ve istisna mesajı "bir işlem zaten sürüyor" diyor. Demek ki aynı nesneyi aynı anda birden çok istek kullanıyor ve o nesne bunu kaldıramıyor. Sorulacak soru: istekler arasında paylaşılan nesne hangisi? Her isteğe ayrı mı verilir, yoksa uygulama boyunca tek bir tane mi vardır? Raporu üreten sınıf `backend/src/Tezgah.Infrastructure/Reporting/DapperReportingQueries.cs`; sınıfın alanlarına bakın.

*EN:* The error does not appear with a single request but when several requests arrive at the same moment, and the exception message says "a command is already in progress". So several requests use the same object at the same time and that object cannot handle it. The question to ask: which object is shared between requests? Is it given to each request separately, or is there one for the whole application lifetime? The class that produces the report is `backend/src/Tezgah.Infrastructure/Reporting/DapperReportingQueries.cs`; look at the fields of the class.

## İpucu 3

Rapor sınıfı bir veritabanı bağlantısını alanında (`_connection`) tutuyor ve ilk kullanımda bir kez açıp sonraki her çağrıda aynısını kullanıyor. Bu bağlantı aynı anda yalnızca tek komut çalıştırabilir. Sınıfın **ömrünü** belirleyen yer `backend/src/Tezgah.Infrastructure/DependencyInjection.cs`: rapor servisi hangi yaşam süresiyle kaydedilmiş, öteki servisler (örneğin veritabanı bağlamı) hangisiyle? Commit mesajındaki "yaklaşık 40 ms" kazancı gerçekten ölçülmüş mü, yoksa havuzun zaten çözdüğü bir maliyet mi? Önceki hâli için `git log -p -- backend/src/Tezgah.Infrastructure/DependencyInjection.cs backend/src/Tezgah.Infrastructure/Reporting/DapperReportingQueries.cs`.

*EN:* The report class keeps a database connection in a field (`_connection`), opens it once on first use and uses the same one for every later call. That connection can run only one command at a time. The place that decides the class's **lifetime** is `backend/src/Tezgah.Infrastructure/DependencyInjection.cs`: with which lifetime is the report service registered, and which lifetime do the other services (for example the database context) have? Was the "about 40 ms" gain in the commit message really measured, or is it a cost the pool already solves? For the previous version, see `git log -p -- backend/src/Tezgah.Infrastructure/DependencyInjection.cs backend/src/Tezgah.Infrastructure/Reporting/DapperReportingQueries.cs`.
