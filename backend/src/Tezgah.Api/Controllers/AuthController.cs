using Microsoft.AspNetCore.Mvc;
using Tezgah.Application.Auth;
using Tezgah.Application.Carts;

namespace Tezgah.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth, ICartService carts) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var response = await auth.RegisterAsync(request, ct);
        await MergeGuestCartAsync(response.User.Id, ct);
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var response = await auth.LoginAsync(request, ct);
        await MergeGuestCartAsync(response.User.Id, ct);
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken ct) =>
        Ok(await auth.RefreshAsync(request.RefreshToken, ct));

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken ct)
    {
        await auth.LogoutAsync(request.RefreshToken, ct);
        return NoContent();
    }

    private async Task MergeGuestCartAsync(long userId, CancellationToken ct)
    {
        if (Guid.TryParse(Request.Headers[CartController.CartIdHeader], out var anonymousId))
            await carts.MergeAsync(anonymousId, userId, ct);
    }
}
