using Dapper;
using Npgsql;
using System.Text;

namespace AskDataApi.Services;

public interface ISchemaService
{
    Task<string> GetSchemaTextAsync(CancellationToken ct = default);
}

public class SchemaService : ISchemaService
{
    private readonly Func<NpgsqlConnection> _connFactory;
    private readonly ILogger<SchemaService> _logger;
    private string? _cached;
    private DateTime _cachedAt;
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(10);

    public SchemaService(Func<NpgsqlConnection> connFactory, ILogger<SchemaService> logger)
    {
        _connFactory = connFactory;
        _logger = logger;
    }

    public async Task<string> GetSchemaTextAsync(CancellationToken ct = default)
    {
        // simple in-memory cache
        if (_cached is not null && DateTime.UtcNow - _cachedAt < _ttl)
            return _cached;

        await using var conn = _connFactory();
        await conn.OpenAsync(ct);

        // pull all user tables/columns from Neon (Postgres)
        var rows = await conn.QueryAsync(@"
            select
                c.table_schema,
                c.table_name,
                c.column_name,
                c.data_type
            from information_schema.columns c
            join information_schema.tables t
              on c.table_name = t.table_name
             and c.table_schema = t.table_schema
            where c.table_schema not in ('pg_catalog','information_schema')
              and t.table_type = 'BASE TABLE'
            order by c.table_schema, c.table_name, c.ordinal_position;
        ");

        var sb = new StringBuilder();
        sb.AppendLine("You have a PostgreSQL database. Here are the tables and columns:");
        string? currentTable = null;
        string? currentSchema = null;

        foreach (var row in rows)
        {
            string schema = row.table_schema;
            string table = row.table_name;
            string col = row.column_name;
            string type = row.data_type;

            // start new table
            if (currentTable != table || currentSchema != schema)
            {
                sb.AppendLine($"- {schema}.{table} (");
                currentTable = table;
                currentSchema = schema;
            }

            sb.AppendLine($"    {col} {type},");
        }

        var schemaText = sb.ToString();
        _cached = schemaText;
        _cachedAt = DateTime.UtcNow;

        _logger.LogInformation("Schema loaded from Neon, length={Length}", schemaText.Length);

        return schemaText;
    }
}
