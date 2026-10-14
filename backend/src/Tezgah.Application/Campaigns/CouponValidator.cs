using Microsoft.EntityFrameworkCore;
using Tezgah.Application.Common.Interfaces;
using Tezgah.Domain.Campaigns;
using Tezgah.Domain.Common;

namespace Tezgah.Application.Campaigns;

/// <summary>Kuponun şu an ve bu kullanıcı için geçerli olup olmadığını kontrol eder. Sepet ve sipariş aynı kuralları kullanır.</summary>
public class CouponValidator(ITezgahDbContext db, TimeProvider clock)
{
    public async Task<Coupon> ValidateAsync(string code, long? userId, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var normalized = code.Trim().ToUpperInvariant();
        var coupon = await db.Coupons.AsNoTracking().SingleOrDefaultAsync(c => c.Code == normalized, ct);

        if (coupon is null || !coupon.IsActive)
            throw new DomainException("coupon_not_found", "Kupon kodu geçersiz.");
        if (now < coupon.StartsAt)
            throw new DomainException("coupon_not_started", "Bu kupon henüz kullanıma açılmadı.");
        if (now >= coupon.EndsAt)
            throw new DomainException("coupon_expired", "Bu kuponun süresi doldu.");
        if (coupon.MaxUses is { } maxUses && coupon.UsedCount >= maxUses)
            throw new DomainException("coupon_exhausted", "Bu kuponun kullanım limiti doldu.");

        if (coupon.MaxUsesPerUser is { } perUser)
        {
            if (userId is null)
                throw new DomainException("coupon_requires_login", "Bu kuponu kullanmak için giriş yapın.");
            var used = await db.CouponUsages.CountAsync(u => u.CouponId == coupon.Id && u.UserId == userId, ct);
            if (used >= perUser)
                throw new DomainException("coupon_already_used", "Bu kuponu daha önce kullandınız.");
        }
        return coupon;
    }
}
