using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Artist.Domain.Entities;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.B2B.Deal.Domain.Entities;
using Concertable.B2B.Opportunity.Domain.Entities;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.User.Domain.Entities;
using Concertable.B2B.Venue.Domain.Entities;
using Concertable.Kernel.ValueObjects;
using Concertable.Contracts.Enums;

namespace Concertable.B2B.IntegrationTests.Fixtures;

public sealed class SeedStateSnapshot
{
    public SeedUserSnapshot ArtistManager1 { get; }
    public SeedUserSnapshot ArtistManagerNoArtist { get; }
    public SeedUserSnapshot VenueManager1 { get; }
    public SeedUserSnapshot VenueManager2 { get; }
    public SeedUserSnapshot VenueManager3 { get; }
    public SeedUserSnapshot VenueManagerNoVenue { get; }
    public SeedUserSnapshot Admin { get; }
    public IReadOnlyList<SeedUserSnapshot> ArtistManagers { get; }
    public IReadOnlyList<SeedUserSnapshot> VenueManagers { get; }
    public IReadOnlyList<SeedUserSnapshot> Users { get; }
    public SeedArtistSnapshot Artist { get; }
    public SeedVenueSnapshot Venue { get; }
    public IReadOnlyList<SeedTenantSnapshot> Tenants { get; }
    public IReadOnlyList<SeedArtistSnapshot> Artists { get; }
    public IReadOnlyList<SeedVenueSnapshot> Venues { get; }
    public IReadOnlyList<SeedConcertSnapshot> Concerts { get; }
    public SeedFlatFeeDealSnapshot FlatFeeAppDeal { get; }
    public SeedVenueHireDealSnapshot VenueHireAppDeal { get; }
    public SeedOpportunitySnapshot ActiveVenueHireOpportunity { get; }
    public SeedApplicationSnapshot FlatFeeApp { get; }
    public SeedApplicationSnapshot VersusApp { get; }
    public SeedApplicationSnapshot DoorSplitApp { get; }
    public SeedApplicationSnapshot VenueHireApp { get; }
    public SeedApplicationSnapshot ConfirmedApp { get; }
    public SeedBookingSnapshot ConfirmedBooking { get; }
    public SeedApplicationSnapshot AwaitingPaymentApp { get; }
    public SeedBookingSnapshot AwaitingPaymentBooking { get; }
    public SeedApplicationSnapshot PastVersusApp { get; }
    public SeedBookingSnapshot PastVersusBooking { get; }
    public SeedApplicationSnapshot PastFlatFeeApp { get; }
    public SeedBookingSnapshot PastFlatFeeBooking { get; }
    public SeedApplicationSnapshot PastVenueHireApp { get; }
    public SeedBookingSnapshot PastVenueHireBooking { get; }
    public SeedApplicationSnapshot PastDoorSplitApp { get; }
    public SeedBookingSnapshot PastDoorSplitBooking { get; }
    public SeedBookingSnapshot UpcomingFlatFeeBooking { get; }
    public SeedBookingSnapshot UpcomingVenueHireBooking { get; }

    internal SeedStateSnapshot(SeedState source)
    {
        var users = source.Users.Select(SeedUserSnapshot.From).ToDictionary(user => user.Id);
        var artists = source.Artists.Select(SeedArtistSnapshot.From).ToDictionary(artist => artist.Id);
        var venues = source.Venues.Select(SeedVenueSnapshot.From).ToDictionary(venue => venue.Id);
        var applications = source.Applications
            .Select(SeedApplicationSnapshot.From)
            .ToDictionary(application => application.Id);
        var bookings = source.Bookings
            .Select(SeedBookingSnapshot.From)
            .ToDictionary(booking => booking.Id);

        ArtistManager1 = users[source.ArtistManager1.Id];
        ArtistManagerNoArtist = users[source.ArtistManagerNoArtist.Id];
        VenueManager1 = users[source.VenueManager1.Id];
        VenueManager2 = users[source.VenueManager2.Id];
        VenueManager3 = users[source.VenueManager3.Id];
        VenueManagerNoVenue = users[source.VenueManagerNoVenue.Id];
        Admin = users[source.Admin.Id];
        ArtistManagers = source.ArtistManagers.Select(user => users[user.Id]).ToArray();
        VenueManagers = source.VenueManagers.Select(user => users[user.Id]).ToArray();
        Users = source.Users.Select(user => users[user.Id]).ToArray();
        Artist = artists[source.Artist.Id];
        Venue = venues[source.Venue.Id];
        Tenants = source.Tenants.Select(SeedTenantSnapshot.From).ToArray();
        Artists = source.Artists.Select(artist => artists[artist.Id]).ToArray();
        Venues = source.Venues.Select(venue => venues[venue.Id]).ToArray();
        Concerts = source.Concerts.Select(SeedConcertSnapshot.From).ToArray();
        FlatFeeAppDeal = SeedFlatFeeDealSnapshot.From(source.FlatFeeAppDeal);
        VenueHireAppDeal = SeedVenueHireDealSnapshot.From(source.VenueHireAppDeal);
        ActiveVenueHireOpportunity = SeedOpportunitySnapshot.From(source.ActiveVenueHireOpportunity);
        FlatFeeApp = applications[source.FlatFeeApp.Id];
        VersusApp = applications[source.VersusApp.Id];
        DoorSplitApp = applications[source.DoorSplitApp.Id];
        VenueHireApp = applications[source.VenueHireApp.Id];
        ConfirmedApp = applications[source.ConfirmedApp.Id];
        ConfirmedBooking = bookings[source.ConfirmedBooking.Id];
        AwaitingPaymentApp = applications[source.AwaitingPaymentApp.Id];
        AwaitingPaymentBooking = bookings[source.AwaitingPaymentBooking.Id];
        PastVersusApp = applications[source.PastVersusApp.Id];
        PastVersusBooking = bookings[source.PastVersusBooking.Id];
        PastFlatFeeApp = applications[source.PastFlatFeeApp.Id];
        PastFlatFeeBooking = bookings[source.PastFlatFeeBooking.Id];
        PastVenueHireApp = applications[source.PastVenueHireApp.Id];
        PastVenueHireBooking = bookings[source.PastVenueHireBooking.Id];
        PastDoorSplitApp = applications[source.PastDoorSplitApp.Id];
        PastDoorSplitBooking = bookings[source.PastDoorSplitBooking.Id];
        UpcomingFlatFeeBooking = bookings[source.UpcomingFlatFeeBooking.Id];
        UpcomingVenueHireBooking = bookings[source.UpcomingVenueHireBooking.Id];
    }

    public SeedConcertSnapshot ConcertFor(SeedBookingSnapshot booking) =>
        Concerts.Single(concert => concert.BookingId == booking.Id);
}

public sealed record SeedUserSnapshot(Guid Id, string Email)
{
    internal static SeedUserSnapshot From(UserEntity user) => new(user.Id, user.Email);
}

public sealed record SeedTenantSnapshot(
    Guid Id,
    Guid CreatedByUserId,
    string LegalName,
    string? RegisteredAddress)
{
    internal static SeedTenantSnapshot From(TenantEntity tenant) =>
        new(
            tenant.Id,
            tenant.CreatedByUserId,
            tenant.LegalName,
            tenant.TaxCompliance is null
                ? null
                : string.Join(
                    ", ",
                    new[]
                    {
                        tenant.TaxCompliance.RegisteredAddress.Line1,
                        tenant.TaxCompliance.RegisteredAddress.Line2,
                        tenant.TaxCompliance.RegisteredAddress.City,
                        tenant.TaxCompliance.RegisteredAddress.Postcode,
                        tenant.TaxCompliance.RegisteredAddress.Country
                    }.Where(value => !string.IsNullOrWhiteSpace(value))));
}

public sealed record SeedArtistSnapshot(
    int Id,
    Guid TenantId,
    Guid UserId,
    string Name,
    string Email,
    IReadOnlySet<Genre> Genres)
{
    internal static SeedArtistSnapshot From(ArtistEntity artist) =>
        new(artist.Id, artist.TenantId, artist.UserId, artist.Name, artist.Email, artist.Genres.ToHashSet());
}

public sealed record SeedVenueSnapshot(
    int Id,
    Guid TenantId,
    Guid UserId,
    string Name,
    string Email,
    string County,
    string Town)
{
    internal static SeedVenueSnapshot From(VenueEntity venue) =>
        new(
            venue.Id,
            venue.TenantId,
            venue.UserId,
            venue.Name,
            venue.Email,
            venue.Address.County,
            venue.Address.Town);
}

public sealed record SeedFlatFeeDealSnapshot(int Id, Guid TenantId, DealType DealType, decimal Fee)
{
    internal static SeedFlatFeeDealSnapshot From(FlatFeeDealEntity deal) =>
        new(deal.Id, deal.TenantId, deal.DealType, deal.Fee);
}

public sealed record SeedVenueHireDealSnapshot(int Id, Guid TenantId, DealType DealType, decimal HireFee)
{
    internal static SeedVenueHireDealSnapshot From(VenueHireDealEntity deal) =>
        new(deal.Id, deal.TenantId, deal.DealType, deal.HireFee);
}

public sealed record SeedOpportunitySnapshot(int Id, Guid TenantId, int VenueId, int DealId, DateRange Period)
{
    internal static SeedOpportunitySnapshot From(OpportunityEntity opportunity) =>
        new(opportunity.Id, opportunity.TenantId, opportunity.VenueId, opportunity.DealId, opportunity.Period);
}

public sealed record SeedApplicationSnapshot(
    int Id,
    Guid VenueTenantId,
    Guid ArtistTenantId,
    int OpportunityId,
    int ArtistId,
    DealType DealType)
{
    internal static SeedApplicationSnapshot From(ApplicationEntity application) =>
        new(
            application.Id,
            application.VenueTenantId,
            application.ArtistTenantId,
            application.OpportunityId,
            application.ArtistId,
            application.DealType);
}

public sealed record SeedBookingSnapshot(
    int Id,
    Guid VenueTenantId,
    Guid ArtistTenantId,
    int ApplicationId,
    int OpportunityId,
    int ArtistId,
    int VenueId,
    DealType DealType,
    DateTime StartDate,
    DateTime EndDate)
{
    internal static SeedBookingSnapshot From(BookingEntity booking) =>
        new(
            booking.Id,
            booking.VenueTenantId,
            booking.ArtistTenantId,
            booking.ApplicationId,
            booking.OpportunityId,
            booking.ArtistId,
            booking.VenueId,
            booking.DealType,
            booking.StartDate,
            booking.EndDate);
}

public sealed record SeedConcertSnapshot(
    int Id,
    Guid VenueTenantId,
    Guid ArtistTenantId,
    int BookingId,
    int ApplicationId,
    int OpportunityId,
    int ArtistId,
    int VenueId,
    DealType DealType,
    DateRange Period,
    DateTime? DatePosted,
    string? SettlementPaymentMethodId)
{
    internal static SeedConcertSnapshot From(ConcertEntity concert) =>
        new(
            concert.Id,
            concert.VenueTenantId,
            concert.ArtistTenantId,
            concert.BookingId,
            concert.ApplicationId,
            concert.OpportunityId,
            concert.ArtistId,
            concert.VenueId,
            concert.DealType,
            concert.Period,
            concert.DatePosted,
            concert.SettlementPaymentMethodId);
}
