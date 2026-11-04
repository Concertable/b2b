using System.Globalization;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.DataAccess.UnitTests;

public sealed class IdempotencyHashTests
{
    [Fact]
    public void Create_SeparatorInsideParts_DoesNotCollide()
    {
        var first = IdempotencyHash.Create("a\u001fb", "c");
        var second = IdempotencyHash.Create("a", "b\u001fc");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Create_NullAndLiteralSentinel_DoNotCollide()
    {
        var nullPart = IdempotencyHash.Create((object?)null);
        var literal = IdempotencyHash.Create("\u0000");

        Assert.NotEqual(nullPart, literal);
    }

    [Fact]
    public void Create_FormattableValue_IsCultureInvariant()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");
            var british = IdempotencyHash.Create(1234.56m);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var french = IdempotencyHash.Create(1234.56m);

            Assert.Equal(british, french);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Create_DateTimesForSameInstant_AreEqual()
    {
        var utc = new DateTime(2026, 9, 20, 12, 34, 56, DateTimeKind.Utc);
        var local = utc.ToLocalTime();

        Assert.Equal(
            IdempotencyHash.Create(utc),
            IdempotencyHash.Create(local));
    }

    [Fact]
    public void Create_EmptyParts_UsesUnchangedSha256Bytes()
    {
        var hash = IdempotencyHash.Create();

        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", hash.Value);
    }

    [Fact]
    public void From_UppercasePersistedHash_CanonicalizesAndEqualsCreatedHash()
    {
        var created = IdempotencyHash.Create("payload");

        var restored = IdempotencyHash.From(created.Value.ToUpperInvariant());

        Assert.Equal(created.Value, restored.Value);
        Assert.Equal(created, restored);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg")]
    [InlineData("é000000000000000000000000000000000000000000000000000000000000000")]
    public void From_InvalidRepresentation_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => IdempotencyHash.From(value));
    }

    [Fact]
    public void From_NullRepresentation_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => IdempotencyHash.From(null!));
    }
}
