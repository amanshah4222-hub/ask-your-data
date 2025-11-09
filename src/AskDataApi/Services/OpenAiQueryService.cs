using System.Net.Http.Json;
using System.Text.Json;

namespace AskDataApi.Services;

public class OpenAiSqlService
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly ISchemaService _schemaService;

    public OpenAiSqlService(HttpClient http, IConfiguration config, ISchemaService schemaService)
    {
        _http = http;
        _schemaService = schemaService;
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",
                Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new InvalidOperationException("OPENAI_API_KEY missing"));

        _model = config["OpenAI:Model"] ?? "gpt-4o-mini";
    }

    public async Task<(string sql, double confidence)> BuildSqlAsync(string question, int? limit, CancellationToken ct = default)
    {
        var schemaText = await _schemaService.GetSchemaTextAsync(ct);

        var systemPrompt = $"""
        You are a SQL assistant. Target database is PostgreSQL.
        Use ONLY the following schema:
        {schemaText}
        Rules:
        - return exactly ONE SELECT statement
        - do not modify data
        - prefer fully-qualified table names (schema.table)
        - always apply LIMIT {limit ?? 50} if user did not specify
        - use correct column names from the schema
        - give just plain executable sql without sql keyword at start
        - keep single line just 
        """;

        var payload = new
        {
            model = _model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = question }
            },
            temperature = 0
        };

        var res = await _http.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", payload, ct);
        res.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
        var sql = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";

        return (sql.Trim(), 0.8);
    }
}
