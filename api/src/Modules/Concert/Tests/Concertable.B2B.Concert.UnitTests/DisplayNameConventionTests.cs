using System.Reflection;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Venue.Contracts;
using Concertable.Kernel;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class DisplayNameConventionTests
{
    public static TheoryData<Type, string> SelfNamingTypes => new()
    {
        { typeof(ConcertEntity), "Concert" },
        { typeof(InvoiceEntity), "Invoice" },
        { typeof(SelfBillingAgreementEntity), "Self-Billing Agreement" },
        { typeof(ConcertDetails), "Concert" },
        { typeof(ArtistSummary), "Artist" },
        { typeof(VenueSummary), "Venue" },
    };

    [Theory]
    [MemberData(nameof(SelfNamingTypes))]
    public void Of_SelfNamingType_ReturnsDisplayName(Type type, string expected)
    {
        MethodInfo of = typeof(DisplayNameResolver).GetMethod(nameof(DisplayNameResolver.Of))!
            .MakeGenericMethod(type);

        string resolved = (string)of.Invoke(null, null)!;

        Assert.Equal(expected, resolved);
    }
}
