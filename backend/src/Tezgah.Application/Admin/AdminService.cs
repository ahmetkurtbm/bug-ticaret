using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tezgah.Application.Common.Exceptions;
using Tezgah.Application.Common.Interfaces;
using Tezgah.Application.Common.Messaging;
using Tezgah.Application.Common.Models;
using Tezgah.Contracts.Events;
using Tezgah.Domain.Audit;
using Tezgah.Domain.Campaigns;
using Tezgah.Domain.Catalog;
using Tezgah.Domain.Common;
using Tezgah.Domain.Identity;
using Tezgah.Domain.Inventory;
using Tezgah.Domain.Sellers;

namespace Tezgah.Application.Admin;

public interface IAdminService
{
    Task<PagedResult<AdminSellerDto>> ListSellersAsync(SellerStatus? status, string? q, int page, CancellationToken ct);
    Task<AdminSellerDto> ApproveSellerAsync(long sellerId, CancellationToken ct);
    Task<AdminSellerDto> SuspendSellerAsync(long sellerId, SellerDecisionRequest request, CancellationToken ct);
    Task<AdminSellerDto> UpdateCommissionAsync(long sellerId, UpdateCommissionRequest request, CancellationToken ct);
    Task<PagedResult<AdminProductDto>> ListProductsAsync(ProductStatus? status, string? q, int page, CancellationToken ct);
    Task ApproveProductAsync(long productId, CancellationToken ct);
    Task RejectProductAsync(long productId, SellerDecisionRequest request, CancellationToken ct);
    Task<IReadOnlyList<AdminCouponDto>> ListCouponsAsync(CancellationToken ct);
    Task<AdminCouponDto> CreateCouponAsync(CreateCouponRequest request, CancellationToken ct);
    Task<AdminCouponDto> SetCouponActiveAsync(long couponId, bool isActive, CancellationToken ct);
    Task<IReadOnlyList<CampaignHistoryEntryDto>> CampaignHistoryAsync(int take, CancellationToken ct);
}

public class AdminService(ITezgahDbContext db, ICurrentUser currentUser, TimeProvider clock) : IAdminService
{
    private const int PageSize = 20;
    private static readonly TimeZoneInfo Istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    // ---------------- satıcılar
    public async Task<PagedResult<AdminSellerDto>> ListSellersAsync(SellerStatus? status, string? q, int page, CancellationToken ct)
    {
        page = Math.Max(1, page);
        var query = db.Sellers.AsNoTracking();
        if (status is not null) query = query.Where(s => s.Status == status);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(s => s.StoreName.ToLower().Contains(term) || s.TaxNumber == term);
        }
        var total = await query.CountAsync(ct);
        var items = await SellerProjection(query.OrderBy(s => s.Status).ThenByDescending(s => s.CreatedAt).Skip((page - 1) * PageSize).Take(PageSize)).ToListAsync(ct);
        return new PagedResult<AdminSellerDto>(items, page, PageSize, total);
    }

    public async Task<AdminSellerDto> ApproveSellerAsync(long sellerId, CancellationToken ct)
    {
        var seller = await db.Sellers.SingleOrDefaultAsync(s => s.Id == sellerId, ct) ?? throw new NotFoundException("Satıcı bulunamadı.");
        if (seller.Status == SellerStatus.Active) return await GetSellerAsync(sellerId, ct);
        if (seller.Status is not (SellerStatus.Pending or SellerStatus.Suspended))
            throw new DomainException("seller_closed", "Kapatılmış mağaza yeniden açılamaz.");

        var now = clock.GetUtcNow();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        seller.Status = SellerStatus.Active;
        seller.ApprovedAt ??= now;

        var sellerRoleId = await db.Roles.Where(r => r.Name == Role.Seller).Select(r => r.Id).SingleAsync(ct);
        if (!await db.UserRoles.AnyAsync(ur => ur.UserId == seller.OwnerUserId && ur.RoleId == sellerRoleId, ct))
            db.UserRoles.Add(new UserRole { UserId = seller.OwnerUserId, RoleId = sellerRoleId });
        if (!await db.Warehouses.AnyAsync(w => w.SellerId == seller.Id, ct))
            db.Warehouses.Add(new Warehouse { SellerId = seller.Id, Name = "Ana depo", City = seller.City });

        Audit("seller.approved", "seller", seller.Id, null, now);
        var owner = await db.Users.AsNoTracking().Where(u => u.Id == seller.OwnerUserId)
            .Select(u => new { u.Email, Name = u.FirstName + " " + u.LastName }).SingleAsync(ct);
        var approved = new SellerApprovedEvent(Guid.CreateVersion7(), seller.Id, seller.StoreName, owner.Email, owner.Name, now);
        Outbox.Add(db, approved.EventId, EventTopology.SellerApproved, approved, now);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await GetSellerAsync(sellerId, ct);
    }

    public async Task<AdminSellerDto> SuspendSellerAsync(long sellerId, SellerDecisionRequest request, CancellationToken ct)
    {
        var seller = await db.Sellers.SingleOrDefaultAsync(s => s.Id == sellerId, ct) ?? throw new NotFoundException("Satıcı bulunamadı.");
        // Bekleyen başvurunun reddi mağazayı kapatır; aktif mağaza askıya alınır. Diğer durumlarda geçiş yok
        // (kapatılmış mağaza önce askıya alınıp sonra onaylanarak yeniden açılamamalı).
        seller.Status = seller.Status switch
        {
            SellerStatus.Pending => SellerStatus.Closed,
            SellerStatus.Active => SellerStatus.Suspended,
            _ => throw new DomainException("invalid_status", "Yalnızca bekleyen başvuru reddedilebilir ya da aktif mağaza askıya alınabilir."),
        };
        Audit(seller.Status == SellerStatus.Closed ? "seller.rejected" : "seller.suspended", "seller", seller.Id, new { note = request.Note }, clock.GetUtcNow());
        await db.SaveChangesAsync(ct);
        return await GetSellerAsync(sellerId, ct);
    }

    public async Task<AdminSellerDto> UpdateCommissionAsync(long sellerId, UpdateCommissionRequest request, CancellationToken ct)
    {
        var seller = await db.Sellers.SingleOrDefaultAsync(s => s.Id == sellerId, ct) ?? throw new NotFoundException("Satıcı bulunamadı.");
        Audit("seller.commission_changed", "seller", seller.Id, new { from = seller.CommissionRate, to = request.CommissionRate }, clock.GetUtcNow());
        seller.CommissionRate = request.CommissionRate; // yeni siparişlere uygulanır; eski siparişte oran kalem üzerinde saklı
        await db.SaveChangesAsync(ct);
        return await GetSellerAsync(sellerId, ct);
    }

    private async Task<AdminSellerDto> GetSellerAsync(long id, CancellationToken ct) =>
        await SellerProjection(db.Sellers.AsNoTracking().Where(s => s.Id == id)).SingleAsync(ct);

    private IQueryable<AdminSellerDto> SellerProjection(IQueryable<Seller> source) =>
        source.Select(s => new AdminSellerDto(
            s.Id, s.StoreName, s.Slug, s.LegalName, s.TaxNumber, s.City, s.Status, s.CommissionRate,
            db.Users.Where(u => u.Id == s.OwnerUserId).Select(u => u.Email).FirstOrDefault() ?? "",
            db.Users.Where(u => u.Id == s.OwnerUserId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault() ?? "",
            db.Products.Count(p => p.SellerId == s.Id),
            s.CreatedAt, s.ApprovedAt));

    // ---------------- ürün onayı
    public async Task<PagedResult<AdminProductDto>> ListProductsAsync(ProductStatus? status, string? q, int page, CancellationToken ct)
    {
        page = Math.Max(1, page);
        var query = db.Products.AsNoTracking();
        if (status is not null) query = query.Where(p => p.Status == status);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(term));
        }
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(p => p.UpdatedAt ?? p.CreatedAt).ThenBy(p => p.Id)
            .Skip((page - 1) * PageSize).Take(PageSize)
            .Select(p => new AdminProductDto(p.Id, p.Name, p.Slug, p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                p.Seller.StoreName, p.Category.Name, p.Status, p.Variants.Count, p.Variants.Min(v => (decimal?)v.Price) ?? 0, p.CreatedAt, p.UpdatedAt))
            .ToListAsync(ct);
        return new PagedResult<AdminProductDto>(items, page, PageSize, total);
    }

    public async Task ApproveProductAsync(long productId, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == productId, ct) ?? throw new NotFoundException("Ürün bulunamadı.");
        if (product.Status != ProductStatus.PendingApproval)
            throw new DomainException("invalid_status", "Yalnızca onay bekleyen ürün onaylanabilir.");
        product.Status = ProductStatus.Active;
        Audit("product.approved", "product", product.Id, null, clock.GetUtcNow());
        await db.SaveChangesAsync(ct);
    }

    public async Task RejectProductAsync(long productId, SellerDecisionRequest request, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == productId, ct) ?? throw new NotFoundException("Ürün bulunamadı.");
        if (product.Status != ProductStatus.PendingApproval)
            throw new DomainException("invalid_status", "Yalnızca onay bekleyen ürün reddedilebilir.");
        product.Status = ProductStatus.Rejected;
        Audit("product.rejected", "product", product.Id, new { note = request.Note }, clock.GetUtcNow());
        await db.SaveChangesAsync(ct);
    }

    // ---------------- kuponlar ve kampanya geçmişi
    public async Task<IReadOnlyList<AdminCouponDto>> ListCouponsAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var coupons = await db.Coupons.AsNoTracking()
            .OrderByDescending(c => c.StartsAt).ThenBy(c => c.Code)
            .Select(c => new { c, SellerName = db.Sellers.Where(s => s.Id == c.SellerId).Select(s => s.StoreName).FirstOrDefault() })
            .ToListAsync(ct);
        return coupons.Select(x => ToDto(x.c, x.SellerName, now)).ToList();
    }

    public async Task<AdminCouponDto> CreateCouponAsync(CreateCouponRequest request, CancellationToken ct)
    {
        var startsOn = request.StartsOn!.Value;
        var endsOn = request.EndsOn!.Value;
        if (endsOn < startsOn)
            throw RequestValidationException.For("EndsOn", "Bitiş günü başlangıçtan önce olamaz.");
        if (startsOn.Year < 2020 || endsOn.DayNumber - startsOn.DayNumber > 366 * 2)
            throw RequestValidationException.For("EndsOn", "Kampanya en fazla 2 yıl sürebilir.");
        if (request.Type == CouponType.Percentage && request.Value > 90)
            throw RequestValidationException.For("Value", "Yüzde indirim en fazla %90 olabilir.");
        if (await db.Coupons.AnyAsync(c => c.Code == request.Code, ct))
            throw RequestValidationException.For("Code", "Bu kod zaten kullanılıyor.");
        string? sellerName = null;
        if (request.SellerId is { } sid)
            sellerName = await db.Sellers.Where(s => s.Id == sid).Select(s => s.StoreName).SingleOrDefaultAsync(ct)
                ?? throw RequestValidationException.For("SellerId", "Satıcı bulunamadı.");

        var coupon = new Coupon
        {
            Code = request.Code,
            Description = request.Description.Trim(),
            Type = request.Type,
            Value = request.Value,
            MaxDiscountAmount = request.MaxDiscountAmount,
            MinOrderAmount = request.MinOrderAmount,
            MaxUses = request.MaxUses,
            MaxUsesPerUser = request.MaxUsesPerUser,
            SellerId = request.SellerId,
            // Kampanya günleri İstanbul takvimiyle; bitiş günü dahil (ertesi gün 00:00'a kadar).
            StartsAt = StartOfDay(startsOn),
            EndsAt = StartOfDay(endsOn.AddDays(1)),
            IsActive = true,
        };
        db.Coupons.Add(coupon);
        await db.SaveChangesAsync(ct);
        Audit("coupon.created", "coupon", coupon.Id, new { coupon.Code, coupon.Type, coupon.Value, starts = startsOn, ends = endsOn }, clock.GetUtcNow());
        await db.SaveChangesAsync(ct);
        return ToDto(coupon, sellerName, clock.GetUtcNow());
    }

    public async Task<AdminCouponDto> SetCouponActiveAsync(long couponId, bool isActive, CancellationToken ct)
    {
        var coupon = await db.Coupons.SingleOrDefaultAsync(c => c.Id == couponId, ct) ?? throw new NotFoundException("Kupon bulunamadı.");
        coupon.IsActive = isActive;
        Audit(isActive ? "coupon.activated" : "coupon.deactivated", "coupon", coupon.Id, new { coupon.Code }, clock.GetUtcNow());
        await db.SaveChangesAsync(ct);
        var sellerName = await db.Sellers.Where(s => s.Id == coupon.SellerId).Select(s => s.StoreName).FirstOrDefaultAsync(ct);
        return ToDto(coupon, sellerName, clock.GetUtcNow());
    }

    public async Task<IReadOnlyList<CampaignHistoryEntryDto>> CampaignHistoryAsync(int take, CancellationToken ct) =>
        await db.AuditLogs.AsNoTracking()
            .Where(a => a.EntityType == "coupon")
            .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
            .Take(Math.Clamp(take, 1, 200))
            .Select(a => new CampaignHistoryEntryDto(a.CreatedAt, a.Action,
                db.Coupons.Where(c => c.Id == a.EntityId).Select(c => c.Code).FirstOrDefault(),
                db.Users.Where(u => u.Id == a.UserId).Select(u => u.Email).FirstOrDefault(),
                a.Data))
            .ToListAsync(ct);

    private static DateTimeOffset StartOfDay(DateOnly day)
    {
        var local = day.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, Istanbul.GetUtcOffset(local)).ToUniversalTime();
    }

    private static AdminCouponDto ToDto(Coupon c, string? sellerName, DateTimeOffset now) => new(
        c.Id, c.Code, c.Description, c.Type, c.Value, c.MaxDiscountAmount, c.MinOrderAmount, c.MaxUses, c.MaxUsesPerUser, c.UsedCount, sellerName,
        c.StartsAt, c.EndsAt, c.IsActive,
        !c.IsActive ? "inactive" : now < c.StartsAt ? "scheduled" : now >= c.EndsAt ? "expired" : c.MaxUses is { } m && c.UsedCount >= m ? "exhausted" : "running");

    private void Audit(string action, string entityType, long entityId, object? data, DateTimeOffset now) =>
        db.AuditLogs.Add(new AuditLog
        {
            UserId = currentUser.UserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Data = data is null ? null : JsonSerializer.Serialize(data, JsonSerializerOptions.Web),
            CreatedAt = now,
        });
}
