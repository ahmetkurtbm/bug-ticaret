using Tezgah.Domain.Common;

namespace Tezgah.Domain.Identity;

public class Address : AuditableEntity
{
    public long UserId { get; set; }
    public string Title { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string? Phone { get; set; }
    public string City { get; set; } = default!;
    public string District { get; set; } = default!;
    public string Line { get; set; } = default!;
    public string? PostalCode { get; set; }
    public bool IsDefault { get; set; }
}
