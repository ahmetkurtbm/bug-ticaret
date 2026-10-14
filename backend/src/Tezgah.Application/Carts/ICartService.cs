namespace Tezgah.Application.Carts;

public interface ICartService
{
    Task<CartDto> GetAsync(CartOwner owner, CancellationToken ct);
    Task<CartDto> AddItemAsync(CartOwner owner, AddCartItemRequest request, CancellationToken ct);
    Task<CartDto> UpdateItemAsync(CartOwner owner, long itemId, int quantity, CancellationToken ct);
    Task<CartDto> RemoveItemAsync(CartOwner owner, long itemId, CancellationToken ct);
    Task<CartDto> ApplyCouponAsync(CartOwner owner, string couponCode, CancellationToken ct);
    Task<CartDto> RemoveCouponAsync(CartOwner owner, CancellationToken ct);

    /// <summary>Girişten sonra ziyaretçi sepetini kullanıcının sepetine taşır.</summary>
    Task MergeAsync(Guid anonymousId, long userId, CancellationToken ct);
}
