using Tezgah.Domain.Common;

namespace Tezgah.Domain.Identity;

public class Role : Entity
{
    public const string Customer = "customer";
    public const string Seller = "seller";
    public const string Admin = "admin";

    public string Name { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
}
