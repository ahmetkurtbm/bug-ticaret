namespace Tezgah.Domain.Orders;

public enum OrderStatus
{
    PendingPayment = 1,
    Paid = 2,
    Preparing = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6,
    Returned = 7,
}
