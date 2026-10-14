using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tezgah.Application.Common.Interfaces;
using Tezgah.Application.Common.Models;
using Tezgah.Application.Orders;

namespace Tezgah.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public class OrdersController(IOrderService orders, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Sepetten sipariş oluşturur. <c>Idempotency-Key</c> başlığı aynı isteğin iki kez sipariş açmasını önler.</summary>
    [HttpPost]
    public async Task<ActionResult<PlaceOrderResponse>> Place(
        PlaceOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        if (idempotencyKey is { Length: > 64 })
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["Idempotency-Key"] = ["En fazla 64 karakter olabilir."] }));
        return Ok(await orders.PlaceOrderAsync(currentUser.UserId!.Value, request, idempotencyKey, ct));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default) =>
        Ok(await orders.ListAsync(currentUser.UserId!.Value, page, pageSize, ct));

    [HttpGet("{orderNumber}")]
    public async Task<ActionResult<OrderDetailDto>> Get(string orderNumber, CancellationToken ct) =>
        Ok(await orders.GetAsync(currentUser.UserId!.Value, orderNumber, ct));

    [HttpPost("{orderNumber}/payment")]
    public async Task<ActionResult<PlaceOrderResponse>> RetryPayment(string orderNumber, CancellationToken ct) =>
        Ok(await orders.RetryPaymentAsync(currentUser.UserId!.Value, orderNumber, ct));
}
