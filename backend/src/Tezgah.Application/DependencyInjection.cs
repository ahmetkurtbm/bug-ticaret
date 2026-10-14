using Microsoft.Extensions.DependencyInjection;
using Tezgah.Application.Addresses;
using Tezgah.Application.Admin;
using Tezgah.Application.Sellers;
using Tezgah.Application.Auth;
using Tezgah.Application.Campaigns;
using Tezgah.Application.Carts;
using Tezgah.Application.Catalog;
using Tezgah.Application.Orders;
using Tezgah.Application.Payments;

namespace Tezgah.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<CouponValidator>();
        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentWebhookService, PaymentWebhookService>();
        services.AddScoped<ExpireUnpaidOrders>();
        services.AddScoped<SellerContext>();
        services.AddScoped<ISellerApplicationService, SellerApplicationService>();
        services.AddScoped<ISellerCatalogService, SellerCatalogService>();
        services.AddScoped<ISellerOrderService, SellerOrderService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IAdminOrderService, AdminOrderService>();
        return services;
    }
}
