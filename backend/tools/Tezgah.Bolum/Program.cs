// Bölüm aracı. Depo kökünden ya da herhangi bir alt klasörden:
//   dotnet run --project backend/tools/Tezgah.Bolum -- liste
//   dotnet run --project backend/tools/Tezgah.Bolum -- baslat 8
//   dotnet run --project backend/tools/Tezgah.Bolum -- ipucu 8
//   dotnet run --project backend/tools/Tezgah.Bolum -- kontrol      (bulunduğunuz dalın bölümü)
//   dotnet run --project backend/tools/Tezgah.Bolum -- kontrol 8
using System.Text;
using Tezgah.Bolum;

Console.OutputEncoding = Encoding.UTF8;
var command = args.FirstOrDefault()?.ToLowerInvariant();
var arg = args.Skip(1).FirstOrDefault();

try
{
    var repo = Repository.Locate();
    var chapters = ChapterCatalog.Load(repo);
    return command switch
    {
        "liste" or "list" => Commands.List(repo, chapters),
        "baslat" or "start" => Commands.Start(repo, chapters, arg),
        "ipucu" or "hint" => Commands.Hint(repo, chapters, arg, args.Skip(2).FirstOrDefault()),
        "kontrol" or "check" => await Commands.CheckAsync(repo, chapters, arg),
        "bitir" or "finish" => Commands.Finish(repo),
        _ => Commands.Help(),
    };
}
catch (BolumException ex)
{
    Output.Error(ex.Message);
    return 2;
}
