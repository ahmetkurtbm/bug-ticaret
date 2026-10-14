using Tezgah.Application.Common.Interfaces;

namespace Tezgah.Api.Infrastructure;

public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private System.Security.Claims.ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public long? UserId => long.TryParse(Principal?.FindFirst("sub")?.Value, out var id) ? id : null;

    public long? SellerId => long.TryParse(Principal?.FindFirst("seller_id")?.Value, out var id) ? id : null;

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
