using System.Globalization;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.DataAccess.UnitTests;

public sealed class CommandPayloadHashTests
{
    [Fact]
    public void Compute_SeparatorInsideParts_DoesNotCollide()
    {
        var first = CommandPayloadHash.Compute("a\u001fb", "c");
        var second = CommandPayloadHash.Compute("a", "b\u001fc");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Compute_NullAndLiteralSentinel_DoNotCollide()
    {
        var nullPart = CommandPayloadHash.Compute((object?)null);
        var literal = CommandPayloadHash.Compute("\u0000");

        Assert.NotEqual(nullPart, literal);
    }

    [Fact]
    public void Compute_FormattableValue_IsCultureInvariant()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");
            var british = CommandPayloadHash.Compute(1234.56m);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var french = CommandPayloadHash.Compute(1234.56m);

            Assert.Equal(british, french);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Compute_DateTimesForSameInstant_AreEqual()
    {
        var utc = new DateTime(2026, 9, 20, 12, 34, 56, DateTimeKind.Utc);
        var local = utc.ToLocalTime();

        Assert.Equal(
            CommandPayloadHash.Compute(utc),
            CommandPayloadHash.Compute(local));
    }
}
