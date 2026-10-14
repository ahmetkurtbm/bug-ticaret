namespace Tezgah.Application.Common.Exceptions;

/// <summary>Alan bazlı doğrulama hatası; API'de ValidationProblemDetails (400) olarak döner.</summary>
public class RequestValidationException(IDictionary<string, string[]> errors) : Exception("Girilen bilgileri kontrol edin.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;

    public static RequestValidationException For(string field, string message) => new(new Dictionary<string, string[]> { [field] = [message] });
}
