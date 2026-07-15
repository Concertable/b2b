using System.Reflection;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Venue.Contracts;
using Concertable.Kernel;

namespace Concertable.B2B.Concert.UnitTests;

// Guards the self-naming OrNotFound path: a type fetched via the zero-arg .OrNotFound<T>() resolves its
// 404 label through DisplayNameResolver.Of<T>(), which THROWS if the type has no [DisplayName]. Pinning
// each type's resolved name here makes a dropped attribute (or a changed label) fail red at test time,
// not only on the runtime throw path. Covers the B2B self-naming types visible from the Concert module.
public sealed class DisplayNameConventionTests
{
    public static TheoryData<Type, string> SelfNamingTypes => new()
    {
        { typeof(ConcertEntity), "Concert" },
        { typeof(ApplicationEntity), "Application" },
        { typeof(BookingEntity), "Booking" },
        { typeof(ContractEntity), "Contract" },
        { typeof(OpportunityEntity), "Concert Opportunity" },
        { typeof(ConcertDetails), "Concert" },
        { typeof(ArtistSummary), "Artist" },
        { typeof(VenueSummary), "Venue" },
    };

    [Theory]
    [MemberData(nameof(SelfNamingTypes))]
    public void Zero_arg_OrNotFound_type_resolves_its_display_name(Type type, string expected)
    {
        MethodInfo of = typeof(DisplayNameResolver).GetMethod(nameof(DisplayNameResolver.Of))!
            .MakeGenericMethod(type);

        string resolved = (string)of.Invoke(null, null)!;

        Assert.Equal(expected, resolved);
    }
}
