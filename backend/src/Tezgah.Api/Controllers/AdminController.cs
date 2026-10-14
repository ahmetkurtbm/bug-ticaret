using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tezgah.Application.Admin;
using Tezgah.Application.Common.Models;
using Tezgah.Application.Reporting;
using Tezgah.Domain.Catalog;
using Tezgah.Domain.Identity;
using Tezgah.Domain.Sellers;

namespace Tezgah.Api.Controllers;

[ApiController]
[Authorize(Roles = Role.Admin)]
[Route("api/admin")]
public class AdminController(IAdminService admin, IAdminOrderService orders, IReportingQueries reports, TimeProvider clock) : ControllerBase
{
    private static readonly TimeZoneInfo Istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    [HttpGet("sellers")]
    public async Task<ActionResult<PagedResult<AdminSellerDto>>> Sellers([FromQuery] SellerStatus? status, [FromQuery] string? q, [FromQuery] int page = 1, CancellationToken ct = default) =>
        Ok(await admin.ListSellersAsync(status, q, page, ct));

    [HttpPost("sellers/{id:long}/approve")]
    public async Task<ActionResult<AdminSellerDto>> ApproveSeller(long id, CancellationToken ct) => Ok(await admin.ApproveSellerAsync(id, ct));

    [HttpPost("sellers/{id:long}/suspend")]
    public async Task<ActionResult<AdminSellerDto>> SuspendSeller(long id, SellerDecisionRequest request, CancellationToken ct) =>
        Ok(await admin.SuspendSellerAsync(id, request, ct));

    [HttpPut("sellers/{id:long}/commission")]
    public async Task<ActionResult<AdminSellerDto>> Commission(long id, UpdateCommissionRequest request, CancellationToken ct) =>
        Ok(await admin.UpdateCommissionAsync(id, request, ct));

    [HttpGet("products")]
    public async Task<ActionResult<PagedResult<AdminProductDto>>> Products([FromQuery] ProductStatus? status = ProductStatus.PendingApproval, [FromQuery] string? q = null, [FromQuery] int page = 1, CancellationToken ct = default) =>
        Ok(await admin.ListProductsAsync(status, q, page, ct));

    [HttpPost("products/{id:long}/approve")]
    public async Task<IActionResult> ApproveProduct(long id, CancellationToken ct)
    {
        await admin.ApproveProductAsync(id, ct);
        return NoContent();
    }

    [HttpPost("products/{id:long}/reject")]
    public async Task<IActionResult> RejectProduct(long id, SellerDecisionRequest request, CancellationToken ct)
    {
        await admin.RejectProductAsync(id, request, ct);
        return NoContent();
    }

    [HttpGet("orders")]
    public async Task<ActionResult<PagedResult<AdminOrderListItemDto>>> Orders([FromQuery] AdminOrderQuery query, CancellationToken ct) =>
        Ok(await orders.ListAsync(query, ct));

    [HttpGet("orders/{orderNumber}")]
    public async Task<ActionResult<AdminOrderDetailDto>> Order(string orderNumber, CancellationToken ct) => Ok(await orders.GetAsync(orderNumber, ct));

    [HttpGet("coupons")]
    public async Task<ActionResult<IReadOnlyList<AdminCouponDto>>> Coupons(CancellationToken ct) => Ok(await admin.ListCouponsAsync(ct));

    [HttpPost("coupons")]
    public async Task<ActionResult<AdminCouponDto>> CreateCoupon(CreateCouponRequest request, CancellationToken ct) => Ok(await admin.CreateCouponAsync(request, ct));

    [HttpPost("coupons/{id:long}/activate")]
    public async Task<ActionResult<AdminCouponDto>> Activate(long id, CancellationToken ct) => Ok(await admin.SetCouponActiveAsync(id, true, ct));

    [HttpPost("coupons/{id:long}/deactivate")]
    public async Task<ActionResult<AdminCouponDto>> Deactivate(long id, CancellationToken ct) => Ok(await admin.SetCouponActiveAsync(id, false, ct));

    [HttpGet("campaign-history")]
    public async Task<ActionResult<IReadOnlyList<CampaignHistoryEntryDto>>> CampaignHistory([FromQuery] int take = 50, CancellationToken ct = default) =>
        Ok(await admin.CampaignHistoryAsync(take, ct));

    /// <summary>Satış raporu. Tarihler İstanbul takvim günü; varsayılan son 30 gün.</summary>
    [HttpGet("reports/sales")]
    public async Task<ActionResult<SalesReport>> Sales([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Istanbul).Date);
        var end = to ?? today;
        var start = from ?? end.AddDays(-29);
        if (start > end || end.DayNumber - start.DayNumber > 366)
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["from"] = ["Tarih aralığı en fazla 1 yıl olabilir ve başlangıç bitişten sonra olamaz."] }));
        return Ok(await reports.GetSalesReportAsync(start, end, ct));
    }
}
