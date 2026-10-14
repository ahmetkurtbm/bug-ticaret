namespace Tezgah.Application.Orders;

public static class OrderNumber
{
    /// <summary>TZ-2026-0050123 biçimi: yıl + 7 haneli sipariş kimliği.</summary>
    public static string For(long orderId, DateTimeOffset placedAt) => $"TZ-{placedAt.Year}-{orderId:D7}";

    /// <summary>Kimlik henüz belli değilken kullanılan geçici numara (benzersiz, 20 karakter).</summary>
    public static string Temporary() => "TMP" + Guid.NewGuid().ToString("N")[..17].ToUpperInvariant();
}
