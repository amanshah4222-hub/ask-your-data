using AskDataApi.Domain.Query;
using FluentAssertions;

public class SqlValidatorTests
{
    [Fact]
    public void Rejects_Dml_And_Ddl()
    {
        var v = new SqlValidator();
        v.Validate("DELETE FROM x").IsValid.Should().BeFalse();
        v.Validate("DROP TABLE y").IsValid.Should().BeFalse();
        v.Validate("UPDATE a SET b=1").IsValid.Should().BeFalse();
    }

    [Fact]
    public void Allows_Select_And_Appends_Limit()
    {
        var v = new SqlValidator(maxRows: 123);
        var res = v.Validate("select id from products");
        res.IsValid.Should().BeTrue();
        res.RewrittenSql.ToLowerInvariant().Should().Contain("limit 123");
    }

    [Fact]
    public void Forbids_Multi_Statements()
    {
        var v = new SqlValidator();
        var r = v.Validate("select 1; select 2;");
        r.IsValid.Should().BeFalse();
    }
}
