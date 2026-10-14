using Tezgah.Contracts.Payments;

namespace Tezgah.Application.Payments;

/// <summary>Ödeme servisine giden çağrı. Uygulaması Infrastructure'da (HTTP).</summary>
public interface IPaymentGateway
{
    Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken ct);
}

public class PaymentGatewayException(string message, Exception? inner = null) : Exception(message, inner)
{
    /// <summary>Ödeme servisi siparişin zaten ödendiğini bildirdi (sonuç olayı henüz gelmemiş olabilir).</summary>
    public bool AlreadyPaid { get; init; }
}
