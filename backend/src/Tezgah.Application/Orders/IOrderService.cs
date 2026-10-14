using Tezgah.Application.Common.Models;

namespace Tezgah.Application.Orders;

public interface IOrderService
{
    Task<PlaceOrderResponse> PlaceOrderAsync(long userId, PlaceOrderRequest request, string? idempotencyKey, CancellationToken ct);
    Task<PlaceOrderResponse> RetryPaymentAsync(long userId, string orderNumber, CancellationToken ct);
    Task<PagedResult<OrderSummaryDto>> ListAsync(long userId, int page, int pageSize, CancellationToken ct);
    Task<OrderDetailDto> GetAsync(long userId, string orderNumber, CancellationToken ct);
}
