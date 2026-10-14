using Tezgah.Application.Common.Models;

namespace Tezgah.Application.Catalog;

public interface ICatalogService
{
    Task<IReadOnlyList<CategoryNodeDto>> GetCategoryTreeAsync(CancellationToken ct);
    Task<PagedResult<ProductCardDto>> SearchAsync(ProductSearchQuery query, CancellationToken ct);
    Task<ProductDetailDto> GetBySlugAsync(string slug, CancellationToken ct);
}
