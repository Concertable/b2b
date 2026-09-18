using Concertable.B2B.Application.Domain;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Kernel.ValueObjects;

namespace Concertable.B2B.Application.UnitTests;

public sealed class ApplicationTermsFingerprintTests
{
    [Fact]
    public void Calculate_SubMicrosecondDatabasePrecisionDifference_IsStable()
    {
        var deal = new FlatFeeDealDto
        {
            Fee = 125.50m,
            PaymentMethod = PaymentMethod.Transfer,
        };
        var start = new DateTime(638937123456789017, DateTimeKind.Utc);
        var end = start.AddHours(3);

        var original = ApplicationTermsFingerprint.Calculate(deal, new DateRange(start, end));
        var persisted = ApplicationTermsFingerprint.Calculate(
            deal,
            new DateRange(
                new DateTime(start.Ticks - start.Ticks % TimeSpan.TicksPerMicrosecond, DateTimeKind.Utc),
                new DateTime(end.Ticks - end.Ticks % TimeSpan.TicksPerMicrosecond, DateTimeKind.Utc)));

        Assert.Equal(original, persisted);
    }
}
