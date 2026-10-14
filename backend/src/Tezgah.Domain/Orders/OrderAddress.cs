namespace Tezgah.Domain.Orders;

/// <summary>Sipariş anındaki adresin kopyası. Müşteri adresini sonra değiştirse de sipariş etkilenmez.</summary>
public class OrderAddress
{
    public string FullName { get; set; } = default!;
    public string? Phone { get; set; }
    public string City { get; set; } = default!;
    public string District { get; set; } = default!;
    public string Line { get; set; } = default!;
    public string? PostalCode { get; set; }
}
