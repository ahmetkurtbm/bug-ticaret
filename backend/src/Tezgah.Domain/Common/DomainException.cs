namespace Tezgah.Domain.Common;

/// <summary>İş kuralı ihlali. API katmanı bunu 400/409 gibi anlamlı yanıtlara çevirir.</summary>
public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}
