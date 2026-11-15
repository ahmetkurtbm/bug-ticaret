# İpuçları · Bölüm 16 (ARAÇ-01)

Üç ipucu var, ağırlığı giderek artar. Önce kendiniz deneyin; takılınca sırayla açın.

## İpucu 1

Bu bölümde sorunun "nerede" olduğunu aramadan önce "ne zaman başladı"yı sorun. `main` dalında kampanya geçmişi doğru, bölüm dalında yanlış; ikisi arasında 12 commit var. Bu tür aramanın aracı `git bisect`'tir: `git bisect start`, `git bisect bad` (şu anki commit bozuk), `git bisect good main` (main doğru). Git size aradaki bir commit'i çıkarır; API'yi o commit'te yeniden başlatıp `dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 16` çalıştırır, sonuca göre `git bisect good` ya da `git bisect bad` dersiniz.

*EN:* In this chapter, before asking "where" the problem is, ask "when" it started. The campaign history is correct on `main` and wrong on the chapter branch; there are 12 commits between them. The tool for this kind of search is `git bisect`: `git bisect start`, `git bisect bad` (the current commit is broken), `git bisect good main` (main is good). Git checks out a commit in between; you restart the API at that commit, run `dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 16`, and answer `git bisect good` or `git bisect bad` depending on the result.

## İpucu 2

12 commit için en fazla 4 adım yeter: her adım aday sayısını yarıya indirir. Elle yapmak istemezseniz `git bisect run` aynı işi otomatik yapar; bu klasördeki `bisect-kontrol.sh` (Linux, macOS, Git Bash) ya da `bisect-kontrol.ps1` (Windows PowerShell) her adımda API'yi derler, başlatır, `kontrol 16`'yı çalıştırır, API'yi durdurur ve çıkış kodunu verir. Bisect commit değiştirirken betik de değişebileceği için betiği önce depo dışına (`/tmp` ya da `$env:TEMP`) kopyalayın. Bisect bittiğinde `git bisect reset` ile dalınıza dönün. Bulduğunuz commit'in `git show` çıktısına bakın: hangi sorgu, hangi sıralama?

*EN:* Four steps at most are enough for 12 commits: each step halves the number of candidates. If you do not want to do it by hand, `git bisect run` does the same automatically; `bisect-kontrol.sh` (Linux, macOS, Git Bash) or `bisect-kontrol.ps1` (Windows PowerShell) in this folder builds and starts the API at each step, runs `kontrol 16`, stops the API and returns the exit code. Because the script itself can change while bisect switches commits, first copy it outside the repository (`/tmp` or `$env:TEMP`). When bisect finishes, return to your branch with `git bisect reset`. Look at the `git show` output of the commit it finds: which query, which ordering?

## İpucu 3

Bozan commit "Kampanya geçmişi sorgusu sadeleştirildi" (Emre Şahin). `backend/src/Tezgah.Application/Admin/AdminService.cs` içindeki `CampaignHistoryAsync` sorgusunda `OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)` yerine `OrderBy(a => a.Id)` konmuş. Sıralama artık eskiden yeniye ve sorgunun sonundaki `Take(...)` ilk N kaydı, yani en eski N kaydı alıyor. Yeniden eskiye sıralamayı geri getirin. Aynı yola `git log -S"OrderByDescending(a => a.CreatedAt)"` ya da `git blame` ile de ulaşabilirsiniz.

*EN:* The culprit is the commit "Kampanya geçmişi sorgusu sadeleştirildi" (Emre Şahin). In the `CampaignHistoryAsync` query in `backend/src/Tezgah.Application/Admin/AdminService.cs`, `OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)` was replaced with `OrderBy(a => a.Id)`. The order is now oldest to newest, and the `Take(...)` at the end takes the first N records, i.e. the oldest N. Restore newest-to-oldest ordering. You can reach the same place with `git log -S"OrderByDescending(a => a.CreatedAt)"` or `git blame`.
