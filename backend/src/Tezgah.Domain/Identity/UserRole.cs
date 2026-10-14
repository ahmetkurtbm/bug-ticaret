namespace Tezgah.Domain.Identity;

public class UserRole
{
    public long UserId { get; set; }
    public User User { get; set; } = default!;
    public long RoleId { get; set; }
    public Role Role { get; set; } = default!;
}
