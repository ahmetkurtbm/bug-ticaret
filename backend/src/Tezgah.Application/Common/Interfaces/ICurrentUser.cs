namespace Tezgah.Application.Common.Interfaces;

/// <summary>İsteği yapan kullanıcı. API katmanı JWT claim'lerinden doldurur.</summary>
public interface ICurrentUser
{
    long? UserId { get; }
    long? SellerId { get; }
    bool IsInRole(string role);
}
