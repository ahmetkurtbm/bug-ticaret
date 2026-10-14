using Microsoft.Extensions.Options;
using Tezgah.Notifications.Logic;

namespace Tezgah.Notifications.Api;

/// <summary>Endpoint'e eklenen işaret: bu uç X-Api-Key ister.</summary>
public sealed class RequireApiKeyMetadata;

/// <summary>
/// X-Api-Key başlığını doğrular; geçersizse 401 ProblemDetails döner. Hangi uca anahtar gerektiğini
/// endpoint metadata'sından okur, bu yüzden UseRouting'den SONRA çalışmalıdır.
/// </summary>
public class ApiKeyMiddleware(RequestDelegate next, IOptions<NotificationsOptions> options)
{
    public const string HeaderName = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        var required = context.GetEndpoint()?.Metadata.GetMetadata<RequireApiKeyMetadata>() is not null;
        if (required)
        {
            context.Request.Headers.TryGetValue(HeaderName, out var provided);
            if (!ApiKeyValidator.IsValid(provided.ToString(), options.Value.ApiKey))
            {
                var problem = context.RequestServices.GetRequiredService<IProblemDetailsService>();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await problem.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails =
                    {
                        Status = StatusCodes.Status401Unauthorized,
                        Title = "Yetkisiz istek",
                        Detail = "Geçerli bir API anahtarı gerekli.",
                    },
                });
                return;
            }
        }

        await next(context);
    }
}
