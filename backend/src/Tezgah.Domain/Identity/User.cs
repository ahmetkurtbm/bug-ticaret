using Tezgah.Domain.Common;

namespace Tezgah.Domain.Identity;

public class User : AuditableEntity
{
    public string Email { get; set; } = default!;
    public string NormalizedEmail { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? Phone { get; set; }
    public DateOnly? BirthDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }

    public List<UserRole> Roles { get; set; } = new();
    public List<Address> Addresses { get; set; } = new();

    public string FullName => $"{FirstName} {LastName}";
}
