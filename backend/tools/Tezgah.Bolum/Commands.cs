namespace Tezgah.Bolum;

public static class Commands
{
    public static int Help()
    {
        Console.WriteLine("""
            Tezgah bölüm aracı

              liste               Bölümleri listeler
              baslat <no>         Bölümün dalından bir çalışma dalı açar (calisma/NN-...)
              ipucu <no> [sıra]   Sıradaki ipucunu gösterir (1, 2, 3)
              kontrol [no]        Hatanın düzelip düzelmediğini kontrol eder
              bitir               main dalına döner

            Örnek: dotnet run --project backend/tools/Tezgah.Bolum -- baslat 8
            """);
        return 0;
    }

    public static int List(Repository repo, IReadOnlyList<Chapter> chapters)
    {
        var current = repo.CurrentBranch();
        Output.Title($"{"No",-3} {"Kod",-8} {"Sv",-3} {"Başlık",-46} Modül");
        foreach (var c in chapters)
        {
            var mark = current == c.Branch || current == c.WorkBranch ? "►" : " ";
            Console.WriteLine($"{mark}{c.No,-2} {c.Code,-8} {new string('●', c.Level),-3} {Truncate(c.Title, 46),-46} {c.Module}");
        }
        Console.WriteLine();
        Console.WriteLine("Başlamak için: baslat <no>   ·   Ayrıntı: bolumler/<no>-<kod>/KAYIT.md");
        return 0;
    }

    public static int Start(Repository repo, IReadOnlyList<Chapter> chapters, string? arg)
    {
        if (arg is null) throw new BolumException("Bölüm numarası verin: baslat 8");
        var chapter = ChapterCatalog.Find(chapters, arg, repo);
        if (!repo.IsClean())
            throw new BolumException("Kaydedilmemiş değişiklikleriniz var. Önce commit'leyin ya da `git stash` ile kenara alın.");
        if (!repo.BranchExists(chapter.Branch))
        {
            var fetch = repo.Git($"fetch origin {chapter.Branch}:{chapter.Branch}");
            if (fetch.ExitCode != 0) throw new BolumException($"{chapter.Branch} dalı bulunamadı. `git fetch --all` deneyin.");
        }

        var result = repo.BranchExists(chapter.WorkBranch)
            ? repo.Git($"switch {chapter.WorkBranch}")
            : repo.Git($"switch -c {chapter.WorkBranch} {chapter.Branch}");
        if (result.ExitCode != 0) throw new BolumException(result.Output.Trim());

        Output.Title($"Bölüm {chapter.No} · {chapter.Code} · {chapter.Title}");
        Output.Info($"Çalışma dalı: {chapter.WorkBranch} (kaynak: {chapter.Branch})");
        Output.Info($"Hata kaydı: bolumler/{chapter.Folder}/KAYIT.md");
        if (chapter.Setup.Length > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Hazırlık:");
            foreach (var step in chapter.Setup) Console.WriteLine($"  - {step}");
        }
        Console.WriteLine();
        Console.WriteLine($"Takılırsanız: ipucu {chapter.No}   ·   Bitirince: kontrol");
        return 0;
    }

    public static int Hint(Repository repo, IReadOnlyList<Chapter> chapters, string? arg, string? order)
    {
        var chapter = ChapterCatalog.Find(chapters, arg, repo);
        var path = Path.Combine(repo.Root, "bolumler", chapter.Folder, "IPUCLARI.md");
        if (!File.Exists(path)) throw new BolumException($"{path} bulunamadı.");

        // Başlıklar "## İpucu 1", "## İpucu 2" ... biçiminde.
        var sections = File.ReadAllText(path).Split("\n## ", StringSplitOptions.RemoveEmptyEntries).Skip(1).ToList();
        if (sections.Count == 0) throw new BolumException("İpucu bulunamadı.");
        var index = int.TryParse(order, out var n) ? Math.Clamp(n, 1, sections.Count) : 1;
        Output.Title($"Bölüm {chapter.No} · {sections[index - 1].Split('\n')[0].Trim()}");
        Console.WriteLine(string.Join('\n', sections[index - 1].Split('\n').Skip(1)).Trim());
        if (index < sections.Count)
        {
            Console.WriteLine();
            Output.Info($"Sonraki ipucu: ipucu {chapter.No} {index + 1}");
        }
        return 0;
    }

    public static async Task<int> CheckAsync(Repository repo, IReadOnlyList<Chapter> chapters, string? arg)
    {
        var chapter = ChapterCatalog.Find(chapters, arg, repo);
        Output.Title($"Kontrol · Bölüm {chapter.No} · {chapter.Code} · {chapter.Title}");
        var check = Checks.For(chapter.Check);
        var context = new CheckContext(repo);
        bool passed;
        try
        {
            passed = await check(context);
        }
        catch (HttpRequestException ex)
        {
            Output.Fail($"Servise ulaşılamadı: {ex.Message}");
            Output.Info("Servisler çalışıyor mu? Hazırlık adımları için: bolumler/" + chapter.Folder + "/KAYIT.md");
            passed = false;
        }
        catch (Npgsql.NpgsqlException ex)
        {
            Output.Fail($"Veritabanına bağlanılamadı: {ex.Message}");
            Output.Info("`docker compose up -d` çalıştırıldı mı?");
            passed = false;
        }

        Console.WriteLine();
        if (passed)
        {
            Output.Pass("Bölüm tamamlandı. Çözümü karşılaştırın: bolumler/" + chapter.Folder + "/COZUM.md");
            return 0;
        }
        Output.Fail("Henüz düzelmedi. İpucu için: ipucu " + chapter.No);
        return 1;
    }

    public static int Finish(Repository repo)
    {
        if (!repo.IsClean())
            throw new BolumException("Kaydedilmemiş değişiklikler var. Commit'leyin ya da `git stash` yapın.");
        var result = repo.Git("switch main");
        if (result.ExitCode != 0) throw new BolumException(result.Output.Trim());
        Output.Pass("main dalına dönüldü. Çalışma dalınız duruyor; silmek için: git branch -D calisma/...");
        return 0;
    }

    private static string Truncate(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";
}
