> **Cevap anahtarı.** Bölümü kendiniz çözmeden okumayın; önce `ipucu` komutunu deneyin.

# Çözüm · Bölüm 29 (ORT-06) · Container imajı değişince kaybolan ortam özellikleri

## Kök neden

`backend/Dockerfile` içindeki çalışma imajı Debian tabanlı imajdan Alpine tabanlı olana taşınmış:

```diff
-FROM mcr.microsoft.com/dotnet/aspnet:10.0
-RUN apt-get update \
- && apt-get install -y --no-install-recommends tzdata curl \
- && rm -rf /var/lib/apt/lists/*
+FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
+RUN apk add --no-cache tzdata curl
```

Alpine tabanlı .NET imajları `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true` ortam değişkeniyle gelir ve **ICU** (dil, bölge ve karşılaştırma verisi) kütüphanesini içermez. Bu modda .NET yalnızca "değişmez kültür"ü bilir. `backend/src/Tezgah.Api/Program.cs` ise açılışta şunu çağırıyor:

```csharp
var culture = CultureInfo.GetCultureInfo(builder.Configuration["Localization:Culture"] ?? "tr-TR");
```

Değişmez modda bu çağrı `CultureNotFoundException` fırlatır ("Only the invariant culture is supported in globalization-invariant mode") ve API daha hiçbir şey çalışmadan çöker; container `exited` olur, siteler veri alamaz. Ödeme ve Bildirim servisleri açılışta kültür ayarlamadığı için ayakta kalır (Ödeme servisinin sayfaları `tr-TR` kültürünü ilk kullandıklarında aynı hatayı verirdi; hata yalnızca ertelenmiş olurdu). Geliştiricinin bilgisayarında (Windows/macOS/Linux'ta ICU yüklü) hata hiç görünmez: "bende çalışıyor".

Değişikliği yapan geliştirici şunu düşünmüş (commit `abc1234`):

> Çalışma imajı alpine'a taşındı
> Servis imajları yaklaşık yarı boyuta indi; CI'da imaj itme/çekme süresi kısaldı.

Hedef (küçük imaj) meşru ve ölçülmüş bir kazanç. Ama bir imajı değiştirmek, imajın **varsayılan olarak sunduğu her şeyi** değiştirmek demektir: C kütüphanesi (musl/glibc), ICU, saat dilimi verisi, kabuk, kurulu araçlar. Bunlar kodda görünmez; yalnızca çalışma anında ortaya çıkar. Dosyanın kendi yorumu bile iki bağımlılığı (tzdata, curl) açıkça saymış; ICU o listede yoktu.

## Nasıl bulunur

1. **Belirti:** Aynı kod bilgisayarda çalışıyor, container'da çalışmıyor. Bu, fark kodda değil ortamdadır demektir.
2. **Durum:** `docker compose ps -a` içinde `api` `exited (...)`; `docker compose logs api` çıktısının başında işlenmemiş bir istisna ve "Only the invariant culture is supported in globalization-invariant mode." mesajı vardır. İstisna `Program.cs` içindeki `CultureInfo.GetCultureInfo` satırını gösterir.
3. **Neden yalnızca API:** Diğer iki servis açılışta kültür ayarlamıyor.
4. **Geçmiş:** `git log -p -- backend/Dockerfile` imaj değişikliğini gösterir; "geçen hafta çalışıyordu" ifadesi bu tür bir değişikliği aratır.
5. **Doğrulama:** `docker run --rm mcr.microsoft.com/dotnet/aspnet:10.0-alpine printenv DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` → `true` yazar; `…:10.0` imajında değişken tanımlı değildir.

**Araç ipucu:** `docker compose logs api` yerine `docker compose logs --tail 50 api` ya da `docker logs <container>`; ayakta kalamayan container için `docker compose ps -a` şarttır (`ps` yalnızca çalışanları listeler).

## Düzeltme

Seçenek 1 (en basit): önceki imaja dönmek.

```diff
-FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
-RUN apk add --no-cache tzdata curl
+FROM mcr.microsoft.com/dotnet/aspnet:10.0
+RUN apt-get update \
+ && apt-get install -y --no-install-recommends tzdata curl \
+ && rm -rf /var/lib/apt/lists/*
```

Seçenek 2: küçük imajı koruyup eksik bileşeni eklemek.

```diff
 FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
-RUN apk add --no-cache tzdata curl
+RUN apk add --no-cache tzdata curl icu-libs icu-data-full
 ARG APP_DLL
 ENV APP_DLL=$APP_DLL \
     ASPNETCORE_HTTP_PORTS=8080 \
-    TZ=UTC
+    TZ=UTC \
+    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
```

`icu-libs` yalnızca kütüphaneyi, `icu-data-full` ise Türkçe gibi dillerin verisini getirir; `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false` ise .NET'e ICU'yu kullanmasını söyler. Üçünden biri eksikse sorun sürer. Değişiklikten sonra `docker compose --profile app up -d --build` ile yeniden derleyin.

## Neden böyle düzeltiyoruz

- **Kodu değiştirip `GetCultureInfo`'yu try/catch'e almak ya da `InvariantCulture`'a düşmek.** Kodun kültür varsayımı bilinçlidir (yorum: "Şirketin sunucuları Türkçe kültürle çalışır"). Ortam eksikliğini koda taşımak, tarih/para biçimlerinin container'da sessizce farklı çıkmasına yol açar.
- **Yalnızca `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false` yazmak.** ICU kütüphanesi imajda yoksa .NET açılışta yine hata verir; ikisi birlikte gerekir.
- **`LANG`/`LC_ALL` ayarlamak.** Bu değişkenler .NET'in kültür desteğinin varlığını sağlamaz.
- **İmaj küçültmeyi tamamen bırakmak.** Seçenek 2 küçüklük kazancını korur; ancak ICU verisi eklendiği için kazancın bir kısmı geri gider. Bunun değip değmediği ölçülerek karar verilir.

## Bir daha olmaması için

- **Aynı imajla test:** Geliştirici makinesinde çalışmak yetmez; her imaj değişikliğinden sonra CI'da `docker compose --profile app up -d --build` ile sistemi gerçekten ayağa kaldırıp sağlık kontrolünü bekleyen bir duman testi. Bölümün kontrolü de container'daki API'nin ayakta olup olmadığına bakar.
- **Sağlık kontrolü ve `depends_on`:** API'nin sağlık kontrolü zaten tanımlı; CI bu kontrol geçmeden "başarılı" saymasın.
- **Kod inceleme maddesi:** Taban imaj değişikliğinde "bu imaj neyi **farklı** getiriyor?" sorusu: C kütüphanesi, ICU, saat dilimi verisi, kabuk (`sh` / `bash`), kullanıcı hesabı, kurulu araçlar. Değişikliği öneren, bunların her biri için uygulamanın bağımlılığı olup olmadığını yazsın.
- **Ortam parite ilkesi:** Geliştirme, CI ve üretim aynı imajı kullansın; yerelde `dotnet run` ile "çalışıyor" demek container'da çalışacağını garanti etmez.
- **Başlangıç denetimi:** Uygulama açılışta kültürü gerçekten bulabildiğini doğrulayıp net bir hata mesajı yazsın (örneğin ICU eksikse nedenini söyleyen bir log), böylece sorun dakikalar yerine saniyeler içinde bulunur.

## İlgili

HATALAR.md · **ORT-06 Container saat dilimi ve kültür**

## English summary

**Root cause:** the runtime image in `backend/Dockerfile` was changed from `mcr.microsoft.com/dotnet/aspnet:10.0` (Debian) to `…:10.0-alpine`. Alpine-based .NET images ship with `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true` and without ICU, so only the invariant culture is available. The API's `Program.cs` calls `CultureInfo.GetCultureInfo("tr-TR")` at startup, which throws `CultureNotFoundException` ("Only the invariant culture is supported in globalization-invariant mode"), and the container exits. The Payments and Notifications services do not set a culture at startup, so they stay up. On the developer's machine ICU is installed, so nothing fails there. The developer (commit "Çalışma imajı alpine'a taşındı") wanted smaller images, but changing a base image changes everything the image provides implicitly.

**How to find it:** `docker compose ps -a` shows `api` exited; `docker compose logs api` shows the unhandled exception pointing to `CultureInfo.GetCultureInfo`; `git log -p -- backend/Dockerfile` shows the image change.

**Fix:** go back to the Debian-based image, or keep Alpine and add `icu-libs icu-data-full` plus `ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false`.

**Prevent:** a CI smoke test that starts the whole system in containers and waits for health, a review checklist for base-image changes (libc, ICU, time-zone data, shell, tools), parity between dev, CI and production images, and a clear startup error when the culture is missing.
