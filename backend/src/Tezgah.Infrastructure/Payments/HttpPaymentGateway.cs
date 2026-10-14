using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Tezgah.Application.Payments;
using Tezgah.Contracts.Payments;

namespace Tezgah.Infrastructure.Payments;

/// <summary>Ödeme servisine typed HttpClient ile gider. Adres, anahtar ve zaman aşımı DI kaydında ayarlanır.</summary>
public class HttpPaymentGateway(HttpClient http, ILogger<HttpPaymentGateway> logger) : IPaymentGateway
{
    public async Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken ct)
    {
        try
        {
            using var response = await http.PostAsJsonAsync("api/payments", request, ct);
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                throw new PaymentGatewayException("Sipariş zaten ödenmiş.") { AlreadyPaid = true };
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Ödeme servisi {Status} döndü: {Body}", (int)response.StatusCode, body.Length > 500 ? body[..500] : body);
                throw new PaymentGatewayException($"Ödeme servisi {(int)response.StatusCode} döndü.");
            }
            return await response.Content.ReadFromJsonAsync<CreatePaymentResponse>(ct)
                ?? throw new PaymentGatewayException("Ödeme servisinden boş yanıt geldi.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            throw new PaymentGatewayException("Ödeme servisine ulaşılamadı.", ex);
        }
    }
}
