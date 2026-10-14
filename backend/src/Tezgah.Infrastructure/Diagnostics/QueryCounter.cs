using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Tezgah.Infrastructure.Diagnostics;

/// <summary>Bir istek boyunca EF Core'un çalıştırdığı SQL komutlarının sayısı ve toplam süresi.</summary>
public class QueryCounter
{
    private int _count;
    private long _ticks;

    public int Count => _count;
    public TimeSpan Duration => TimeSpan.FromTicks(Interlocked.Read(ref _ticks));

    public void Add(TimeSpan duration)
    {
        Interlocked.Increment(ref _count);
        Interlocked.Add(ref _ticks, duration.Ticks);
    }
}

/// <summary>
/// Geliştirme aracı: her SQL komutunu sayar. API, yanıta "Server-Timing: db;dur=..;desc="N sorgu"" ekler;
/// tarayıcının Network sekmesinde (Timing) görünür. N+1 sorgu sorunlarını yakalamanın en ucuz yolu.
/// </summary>
public class QueryCountingInterceptor(QueryCounter counter) : DbCommandInterceptor
{
    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        counter.Add(eventData.Duration);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        counter.Add(eventData.Duration);
        return ValueTask.FromResult(result);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        counter.Add(eventData.Duration);
        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        counter.Add(eventData.Duration);
        return ValueTask.FromResult(result);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        counter.Add(eventData.Duration);
        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        counter.Add(eventData.Duration);
        return ValueTask.FromResult(result);
    }
}
