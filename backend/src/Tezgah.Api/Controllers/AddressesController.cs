using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tezgah.Application.Addresses;
using Tezgah.Application.Common.Interfaces;

namespace Tezgah.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me/addresses")]
public class AddressesController(IAddressService addresses, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AddressDto>>> List(CancellationToken ct) =>
        Ok(await addresses.ListAsync(currentUser.UserId!.Value, ct));

    [HttpPost]
    public async Task<ActionResult<AddressDto>> Create(CreateAddressRequest request, CancellationToken ct) =>
        Ok(await addresses.CreateAsync(currentUser.UserId!.Value, request, ct));
}
