using Microsoft.AspNetCore.Mvc;
using Tezgah.Application.Carts;
using Tezgah.Application.Common.Interfaces;

namespace Tezgah.Api.Controllers;

/// <summary>
/// Giriş yapmış kullanıcının sepeti token'dan, ziyaretçinin sepeti <c>X-Cart-Id</c> başlığındaki
/// GUID'den bulunur.
/// </summary>
[ApiController]
[Route("api/cart")]
public class CartController(ICartService carts, ICurrentUser currentUser) : ControllerBase
{
    public const string CartIdHeader = "X-Cart-Id";

    [HttpGet]
    public async Task<ActionResult<CartDto>> Get(CancellationToken ct) => Ok(await carts.GetAsync(Owner(), ct));

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem(AddCartItemRequest request, CancellationToken ct) =>
        Ok(await carts.AddItemAsync(Owner(), request, ct));

    [HttpPut("items/{itemId:long}")]
    public async Task<ActionResult<CartDto>> UpdateItem(long itemId, UpdateCartItemRequest request, CancellationToken ct) =>
        Ok(await carts.UpdateItemAsync(Owner(), itemId, request.Quantity, ct));

    [HttpDelete("items/{itemId:long}")]
    public async Task<ActionResult<CartDto>> RemoveItem(long itemId, CancellationToken ct) =>
        Ok(await carts.RemoveItemAsync(Owner(), itemId, ct));

    [HttpPut("coupon")]
    public async Task<ActionResult<CartDto>> ApplyCoupon(ApplyCouponRequest request, CancellationToken ct) =>
        Ok(await carts.ApplyCouponAsync(Owner(), request.CouponCode, ct));

    [HttpDelete("coupon")]
    public async Task<ActionResult<CartDto>> RemoveCoupon(CancellationToken ct) =>
        Ok(await carts.RemoveCouponAsync(Owner(), ct));

    private CartOwner Owner()
    {
        Guid? anonymousId = Guid.TryParse(Request.Headers[CartIdHeader], out var id) ? id : null;
        return new CartOwner(currentUser.UserId, anonymousId);
    }
}
