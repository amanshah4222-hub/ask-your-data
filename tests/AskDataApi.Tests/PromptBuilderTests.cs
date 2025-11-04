using AskDataApi.Domain.Nl;
using FluentAssertions;

public class PromptBuilderTests
{
    [Fact]
    public void Maps_Top_Products_Question()
    {
        var r = PromptBuilder.Build("Top products by revenue", 5);
        r.Sql.ToLowerInvariant().Should().Contain("from order_items");
        r.Parameters.Should().NotBeNull();
    }
}
