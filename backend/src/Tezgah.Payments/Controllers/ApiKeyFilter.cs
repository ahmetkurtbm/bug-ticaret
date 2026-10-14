using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Tezgah.Contracts.Payments;
using Tezgah.Payments.Logic;

namespace Tezgah.Payments.Controllers;

/// <summary>Dahili uçlar için X-Api-Key başlığını doğrular; geçersizse 401 ProblemDetails döner.</summary>
public class ApiKeyFilter(IOptions<PaymentsOptions> options) : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        context.HttpContext.Request.Headers.TryGetValue(PaymentWebhookSignature.ApiKeyHeader, out var provided);
        if (ApiKeyValidator.IsValid(provided.ToString(), options.Value.ApiKey))
            return;

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Yetkisiz istek",
            Detail = "Geçerli bir API anahtarı gerekli.",
        })
        { StatusCode = StatusCodes.Status401Unauthorized };
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
