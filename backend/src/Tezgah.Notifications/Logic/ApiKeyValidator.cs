using System.Security.Cryptography;
using System.Text;

namespace Tezgah.Notifications.Logic;

public static class ApiKeyValidator
{
    /// <summary>Sabit zamanlı karşılaştırma; uzunluk sızmasın diye iki taraf da SHA-256 ile aynı boya getirilir.</summary>
    public static bool IsValid(string? provided, string? expected)
    {
        if (string.IsNullOrEmpty(provided) || string.IsNullOrEmpty(expected))
            return false;
        var a = SHA256.HashData(Encoding.UTF8.GetBytes(provided));
        var b = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
