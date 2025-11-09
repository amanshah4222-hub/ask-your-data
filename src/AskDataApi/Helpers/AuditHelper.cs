using System.Security.Claims;
using Dapper;
using Npgsql;

namespace AskDataApi.Helpers;

public static class AuditHelper{
    public static async Task LogAuditAsync(
    Func<NpgsqlConnection> connFactory,
    string question,
    string generatedSql,
    string rewrittenSql,
    double confidence,
    int elapsedMs,
    ClaimsPrincipal user,
    IEnumerable<string> notes,
    string? error)
{
    await using var conn = connFactory();
    var sql = @"
        insert into ask_audit
            (question, generated_sql, rewritten_sql, confidence, elapsed_ms, user_id, user_email, notes)
        values
            (@question, @generated_sql, @rewritten_sql, @confidence, @elapsed_ms, @user_id, @user_email, @notes::jsonb);
    ";
    var userId = user?.FindFirst("sub")?.Value ?? user?.Identity?.Name;
    var email = user?.FindFirst("email")?.Value;
    var notesJson = System.Text.Json.JsonSerializer.Serialize(new {
        notes = notes,
        error = error
    });
    await conn.ExecuteAsync(sql, new {
        question,
        generated_sql = generatedSql,
        rewritten_sql = rewrittenSql,
        confidence,
        elapsed_ms = elapsedMs,
        user_id = userId,
        user_email = email,
        notes = notesJson
    });
}

}