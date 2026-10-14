using Tezgah.Domain.Common;

namespace Tezgah.Domain.Orders;

public class OrderItem : Entity
{
    public long OrderId { get; set; }
    public long SellerId { get; set; }
    public long VariantId { get; set; }

    /// <summary>Stoğun ayrıldığı depo kalemi. Eski siparişlerde boş.</summary>
    public long? StockItemId { get; set; }

    // Sipariş anındaki ürün bilgisi; ürün sonra değişse de silinse de sipariş okunabilir kalır.
    public string ProductName { get; set; } = default!;
    public string Sku { get; set; } = default!;
    public string? VariantLabel { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>Sipariş anında geçerli komisyon oranı (yüzde).</summary>
    public decimal CommissionRate { get; set; }
}
