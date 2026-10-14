using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tezgah.Application.Common.Interfaces;
using Tezgah.Application.Sellers;

namespace Tezgah.Api.Controllers;

/// <summary>Müşterinin satıcı başvurusu. Giriş yapmış her kullanıcı başvurabilir.</summary>
[ApiController]
[Authorize]
[Route("api/seller-applications")]
public class SellerApplicationController(ISellerApplicationService applications, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SellerProfileDto>> Apply(SellerApplicationRequest request, CancellationToken ct) =>
        Ok(await applications.ApplyAsync(currentUser.UserId!.Value, request, ct));

    [HttpGet("mine")]
    public async Task<ActionResult<SellerProfileDto>> Mine(CancellationToken ct) =>
        await applications.GetForUserAsync(currentUser.UserId!.Value, ct) is { } profile ? Ok(profile) : NoContent();
}
