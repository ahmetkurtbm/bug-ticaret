# İpuçları · Bölüm 29

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

"Bende çalışıyor" ile "container'da çalışmıyor" arasındaki farkı container'ın kendisi söyler. `docker compose ps -a` ile `api` servisinin çıkış durumuna bakın, sonra `docker compose logs api` çıktısını baştan okuyun. Açılışta düşen bir uygulamanın son satırları neredeyse her zaman nedenini yazar; iç içe istisna (inner exception) mesajını da okuyun.

*EN:* The container itself tells you the difference between "works on my machine" and "does not work in a container". Check the exit status of the `api` service with `docker compose ps -a`, then read `docker compose logs api` from the beginning. The last lines of an application that crashes at startup almost always say why; also read the inner exception message.

## İpucu 2

Log, uygulamanın açılışta daha ilk satırlarında bir istisna fırlattığını ve bir ortam özelliğinin desteklenmediğini söylüyor. Aynı kod bilgisayarınızda çalıştığına göre fark kodda değil, kodun **çalıştığı ortamda**. Son zamanlarda ortamı tanımlayan dosyada (`backend/Dockerfile`) ne değişti? `git log -p -- backend/Dockerfile` ile bakın. Neden yalnızca API düşüyor da diğer servisler ayakta? API'nin `Program.cs` dosyasının ilk satırlarında diğer servislerde bulunmayan ne var?

*EN:* The log says the application throws an exception in its very first lines at startup and that a feature of the environment is not supported. Since the same code runs on your computer, the difference is not in the code but in the **environment it runs in**. What changed recently in the file that defines the environment (`backend/Dockerfile`)? Look with `git log -p -- backend/Dockerfile`. Why does only the API go down while the other services stay up? What is in the first lines of the API's `Program.cs` that the other services do not have?

## İpucu 3

`backend/Dockerfile` içinde çalışma imajı `mcr.microsoft.com/dotnet/aspnet:10.0` iken `…:10.0-alpine` yapılmış. Bu hafif imajlar yalnızca küçük değil, aynı zamanda **dil ve bölge desteği kütüphanesi olmadan** (ICU yok) gelir ve .NET o durumda "yalnızca kültürden bağımsız mod"da çalışır. `Program.cs` ise açılışta `CultureInfo.GetCultureInfo("tr-TR")` çağırıyor; böyle bir modda yalnızca değişmez kültür desteklenir, bu çağrı istisna fırlatır ve süreç çıkar. Log mesajındaki "invariant" kelimesini aratın.

*EN:* In `backend/Dockerfile` the runtime image was changed from `mcr.microsoft.com/dotnet/aspnet:10.0` to `…:10.0-alpine`. These light images are not only smaller; they also come **without the language and region support library** (no ICU), and .NET then runs in "invariant globalization mode". `Program.cs` calls `CultureInfo.GetCultureInfo("tr-TR")` at startup; in such a mode only the invariant culture is supported, so the call throws and the process exits. Search the log message for the word "invariant".
