using Microsoft.AspNetCore.Mvc;
using Tezgah.Application.Catalog;

namespace Tezgah.Api.Controllers;

[ApiController]
[Route("api/sellers")]
public class SellersController(ICatalogService catalog) : ControllerBase
{
    [HttpGet("{slug}")]
    public async Task<ActionResult<SellerPublicDto>> GetBySlug(string slug, CancellationToken ct) =>
        Ok(await catalog.GetSellerAsync(slug, ct));
}
