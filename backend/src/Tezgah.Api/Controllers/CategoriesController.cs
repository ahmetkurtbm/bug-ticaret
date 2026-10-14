using Microsoft.AspNetCore.Mvc;
using Tezgah.Application.Catalog;

namespace Tezgah.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(ICatalogService catalog) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryNodeDto>>> GetTree(CancellationToken ct) =>
        Ok(await catalog.GetCategoryTreeAsync(ct));
}
