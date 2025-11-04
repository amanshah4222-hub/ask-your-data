using System.Text.RegularExpressions;

namespace AskDataApi.Domain.Query;

public record SqlValidationResult(bool IsValid, string RewrittenSql, List<string> Notes, List<string> Errors);

public class SqlValidator
{
    private readonly int _maxRows;
    private static readonly Regex MultiWhitespace = new(@"\s+", RegexOptions.Compiled);
    private static readonly string[] Forbidden =
    {
        "insert","update","delete","merge","alter","drop","create","grant","revoke",
        "truncate","call","copy","vacuum","analyze","explain analyze","listen","notify",
        "set ","reset ","do ", "refresh materialized view", "cluster", "reindex"
    };

    public SqlValidator(int maxRows = 5000) => _maxRows = maxRows;

    public SqlValidationResult Validate(string sql)
    {
        var notes = new List<string>();
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(sql))
            return new SqlValidationResult(false, "", notes, new() { "EMPTY_SQL" });

        var s = sql.Trim();
        // Normalize whitespace 
        var normalized = MultiWhitespace.Replace(s, " ").Trim();

        // No semicolons inside
        if (normalized.Count(c => c == ';') > 1)
            errors.Add("MULTI_STATEMENT_NOT_ALLOWED");

        normalized = normalized.TrimEnd(';');

        // Must start with SELECT
        if (!normalized.StartsWith("select", StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith("with", StringComparison.OrdinalIgnoreCase)) // allow CTEs? Disable for now
        {
            errors.Add("ONLY_SELECT_ALLOWED");
        }

        // Block obvious dangerous tokens (substring match, case-insensitive)
        var lowered = normalized.ToLowerInvariant();
        foreach (var bad in Forbidden)
        {
            if (lowered.Contains(bad))
            {
                errors.Add($"FORBIDDEN_TOKEN:{bad.Trim()}");
            }
        }

        if (errors.Count > 0)
            return new SqlValidationResult(false, "", notes, errors);

        // Ensure LIMIT cap exists 
        if (!Regex.IsMatch(lowered, @"\blimit\b", RegexOptions.IgnoreCase))
        {
            normalized += $" limit {_maxRows}";
            notes.Add($"LIMIT_APPLIED:{_maxRows}");
        }

        return new SqlValidationResult(true, normalized, notes, errors);
    }
}
