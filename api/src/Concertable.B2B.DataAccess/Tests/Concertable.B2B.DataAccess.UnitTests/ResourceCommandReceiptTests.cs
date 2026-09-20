using System.Globalization;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.DataAccess.UnitTests;

public sealed class ResourceCommandReceiptTests
{
    [Fact]
    public void HashPayload_SeparatorInsideParts_DoesNotCollide()
    {
        var first = ResourceCommandReceipt.HashPayload("a\u001fb", "c");
        var second = ResourceCommandReceipt.HashPayload("a", "b\u001fc");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void HashPayload_NullAndLiteralSentinel_DoNotCollide()
    {
        var nullPart = ResourceCommandReceipt.HashPayload((object?)null);
        var literal = ResourceCommandReceipt.HashPayload("\u0000");

        Assert.NotEqual(nullPart, literal);
    }

    [Fact]
    public void HashPayload_FormattableValue_IsCultureInvariant()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");
            var british = ResourceCommandReceipt.HashPayload(1234.56m);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var french = ResourceCommandReceipt.HashPayload(1234.56m);

            Assert.Equal(british, french);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void HashPayload_DateTimesForSameInstant_AreEqual()
    {
        var utc = new DateTime(2026, 9, 20, 12, 34, 56, DateTimeKind.Utc);
        var local = utc.ToLocalTime();

        Assert.Equal(
            ResourceCommandReceipt.HashPayload(utc),
            ResourceCommandReceipt.HashPayload(local));
    }
}
