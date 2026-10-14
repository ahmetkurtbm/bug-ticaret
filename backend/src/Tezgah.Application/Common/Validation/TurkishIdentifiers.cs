using System.Numerics;

namespace Tezgah.Application.Common.Validation;

public static class TurkishIdentifiers
{
    /// <summary>TR IBAN: "TR" + 24 hane, ISO 13616 mod 97 kontrolü. Boşluklar yok sayılır.</summary>
    public static bool IsValidIban(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban)) return false;
        var s = iban.Replace(" ", "").ToUpperInvariant();
        if (s.Length != 26 || !s.StartsWith("TR", StringComparison.Ordinal) || !s[2..].All(char.IsAsciiDigit)) return false;
        var rearranged = s[4..] + "2927" + s[2..4]; // T=29, R=27
        return BigInteger.Parse(rearranged) % 97 == 1;
    }

    public static string NormalizeIban(string iban) => iban.Replace(" ", "").ToUpperInvariant();
}
