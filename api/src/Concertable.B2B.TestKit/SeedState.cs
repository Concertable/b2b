namespace Concertable.B2B.TestKit;

public sealed record SeedState
{
    public const string TestPassword = "Password11!";

    public required TestUser ArtistManager1 { get; init; }
    public required TestUser VenueManager1 { get; init; }
    public required TestUser VenueManager2 { get; init; }
    public required TestUser VenueManager3 { get; init; }
    public required IReadOnlyList<TestTenant> Tenants { get; init; }
    public required TestEntity Venue { get; init; }
    public required TestOpportunity ActiveVenueHireOpportunity { get; init; }
    public required TestEntity FlatFeeApplication { get; init; }
    public required TestEntity DoorSplitApplication { get; init; }
    public required TestEntity VersusApplication { get; init; }
    public required TestApplication VenueHireApplication { get; init; }
    public required TestEntity PastFlatFeeApplication { get; init; }
    public required TestEntity PastVenueHireApplication { get; init; }
    public required TestBooking PastDoorSplitBooking { get; init; }
    public required TestBooking PastVersusBooking { get; init; }
}

public sealed record TestUser(Guid Id, string Email);

public sealed record TestTenant(Guid Id, Guid CreatedByUserId);

public sealed record TestEntity(int Id);

public sealed record TestApplication(int Id, int OpportunityId);

public sealed record TestOpportunity(int Id, int VenueId);

public sealed record TestConcert(int Id, int TicketsSold);

public sealed record TestBooking(int Id, int ApplicationId, TestConcert Concert);
