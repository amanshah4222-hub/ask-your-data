namespace AskDataApi.Domain.Nl;

public record PromptResult(string Sql, double Confidence, object? Parameters);

public static class PromptBuilder
{
    public static PromptResult Build(string question, int? limitOverride = null)
    {
        var q = (question ?? string.Empty).Trim().ToLowerInvariant();

        if (q.Contains("top") && q.Contains("product") && (q.Contains("revenue") || q.Contains("sales")))
        {
            var limit = limitOverride is > 0 and <= 100 ? limitOverride!.Value : 10;
            var sql = """
                select p.id, p.name, sum(oi.quantity * oi.unit_price) as revenue
                from order_items oi
                join products p on p.id = oi.product_id
                group by p.id, p.name
                order by revenue desc
                limit @limit;
            """;
            return new PromptResult(sql, 0.70, new { limit });
        }

        if ((q.Contains("list") || q.Contains("show")) && q.Contains("tables"))
        {
            var sql = """
                select table_schema, table_name
                from information_schema.tables
                where table_schema not in ('pg_catalog','information_schema')
                order by table_schema, table_name
                limit @limit;
            """;
            return new PromptResult(sql, 0.60, new { limit = limitOverride ?? 50 });
        }

        return new PromptResult("select 1 as unsupported_request limit 1;", 0.10, null);
    }
}
