using Dapper;
using Npgsql;

namespace AskDataApi.Domain.Query;

public class QueryOrchestrator
{
    private readonly Func<NpgsqlConnection> _connFactory;
    private readonly TimeSpan _timeout;

    public QueryOrchestrator(Func<NpgsqlConnection> connFactory, TimeSpan? timeout = null)
    {
        _connFactory = connFactory;
        _timeout = timeout ?? TimeSpan.FromSeconds(15);
    }

    public async Task<(IEnumerable<dynamic> Rows, TimeSpan Elapsed)> ExecuteAsync(
        string sql, object? parameters, CancellationToken ct = default)
    {
        var start = DateTime.UtcNow;

        await using var conn = _connFactory();
        await conn.OpenAsync(ct);

        // Enforce statement timeout per-connection 
        await using (var cmd = new NpgsqlCommand($"SET LOCAL statement_timeout = {(int)_timeout.TotalMilliseconds};", conn))
        {
            await cmd.ExecuteNonQueryAsync(ct);
        }

        var rows = await conn.QueryAsync(sql, parameters);
        var elapsed = DateTime.UtcNow - start;
        return (rows, elapsed);
    }
}
