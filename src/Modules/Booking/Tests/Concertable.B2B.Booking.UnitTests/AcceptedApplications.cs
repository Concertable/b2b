using System.Net;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Contracts.Enums;

namespace Concertable.B2B.Booking.UnitTests;

internal static class AcceptedApplications
{
    public static FlatFeeAcceptedApplication FlatFee() => new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        42,
        43,
        44,
        45,
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        Guid.Parse("33333333-3333-3333-3333-333333333333"),
        PaymentMethod.Transfer,
        new DateTime(2030, 1, 1, 19, 0, 0, DateTimeKind.Utc),
        new DateTime(2030, 1, 1, 22, 0, 0, DateTimeKind.Utc),
        [Genre.Rock],
        "Artist",
        "Venue",
        "Terms",
        "1",
        Signature("Artist"),
        Signature("Venue"),
        100m);

    public static DoorSplitAcceptedApplication DoorSplit(VerifyPayment? verification = null) => new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        42,
        43,
        44,
        45,
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        Guid.Parse("33333333-3333-3333-3333-333333333333"),
        PaymentMethod.Transfer,
        new DateTime(2030, 1, 1, 19, 0, 0, DateTimeKind.Utc),
        new DateTime(2030, 1, 1, 22, 0, 0, DateTimeKind.Utc),
        [Genre.Rock],
        "Artist",
        "Venue",
        "Terms",
        "1",
        Signature("Artist"),
        Signature("Venue"),
        70m,
        "pm_123",
        verification);

    private static SignatureDto Signature(string name) => new(
        Guid.NewGuid(),
        new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        IPAddress.Loopback,
        "tests",
        name,
        null);
}
