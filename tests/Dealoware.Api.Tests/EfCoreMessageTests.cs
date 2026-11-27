using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Dealoware.Api.Tests;

/// <summary>
/// EF Core's error events put a whole exception's text, with the database's own words, after the
/// first line of their message. For its categories the formatter keeps the first line only.
/// </summary>
public class EfCoreMessageTests
{
    private const string Quoted = "quoted-value-3f9a-NOT-FOR-LOGS";

    [Theory]
    [InlineData("Microsoft.EntityFrameworkCore.Update")]
    [InlineData("Microsoft.EntityFrameworkCore.Database.Command")]
    public void EfCoreMessage_IsCutAtItsFirstLine(string category)
    {
        var capture = new FormatterCapture();
        var message = "An exception occurred in the database while saving changes." + Environment.NewLine
            + "Npgsql.PostgresException (0x80004005): 23505: duplicate key value" + Environment.NewLine
            + "DETAIL: Key (Name)=(" + Quoted + ") already exists.";

        capture.CreateLogger(category).LogError("{Text}", message);

        var json = JsonDocument.Parse(Assert.Single(capture.Lines)).RootElement;
        Assert.Equal("An exception occurred in the database while saving changes.", json.GetProperty("Message").GetString());
        Assert.DoesNotContain(Quoted, capture.Text);
    }

    [Fact]
    public void OtherCategories_KeepTheirWholeMessage()
    {
        var capture = new FormatterCapture();

        capture.CreateLogger("Dealoware.Something").LogInformation("{Text}", "first\nsecond");

        var json = JsonDocument.Parse(Assert.Single(capture.Lines)).RootElement;
        Assert.Equal("first\nsecond", json.GetProperty("Message").GetString());
    }
}
