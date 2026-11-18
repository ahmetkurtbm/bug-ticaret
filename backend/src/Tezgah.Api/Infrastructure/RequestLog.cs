using System.Diagnostics;
using Tezgah.Infrastructure.Diagnostics;

namespace Tezgah.Api.Infrastructure;

/// <summary>Gelen istekler ekranındaki tek satır. Zaman UTC; ekranda Europe/Istanbul'a çevrilir.</summary>
public record RequestLogEntry(
    DateTimeOffset Time,
    string Method,
    string Path,
    int Status,
    double DurationMs,
    int SqlCount,
    string? User,
    string? ExceptionType,
    string? ExceptionMessage,
    string? StackTrace);

/// <summary>Son N isteği bellekte tutan halka tampon (thread-safe, singleton). Yalnızca geliştirme ortamında kullanılır.</summary>
public class RequestLogStore
{
    public const int Capacity = 200;

    private readonly Lock _lock = new();
    private readonly RequestLogEntry[] _buffer = new RequestLogEntry[Capacity];
    private int _next;
    private int _count;

    public void Add(RequestLogEntry entry)
    {
        lock (_lock)
        {
            _buffer[_next] = entry;
            _next = (_next + 1) % Capacity;
            if (_count < Capacity)
                _count++;
        }
    }

    /// <summary>Yeniden eskiye sıralı kopya.</summary>
    public IReadOnlyList<RequestLogEntry> Snapshot()
    {
        lock (_lock)
        {
            var result = new List<RequestLogEntry>(_count);
            for (var i = 1; i <= _count; i++)
                result.Add(_buffer[(_next - i + Capacity) % Capacity]);
            return result;
        }
    }
}

/// <summary>
/// İstekleri RequestLogStore'a yazar. UseExceptionHandler'dan SONRA eklenir: istisnayı kaydedip yeniden fırlatır,
/// satır yanıt tamamlanınca yazılır ki exception handler'ın belirlediği gerçek durum kodu (404, 409, 500...) görünsün.
/// </summary>
public class RequestLogMiddleware(RequestDelegate next, RequestLogStore store)
{
    private const int MaxStackLines = 15;

    // Ekranın kendi istekleri ve sağlık kontrolü kaydedilmez.
    private static readonly string[] Ignored = ["/", "/_istekler", "/favicon.ico", "/health"];

    public async Task InvokeAsync(HttpContext context)
    {
        if (Ignored.Any(p => string.Equals(context.Request.Path.Value, p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var started = DateTimeOffset.UtcNow;
        var timer = Stopwatch.StartNew();
        var counter = context.RequestServices.GetRequiredService<QueryCounter>();
        Exception? error = null;

        context.Response.OnCompleted(() =>
        {
            timer.Stop();
            var status = context.Response.StatusCode;
            if (error is not null && status < 400)
                status = StatusCodes.Status500InternalServerError;

            store.Add(new RequestLogEntry(
                started,
                context.Request.Method,
                context.Request.Path + context.Request.QueryString.ToString(),
                status,
                Math.Round(timer.Elapsed.TotalMilliseconds, 1),
                counter.Count,
                GetUser(context),
                error?.GetType().FullName,
                error?.Message,
                error?.StackTrace is null ? null : FirstLines(error.StackTrace, MaxStackLines)));
            return Task.CompletedTask;
        });

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
    }

    // Kimlik doğrulama bu middleware'den sonra çalışır; yanıt tamamlandığında User dolmuş olur.
    private static string? GetUser(HttpContext context)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated != true)
            return null;
        return user.FindFirst("email")?.Value ?? user.FindFirst("sub")?.Value;
    }

    private static string FirstLines(string text, int count) =>
        string.Join('\n', text.Split('\n').Take(count).Select(l => l.TrimEnd('\r')));
}
