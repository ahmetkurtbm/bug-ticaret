using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tezgah.Bolum;

public class BolumException(string message) : Exception(message);

public record Chapter(
    int No,
    string Code,
    string Slug,
    string Title,
    [property: JsonPropertyName("titleEn")] string TitleEn,
    int Level,
    string Module,
    string Check,
    string[] Setup)
{
    public string Folder => $"{No:D2}-{Code.ToLowerInvariant().Replace("ç", "c").Replace("ş", "s").Replace("ğ", "g").Replace("ü", "u").Replace("ö", "o").Replace("ı", "i")}";
    public string Branch => $"bolum/{Folder}";
    public string WorkBranch => $"calisma/{Folder}";
}

public static class ChapterCatalog
{
    public static IReadOnlyList<Chapter> Load(Repository repo)
    {
        var path = Path.Combine(repo.Root, "bolumler", "bolumler.json");
        var list = JsonSerializer.Deserialize<List<Chapter>>(File.ReadAllText(path), new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new BolumException("bolumler/bolumler.json okunamadı.");
        return list.OrderBy(c => c.No).ToList();
    }

    public static Chapter Find(IReadOnlyList<Chapter> chapters, string? arg, Repository repo)
    {
        if (arg is null)
        {
            var branch = repo.CurrentBranch();
            var match = chapters.FirstOrDefault(c => branch == c.Branch || branch == c.WorkBranch || branch.EndsWith("/" + c.Folder, StringComparison.Ordinal));
            return match ?? throw new BolumException($"Bulunduğunuz dal ({branch}) bir bölüme ait değil. Bölüm numarası verin: kontrol 8");
        }
        var byNo = int.TryParse(arg, out var no) ? chapters.FirstOrDefault(c => c.No == no) : null;
        return byNo
            ?? chapters.FirstOrDefault(c => string.Equals(c.Code, arg, StringComparison.OrdinalIgnoreCase))
            ?? throw new BolumException($"\"{arg}\" diye bir bölüm yok. Liste için: liste");
    }
}

public class Repository(string root)
{
    public string Root { get; } = root;

    public static Repository Locate()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "bolumler", "bolumler.json")))
                return new Repository(dir.FullName);
            dir = dir.Parent;
        }
        throw new BolumException("Depo kökü bulunamadı. Komutu bug-ticaret klasörünün içinden çalıştırın.");
    }

    public string CurrentBranch() => Git("rev-parse --abbrev-ref HEAD").Output.Trim();

    public bool IsClean() => Git("status --porcelain --untracked-files=no").Output.Trim().Length == 0;

    public bool BranchExists(string name) => Git($"rev-parse --verify --quiet refs/heads/{name}").ExitCode == 0;

    public ProcessResult Git(string arguments) => Shell.Run("git", arguments, Root);
}

public record ProcessResult(int ExitCode, string Output);

public static class Shell
{
    public static ProcessResult Run(string file, string arguments, string workingDirectory, bool echo = false)
    {
        var info = new ProcessStartInfo(file, arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var process = Process.Start(info) ?? throw new BolumException($"{file} çalıştırılamadı.");
        var output = new System.Text.StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is null) return; output.AppendLine(e.Data); if (echo) Console.WriteLine("  " + e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is null) return; output.AppendLine(e.Data); if (echo) Console.WriteLine("  " + e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        return new ProcessResult(process.ExitCode, output.ToString());
    }

    /// <summary>npm Windows'ta npm.cmd'dir; cmd üzerinden çalıştırılır.</summary>
    public static ProcessResult Npm(string arguments, string workingDirectory, bool echo = false) =>
        OperatingSystem.IsWindows()
            ? Run("cmd.exe", $"/c npm {arguments}", workingDirectory, echo)
            : Run("npm", arguments, workingDirectory, echo);
}

public static class Output
{
    public static void Title(string text)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    public static void Pass(string text) => Line("  ✔ ", text, ConsoleColor.Green);
    public static void Fail(string text) => Line("  ✘ ", text, ConsoleColor.Red);
    public static void Info(string text) => Line("  · ", text, ConsoleColor.Gray);
    public static void Error(string text) => Line("Hata: ", text, ConsoleColor.Red);

    private static void Line(string prefix, string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write(prefix);
        Console.ResetColor();
        Console.WriteLine(text);
    }
}
