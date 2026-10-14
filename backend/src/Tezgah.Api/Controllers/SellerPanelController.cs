using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tezgah.Application.Common.Models;
using Tezgah.Application.Sellers;
using Tezgah.Domain.Catalog;
using Tezgah.Domain.Identity;

namespace Tezgah.Api.Controllers;

/// <summary>Satıcı paneli uçları. Rol yoksa 403, mağaza aktif değilse 403 (sebep mesajda).</summary>
[ApiController]
[Authorize(Roles = Role.Seller)]
[Route("api/seller")]
public class SellerPanelController(ISellerCatalogService catalog, ISellerOrderService orders, ISellerApplicationService applications,
    Tezgah.Application.Common.Interfaces.ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<SellerProfileDto>> Me(CancellationToken ct) =>
        await applications.GetForUserAsync(currentUser.UserId!.Value, ct) is { } p ? Ok(p) : NotFound();

    [HttpGet("dashboard")]
    public async Task<ActionResult<SellerDashboardDto>> Dashboard(CancellationToken ct) => Ok(await orders.GetDashboardAsync(ct));

    [HttpGet("products")]
    public async Task<ActionResult<PagedResult<SellerProductListItemDto>>> Products([FromQuery] string? q, [FromQuery] ProductStatus? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await catalog.ListAsync(q, status, page, pageSize, ct));

    [HttpGet("products/{id:long}")]
    public async Task<ActionResult<SellerProductDetailDto>> Product(long id, CancellationToken ct) => Ok(await catalog.GetAsync(id, ct));

    [HttpPost("products")]
    public async Task<ActionResult<SellerProductDetailDto>> Create(CreateProductRequest request, CancellationToken ct)
    {
        var created = await catalog.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Product), new { id = created.Id }, created);
    }

    [HttpPut("products/{id:long}")]
    public async Task<ActionResult<SellerProductDetailDto>> Update(long id, UpdateProductRequest request, CancellationToken ct) =>
        Ok(await catalog.UpdateAsync(id, request, ct));

    [HttpPost("products/{id:long}/submit")]
    public async Task<ActionResult<SellerProductDetailDto>> Submit(long id, CancellationToken ct) => Ok(await catalog.SubmitAsync(id, ct));

    [HttpPost("products/{id:long}/archive")]
    public async Task<ActionResult<SellerProductDetailDto>> Archive(long id, CancellationToken ct) => Ok(await catalog.ArchiveAsync(id, ct));

    [HttpPut("variants/{id:long}")]
    public async Task<ActionResult<SellerVariantDto>> UpdateVariant(long id, UpdateVariantRequest request, CancellationToken ct) =>
        Ok(await catalog.UpdateVariantAsync(id, request, ct));

    [HttpPut("variants/{id:long}/stock")]
    public async Task<ActionResult<SellerVariantDto>> SetStock(long id, SetStockRequest request, CancellationToken ct) =>
        Ok(await catalog.SetStockAsync(id, request, ct));

    [HttpGet("orders")]
    public async Task<ActionResult<PagedResult<SellerOrderDto>>> Orders([FromQuery] string filter = SellerOrderFilter.ToShip,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await orders.ListAsync(filter, page, pageSize, ct));

    [HttpPost("orders/{orderNumber}/ship")]
    public async Task<ActionResult<SellerOrderDto>> Ship(string orderNumber, ShipOrderRequest request, CancellationToken ct) =>
        Ok(await orders.ShipAsync(orderNumber, request, ct));
}
