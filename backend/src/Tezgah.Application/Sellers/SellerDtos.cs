using System.ComponentModel.DataAnnotations;
using Tezgah.Domain.Catalog;
using Tezgah.Domain.Orders;
using Tezgah.Domain.Sellers;

namespace Tezgah.Application.Sellers;

public class SellerApplicationRequest
{
    [Required, MinLength(3), MaxLength(100)]
    public string StoreName { get; set; } = default!;

    [Required, MaxLength(200)]
    public string LegalName { get; set; } = default!;

    [Required, RegularExpression(@"^\d{10,11}$", ErrorMessage = "Vergi numarası 10, T.C. kimlik numarası 11 haneli olmalı.")]
    public string TaxNumber { get; set; } = default!;

    [Required, MaxLength(100)]
    public string TaxOffice { get; set; } = default!;

    [Required, MaxLength(34)]
    public string Iban { get; set; } = default!;

    [Required, MaxLength(60)]
    public string City { get; set; } = default!;
}

public record SellerProfileDto(long Id, string StoreName, string Slug, string LegalName, string TaxNumber, string TaxOffice, string? Iban,
    string City, SellerStatus Status, decimal CommissionRate, decimal Rating, int RatingCount, DateTimeOffset CreatedAt, DateTimeOffset? ApprovedAt);

public record SellerDashboardDto(
    int OrdersToShip,
    int LowStockVariants,
    int ActiveProducts,
    int PendingApprovalProducts,
    decimal Last30DaysSales,
    decimal Last30DaysCommission,
    decimal Last30DaysNet,
    IReadOnlyList<DailySalesPoint> DailySales);

public record DailySalesPoint(DateOnly Day, decimal Sales, int Orders);

// ---- ürünler
public record SellerProductListItemDto(long Id, string Name, string Slug, string? ImageUrl, string CategoryName, ProductStatus Status,
    int VariantCount, int TotalStock, decimal MinPrice, int SoldCount, DateTimeOffset CreatedAt);

public record SellerVariantDto(long Id, string Sku, string? Barcode, string? Color, string? Size, decimal Price, decimal? ListPrice,
    decimal VatRate, bool IsActive, int Quantity, int Reserved);

public record SellerProductDetailDto(long Id, string Name, string Slug, string? Description, long CategoryId, string CategoryName,
    long? BrandId, string? BrandName, ProductStatus Status, IReadOnlyList<string> Images, IReadOnlyList<SellerVariantDto> Variants);

public class CreateVariantRequest
{
    [Required, RegularExpression(@"^[A-Z0-9-]{4,40}$", ErrorMessage = "SKU 4-40 karakter; büyük harf, rakam ve tire içerebilir.")]
    public string Sku { get; set; } = default!;

    [MaxLength(30)]
    public string? Barcode { get; set; }

    [MaxLength(40)]
    public string? Color { get; set; }

    [MaxLength(20)]
    public string? Size { get; set; }

    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "Fiyat 0,01 ile 1.000.000 TL arasında olmalı.", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Price { get; set; }

    public decimal? ListPrice { get; set; }

    /// <summary>0, 1, 10 ya da 20 (servis doğrular; AllowedValues int/decimal karşılaştıramaz).</summary>
    public decimal VatRate { get; set; } = 20;

    [Range(0, 100000)]
    public int Stock { get; set; }
}

public class CreateProductRequest
{
    [Required, MinLength(5), MaxLength(200)]
    public string Name { get; set; } = default!;

    [MaxLength(5000)]
    public string? Description { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "Kategori seçin.")]
    public long CategoryId { get; set; }

    public long? BrandId { get; set; }

    [MaxLength(8)]
    public List<string> ImageUrls { get; set; } = [];

    [Required, MinLength(1, ErrorMessage = "En az bir varyant ekleyin."), MaxLength(50)]
    public List<CreateVariantRequest> Variants { get; set; } = [];

    /// <summary>true ise ürün doğrudan onaya gönderilir; değilse taslak kalır.</summary>
    public bool SubmitForApproval { get; set; }
}

public class UpdateProductRequest
{
    [Required, MinLength(5), MaxLength(200)]
    public string Name { get; set; } = default!;

    [MaxLength(5000)]
    public string? Description { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "Kategori seçin.")]
    public long CategoryId { get; set; }

    public long? BrandId { get; set; }
}

public class UpdateVariantRequest
{
    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "Fiyat 0,01 ile 1.000.000 TL arasında olmalı.", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Price { get; set; }

    public decimal? ListPrice { get; set; }

    public bool IsActive { get; set; } = true;
}

public class SetStockRequest
{
    [Range(0, 100000)]
    public int Quantity { get; set; }

    [MaxLength(200)]
    public string? Note { get; set; }
}

// ---- siparişler
public record SellerOrderItemDto(string ProductName, string Sku, string? VariantLabel, int Quantity, decimal UnitPrice, decimal LineTotal);

public record SellerShipmentDto(string Carrier, string? TrackingNumber, DateTimeOffset? ShippedAt);

public record SellerOrderDto(
    string OrderNumber,
    DateTimeOffset PlacedAt,
    OrderStatus Status,
    string? CustomerName,
    string? City,
    string? District,
    string? AddressLine,
    IReadOnlyList<SellerOrderItemDto> Items,
    decimal SellerTotal,
    SellerShipmentDto? Shipment);

public class ShipOrderRequest
{
    [Required, AllowedValues("Yurtiçi Kargo", "Aras Kargo", "MNG Kargo", "PTT Kargo", "Sürat Kargo", "HepsiJet", ErrorMessage = "Geçerli bir kargo firması seçin.")]
    public string Carrier { get; set; } = default!;

    [Required, RegularExpression(@"^[A-Z0-9]{8,20}$", ErrorMessage = "Takip numarası 8-20 karakter, büyük harf ve rakam olmalı.")]
    public string TrackingNumber { get; set; } = default!;
}

public static class SellerOrderFilter
{
    public const string ToShip = "to_ship";
    public const string Shipped = "shipped";
    public const string All = "all";
}
