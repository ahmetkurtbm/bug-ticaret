using System.Globalization;
using System.Text;

namespace Tezgah.Application.Common.Text;

public static class SlugHelper
{
    private static readonly Dictionary<char, string> TurkishMap = new()
    {
        ['ç'] = "c", ['ğ'] = "g", ['ı'] = "i", ['ö'] = "o", ['ş'] = "s", ['ü'] = "u",
        ['Ç'] = "c", ['Ğ'] = "g", ['İ'] = "i", ['Ö'] = "o", ['Ş'] = "s", ['Ü'] = "u",
    };

    /// <summary>"Erkek Çocuk Ayakkabı" → "erkek-cocuk-ayakkabi". Kültürden bağımsız çalışır.</summary>
    public static string Slugify(string value)
    {
        var sb = new StringBuilder(value.Length);
        var lastDash = true;
        foreach (var ch in value)
        {
            if (TurkishMap.TryGetValue(ch, out var mapped))
            {
                sb.Append(mapped);
                lastDash = false;
            }
            else if (char.IsLetterOrDigit(ch) && ch < 128)
            {
                sb.Append(char.ToLowerInvariant(ch));
                lastDash = false;
            }
            else if (!lastDash)
            {
                sb.Append('-');
                lastDash = true;
            }
        }
        return sb.ToString().Trim('-');
    }

    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}
