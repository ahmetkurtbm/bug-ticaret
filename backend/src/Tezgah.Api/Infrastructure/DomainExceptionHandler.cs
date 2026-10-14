using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Tezgah.Application.Common.Exceptions;
using Tezgah.Domain.Common;

namespace Tezgah.Api.Infrastructure;

/// <summary>Uygulama ve iş kuralı istisnalarını ProblemDetails yanıtına çevirir; diğerleri 500 olarak loglanır.</summary>
public class DomainExceptionHandler(IProblemDetailsService problemDetails, ILogger<DomainExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, code) = exception switch
        {
            NotFoundException e => (StatusCodes.Status404NotFound, e.Message, "not_found"),
            ForbiddenException e => (StatusCodes.Status403Forbidden, e.Message, "forbidden"),
            ConflictException e => (StatusCodes.Status409Conflict, e.Message, e.Code),
            DomainException e => (StatusCodes.Status400BadRequest, e.Message, e.Code),
            _ => (0, null, null),
        };

        if (exception is RequestValidationException validation)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var details = new ValidationProblemDetails(validation.Errors) { Status = 400, Title = validation.Message };
            return await problemDetails.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = details, Exception = exception });
        }

        if (status == 0)
        {
            logger.LogError(exception, "Beklenmeyen hata: {Path}", context.Request.Path);
            return false;
        }

        context.Response.StatusCode = status;
        var problem = new ProblemDetails { Status = status, Title = title };
        problem.Extensions["code"] = code;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
