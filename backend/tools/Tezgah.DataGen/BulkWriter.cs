using System.Diagnostics;
using Npgsql;
using NpgsqlTypes;

namespace Tezgah.DataGen;

/// <summary>PostgreSQL binary COPY üzerine ince sarmalayıcı. Milyonlarca satır için EF Core'dan kat kat hızlı.</summary>
public sealed class BulkWriter : IAsyncDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlBinaryImporter _importer;
    private readonly string _table;
    private readonly Stopwatch _watch = Stopwatch.StartNew();
    private long _rows;

    private BulkWriter(NpgsqlConnection connection, NpgsqlBinaryImporter importer, string table)
    {
        _connection = connection;
        _importer = importer;
        _table = table;
    }

    /// <summary>
    /// Her yazıcı kendi bağlantısını açar; böylece birden fazla tabloya aynı anda yazılabilir.
    /// Yükleme sırasında yabancı anahtar tetikleyicileri bu bağlantıda kapatılır (tutarlılığı
    /// üretici sağlar), aksi hâlde henüz commit edilmemiş üst kayıtlar görünmez.
    /// </summary>
    public static async Task<BulkWriter> OpenAsync(NpgsqlDataSource dataSource, string table, params string[] columns)
    {
        var conn = await dataSource.OpenConnectionAsync();
        await using (var cmd = new NpgsqlCommand("SET session_replication_role = replica", conn))
            await cmd.ExecuteNonQueryAsync();
        var importer = await conn.BeginBinaryImportAsync($"COPY {table} ({string.Join(", ", columns)}) FROM STDIN (FORMAT BINARY)");
        return new BulkWriter(conn, importer, table);
    }

    public async Task RowAsync(params object?[] values)
    {
        await _importer.StartRowAsync();
        foreach (var value in values)
        {
            switch (value)
            {
                case null: await _importer.WriteNullAsync(); break;
                case long v: await _importer.WriteAsync(v, NpgsqlDbType.Bigint); break;
                case int v: await _importer.WriteAsync(v, NpgsqlDbType.Integer); break;
                case decimal v: await _importer.WriteAsync(v, NpgsqlDbType.Numeric); break;
                case bool v: await _importer.WriteAsync(v, NpgsqlDbType.Boolean); break;
                case string v: await _importer.WriteAsync(v, NpgsqlDbType.Text); break;
                case DateTime v: await _importer.WriteAsync(DateTime.SpecifyKind(v, DateTimeKind.Utc), NpgsqlDbType.TimestampTz); break;
                case DateOnly v: await _importer.WriteAsync(v, NpgsqlDbType.Date); break;
                case Guid v: await _importer.WriteAsync(v, NpgsqlDbType.Uuid); break;
                default: throw new NotSupportedException($"{_table}: desteklenmeyen tip {value.GetType().Name}");
            }
        }
        _rows++;
    }

    private bool _disposed;

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await _importer.CompleteAsync();
        await _importer.DisposeAsync();
        await _connection.DisposeAsync();
        Console.WriteLine($"  {_table,-26} {_rows,12:N0} satır  {_watch.Elapsed.TotalSeconds,6:0.0} sn");
    }
}
