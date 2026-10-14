using Microsoft.EntityFrameworkCore;
using Tezgah.Application.Common.Exceptions;
using Tezgah.Application.Common.Interfaces;
using Tezgah.Application.Common.Text;
using Tezgah.Application.Common.Validation;
using Tezgah.Domain.Audit;
using Tezgah.Domain.Common;
using Tezgah.Domain.Sellers;

namespace Tezgah.Application.Sellers;

public interface ISellerApplicationService
{
    Task<SellerProfileDto> ApplyAsync(long userId, SellerApplicationRequest request, CancellationToken ct);
    Task<SellerProfileDto?> GetForUserAsync(long userId, CancellationToken ct);
}

/// <summary>Müşterinin satıcı olma başvurusu. Başvuru yönetim panelinden onaylanınca kullanıcıya satıcı rolü verilir.</summary>
public class SellerApplicationService(ITezgahDbContext db, TimeProvider clock) : ISellerApplicationService
{
    public const decimal DefaultCommissionRate = 12.50m;

    public async Task<SellerProfileDto> ApplyAsync(long userId, SellerApplicationRequest request, CancellationToken ct)
    {
        if (await db.Sellers.AnyAsync(s => s.OwnerUserId == userId, ct))
            throw new ConflictException("seller_exists", "Bu hesapla zaten bir satıcı başvurusu var.");

        var errors = new Dictionary<string, string[]>();
        if (!TurkishIdentifiers.IsValidIban(request.Iban))
            errors["Iban"] = ["IBAN geçersiz. TR ile başlayan 26 karakterli IBAN girin."];
        if (await db.Sellers.AnyAsync(s => s.TaxNumber == request.TaxNumber, ct))
            errors["TaxNumber"] = ["Bu vergi numarasıyla kayıtlı bir mağaza var."];
        if (errors.Count > 0)
            throw new RequestValidationException(errors);

        var storeName = request.StoreName.Trim();
        var baseSlug = SlugHelper.Slugify(storeName);
        var slug = baseSlug;
        for (var i = 2; await db.Sellers.AnyAsync(s => s.Slug == slug, ct); i++)
            slug = $"{baseSlug}-{i}";

        var now = clock.GetUtcNow();
        var seller = new Seller
        {
            OwnerUserId = userId,
            StoreName = storeName,
            Slug = slug,
            LegalName = request.LegalName.Trim(),
            TaxNumber = request.TaxNumber,
            TaxOffice = request.TaxOffice.Trim(),
            Iban = TurkishIdentifiers.NormalizeIban(request.Iban),
            City = request.City.Trim(),
            Status = SellerStatus.Pending,
            CommissionRate = DefaultCommissionRate,
        };
        db.Sellers.Add(seller);
        await db.SaveChangesAsync(ct);
        db.AuditLogs.Add(new AuditLog { UserId = userId, Action = "seller.applied", EntityType = "seller", EntityId = seller.Id, CreatedAt = now });
        await db.SaveChangesAsync(ct);
        return SellerMapping.ToProfile(seller);
    }

    public async Task<SellerProfileDto?> GetForUserAsync(long userId, CancellationToken ct)
    {
        var seller = await db.Sellers.AsNoTracking().SingleOrDefaultAsync(s => s.OwnerUserId == userId, ct);
        return seller is null ? null : SellerMapping.ToProfile(seller);
    }
}

internal static class SellerMapping
{
    public static SellerProfileDto ToProfile(Seller s) => new(s.Id, s.StoreName, s.Slug, s.LegalName, s.TaxNumber, s.TaxOffice,
        MaskIban(s.Iban), s.City, s.Status, s.CommissionRate, s.Rating, s.RatingCount, s.CreatedAt, s.ApprovedAt);

    /// <summary>IBAN'ın yalnızca son 4 hanesi gösterilir.</summary>
    public static string? MaskIban(string? iban) => iban is { Length: > 8 } ? $"{iban[..4]} **** **** **** **** {iban[^4..]}" : iban;
}
