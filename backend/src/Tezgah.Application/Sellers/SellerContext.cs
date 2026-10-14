using Microsoft.EntityFrameworkCore;
using Tezgah.Application.Common.Exceptions;
using Tezgah.Application.Common.Interfaces;
using Tezgah.Domain.Sellers;

namespace Tezgah.Application.Sellers;

/// <summary>Satıcı panelindeki isteğin hangi mağaza adına yapıldığı. Her satıcı sorgusu bu kimlikle süzülür.</summary>
public class SellerContext(ICurrentUser currentUser, ITezgahDbContext db)
{
    private long? _activeSellerId;

    public async Task<long> GetActiveSellerIdAsync(CancellationToken ct)
    {
        if (_activeSellerId is { } cached) return cached;
        var sellerId = currentUser.SellerId ?? throw new ForbiddenException("Bu işlem için satıcı hesabı gerekiyor.");
        var status = await db.Sellers.AsNoTracking().Where(s => s.Id == sellerId).Select(s => (SellerStatus?)s.Status).SingleOrDefaultAsync(ct);
        if (status != SellerStatus.Active)
            throw new ForbiddenException("Mağazanız aktif değil. Yönetici onayı bekleniyor ya da mağaza askıya alındı.");
        _activeSellerId = sellerId;
        return sellerId;
    }
}
