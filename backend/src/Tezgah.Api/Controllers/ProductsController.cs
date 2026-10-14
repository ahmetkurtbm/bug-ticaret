using Microsoft.AspNetCore.Mvc;
using Tezgah.Application.Catalog;
using Tezgah.Application.Common.Models;

namespace Tezgah.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(ICatalogService catalog) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductCardDto>>> Search([FromQuery] ProductSearchQuery query, CancellationToken ct) =>
        Ok(await catalog.SearchAsync(query, ct));

    [HttpGet("{slug}")]
    public async Task<ActionResult<ProductDetailDto>> GetBySlug(string slug, CancellationToken ct) =>
        Ok(await catalog.GetBySlugAsync(slug, ct));
}
