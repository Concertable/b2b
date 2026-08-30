using Concertable.B2B.Artist.Domain.Entities;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Deal.Domain.Entities;
using Concertable.B2B.Seed.Contracts;
using Concertable.B2B.Seed.Infrastructure.Factories;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Contracts.Enums;
using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.User.Domain.Entities;
using Concertable.Contracts;
using Concertable.B2B.Venue.Domain.Entities;
using Concertable.B2B.Opportunity.Domain.Entities;
using Concertable.Kernel.ValueObjects;
using Concertable.Seed.Identity;
using Concertable.Seed.Identity.Extensions;
using NetTopologySuite.Geometries;

namespace Concertable.B2B.Seed.Infrastructure;

public sealed class SeedState
{
    private readonly Dictionary<int, ApplicationEntity> bookingApplications = [];

    public const string TestPassword = "Password11!";

    public UserEntity ArtistManager1 { get; }
    public UserEntity ArtistManagerNoArtist { get; }
    public UserEntity VenueManager1 { get; }
    public UserEntity VenueManager2 { get; }
    public UserEntity VenueManager3 { get; }
    public UserEntity VenueManagerNoVenue { get; }
    public UserEntity Admin { get; }

    public IReadOnlyList<UserEntity> ArtistManagers { get; }
    public IReadOnlyList<UserEntity> VenueManagers { get; }
    public IReadOnlyList<UserEntity> Users { get; }

    public ArtistEntity Artist { get; }
    public VenueEntity Venue { get; }

    /// <summary>One tenant per operator (the manager's legal entity) — every manager, venue and artist alike.
    /// Venues/opportunities/deals and artists all carry the matching <c>TenantId</c>.</summary>
    public IReadOnlyList<TenantEntity> Tenants { get; }

    /// <summary>The founding Owner membership per operator — the source of truth for tenant authority. Only
    /// founding-Owner rows are ever seeded; invitation-derived memberships are handler/API-written, never seeded.</summary>
    public IReadOnlyList<TenantMembershipEntity> Memberships { get; }

    /// <summary>An <c>Approved</c> verification row per tenant that also has tax compliance complete — the
    /// verification gate's fail-closed default would otherwise defer every seeded opportunity-publish and
    /// settlement in the suite. The two bare operators (<see cref="VenueManagerNoVenue"/>,
    /// <see cref="ArtistManagerNoArtist"/>) deliberately have no row, staying unverified alongside their
    /// incomplete tax compliance — the same "registered but never completed onboarding" fixture, for both
    /// dimensions.</summary>
    public IReadOnlyList<TenantVerificationEntity> Verifications { get; }

    private static readonly Guid UnverifiedTenantUserId = new("c1000000-0000-0000-0000-000000000001");

    /// <summary>Tax-complete but never submitted for verification — deliberately not one of
    /// <see cref="Tenants"/>' bare operators, which are also tax-incomplete. Isolates the verification
    /// gate from the tax-compliance gate for <c>TenantVerificationGateApiTests</c>. Owns
    /// <see cref="UnverifiedVenueManager"/>'s venue, so the same fixture also drives the opportunity-
    /// publication gate test.</summary>
    public TenantEntity UnverifiedTenant { get; }

    /// <summary>Founding owner of <see cref="UnverifiedTenant"/> — authenticates the opportunity-creation
    /// gate test.</summary>
    public UserEntity UnverifiedVenueManager { get; }

    public IReadOnlyList<ArtistEntity> Artists { get; }
    public IReadOnlyList<VenueEntity> Venues { get; }

    public IReadOnlyList<DealEntity> Deals { get; }
    public IReadOnlyList<OpportunityEntity> Opportunities { get; }
    public IReadOnlyList<BookingEntity> Bookings { get; }
    public IReadOnlyList<ContractEntity> Contracts { get; }
    public IReadOnlyList<ApplicationEntity> Applications { get; }
    public IReadOnlyList<ConcertAvailabilityEntity> ConcertAvailabilities { get; }
    public IReadOnlyList<ConcertEntity> Concerts { get; }

    public FlatFeeDealEntity FlatFeeAppDeal { get; }
    public FlatFeeDealEntity ConfirmedAppDeal { get; }
    public FlatFeeDealEntity AwaitingPaymentAppDeal { get; }
    public VersusDealEntity VersusAppDeal { get; }
    public DoorSplitDealEntity DoorSplitAppDeal { get; }
    public VenueHireDealEntity VenueHireAppDeal { get; }
    public FlatFeeDealEntity PostedFlatFeeAppDeal { get; }
    public DoorSplitDealEntity PostedDoorSplitAppDeal { get; }
    public VersusDealEntity PostedVersusAppDeal { get; }
    public VenueHireDealEntity PostedVenueHireAppDeal { get; }
    public VersusDealEntity PastVersusAppDeal { get; }
    public FlatFeeDealEntity PastFlatFeeAppDeal { get; }
    public VenueHireDealEntity PastVenueHireAppDeal { get; }
    public DoorSplitDealEntity PastDoorSplitAppDeal { get; }

    public OpportunityEntity ActiveVenueHireOpportunity { get; }

    public ApplicationEntity FlatFeeApp { get; }
    public ApplicationEntity InProgressApplication { get; }
    public ApplicationEntity VersusApp { get; }
    public ApplicationEntity DoorSplitApp { get; }
    public ApplicationEntity VenueHireApp { get; }

    public ApplicationEntity ConfirmedApp { get; }
    public BookingEntity ConfirmedBooking { get; }

    public ApplicationEntity AwaitingPaymentApp { get; }
    public BookingEntity AwaitingPaymentBooking { get; }

    public ApplicationEntity PostedFlatFeeApp { get; }
    public BookingEntity PostedFlatFeeBooking { get; }

    public ApplicationEntity PostedDoorSplitApp { get; }
    public BookingEntity PostedDoorSplitBooking { get; }

    public ApplicationEntity PostedVersusApp { get; }
    public BookingEntity PostedVersusBooking { get; }

    public ApplicationEntity PostedVenueHireApp { get; }
    public BookingEntity PostedVenueHireBooking { get; }

    public ApplicationEntity FinishedDoorSplitApp { get; }
    public BookingEntity FinishedDoorSplitBooking { get; }

    public ApplicationEntity FinishedVersusApp { get; }
    public BookingEntity FinishedVersusBooking { get; }

    public ApplicationEntity PastVersusApp { get; }
    public BookingEntity PastVersusBooking { get; }

    public ApplicationEntity PastFlatFeeApp { get; }
    public BookingEntity PastFlatFeeBooking { get; }

    public ApplicationEntity PastVenueHireApp { get; }
    public BookingEntity PastVenueHireBooking { get; }

    public ApplicationEntity PastDoorSplitApp { get; }
    public BookingEntity PastDoorSplitBooking { get; }

    public ApplicationEntity UpcomingFlatFeeApp { get; }
    public BookingEntity UpcomingFlatFeeBooking { get; }

    public ApplicationEntity UpcomingVenueHireApp { get; }
    public BookingEntity UpcomingVenueHireBooking { get; }

    public SeedState(SeedCatalog catalog)
    {
        var now = catalog.Now;

        ArtistManager1 = UserFactory.FromRegistration(
            SeedUsers.ArtistManagerId(1), SeedUsers.ArtistManagerEmail(1));
        ArtistManagerNoArtist = UserFactory.FromRegistration(
            SeedUsers.ArtistManagerId(SeedUsers.ManagerCount),
            SeedUsers.ArtistManagerEmail(SeedUsers.ManagerCount));
        VenueManager1 = UserFactory.FromRegistration(
            SeedUsers.VenueManagerId(1), SeedUsers.VenueManagerEmail(1));
        VenueManager2 = UserFactory.FromRegistration(
            SeedUsers.VenueManagerId(2), SeedUsers.VenueManagerEmail(2));
        VenueManager3 = UserFactory.FromRegistration(
            SeedUsers.VenueManagerId(3), SeedUsers.VenueManagerEmail(3));
        VenueManagerNoVenue = UserFactory.FromRegistration(
            SeedUsers.VenueManagerId(SeedUsers.ManagerCount),
            SeedUsers.VenueManagerEmail(SeedUsers.ManagerCount));

        var artistManagers = new List<UserEntity> { ArtistManager1 };
        for (int i = 2; i < SeedUsers.ManagerCount; i++)
            artistManagers.Add(UserFactory.FromRegistration(
                SeedUsers.ArtistManagerId(i), SeedUsers.ArtistManagerEmail(i)));
        artistManagers.Add(ArtistManagerNoArtist);
        ArtistManagers = artistManagers;

        var venueManagers = new List<UserEntity> { VenueManager1, VenueManager2, VenueManager3 };
        for (int i = 4; i < SeedUsers.ManagerCount; i++)
            venueManagers.Add(UserFactory.FromRegistration(
                SeedUsers.VenueManagerId(i), SeedUsers.VenueManagerEmail(i)));
        venueManagers.Add(VenueManagerNoVenue);
        VenueManagers = venueManagers;

        Admin = UserFactory.FromRegistration(SeedUsers.Admin, SeedUsers.AdminEmail,
            new Point(-0.5, 51.0) { SRID = 4326 },
            new Address("Leicestershire", "Loughborough"),
            "avatar.jpg");

        UnverifiedVenueManager = UserFactory.FromRegistration(
            UnverifiedTenantUserId, "tenant-verification-gate@test.com");
        // Tax-complete but never submitted for verification, unlike SeedUsers' bare operators below (also
        // tax-incomplete) — isolates the verification gate from the tax-compliance gate. See the property doc.
        UnverifiedTenant = TenantFactory.Create(
            UnverifiedTenantUserId, "tenant-verification-gate@test.com", TenantType.Venue, now,
            taxComplianceComplete: true);

        Users = [Admin, .. ArtistManagers, .. VenueManagers, UnverifiedVenueManager];

        Venues = catalog.Venues.Select(s => VenueFactory.Create(
            id: s.VenueId, userId: s.UserId,
            name: s.Name, about: s.About,
            bannerUrl: s.BannerUrl, avatar: s.Avatar,
            location: new Point(s.Longitude, s.Latitude) { SRID = 4326 },
            address: new Address(s.County, s.Town),
            email: s.Email)).ToList();
        Venue = Venues[0];

        // TenantId is assigned below by the same generic tenantByVenueId loop every other venue goes through.
        var unverifiedVenue = VenueFactory.Create(
            id: 9001, userId: UnverifiedTenantUserId,
            name: "Unverified Venue", about: "Seeded for the tenant-verification gate test.",
            bannerUrl: "grandvenue.jpg", avatar: "avatar.jpg",
            location: new Point(0.0, 51.0) { SRID = 4326 },
            address: new Address("Test County", "Test Town"),
            email: "tenant-verification-gate@test.com");
        Venues = [.. Venues, unverifiedVenue];

        Artists = catalog.Artists.Select(s => ArtistFactory.Create(
            id: s.ArtistId, userId: s.UserId,
            name: s.Name, about: s.About,
            bannerUrl: s.BannerUrl, avatar: s.Avatar,
            location: new Point(s.Longitude, s.Latitude) { SRID = 4326 },
            address: new Address(s.County, s.Town),
            email: s.Email,
            genres: s.Genres)).ToList();
        foreach (var artist in Artists)
            artist.TenantId = TenantSeedIds.For(artist.UserId);
        Artist = Artists[0];

        ConfirmedAppDeal = FlatFeeDealFactory.Create(6, 200m);
        PostedVenueHireAppDeal = VenueHireDealFactory.Create(21, 300m);
        PostedFlatFeeAppDeal = FlatFeeDealFactory.Create(31, 200m);
        AwaitingPaymentAppDeal = FlatFeeDealFactory.Create(33, 150m);
        DoorSplitAppDeal = DoorSplitDealFactory.Create(50, 70m);
        VersusAppDeal = VersusDealFactory.Create(51, 100m, 70m);
        PostedDoorSplitAppDeal = DoorSplitDealFactory.Create(53, 65m);
        PostedVersusAppDeal = VersusDealFactory.Create(54, 120m, 60m);
        FlatFeeAppDeal = FlatFeeDealFactory.Create(55, 180m);
        VenueHireAppDeal = VenueHireDealFactory.Create(52, 170m);
        PastVersusAppDeal = VersusDealFactory.Create(64, 100m, 70m);
        PastFlatFeeAppDeal = FlatFeeDealFactory.Create(65, 200m);
        PastVenueHireAppDeal = VenueHireDealFactory.Create(66, 300m);
        PastDoorSplitAppDeal = DoorSplitDealFactory.Create(67, 70m);

        var deals = new List<DealEntity>
        {
            FlatFeeDealFactory.Create(1, 150m),
            FlatFeeDealFactory.Create(2, 120m),
            DoorSplitDealFactory.Create(3, 60m),
            VersusDealFactory.Create(4, 80m, 50m),
            FlatFeeDealFactory.Create(5, 180m),
            ConfirmedAppDeal,                                                       // 6
            FlatFeeDealFactory.Create(7, 160m),
            FlatFeeDealFactory.Create(8, 140m),
            DoorSplitDealFactory.Create(9, 70m),
            VenueHireDealFactory.Create(10, 250m),
            FlatFeeDealFactory.Create(11, 170m),
            VersusDealFactory.Create(12, 100m, 60m),
            FlatFeeDealFactory.Create(13, 150m),
            DoorSplitDealFactory.Create(14, 65m),
            FlatFeeDealFactory.Create(15, 190m),
            VenueHireDealFactory.Create(16, 220m),
            FlatFeeDealFactory.Create(17, 155m),
            VersusDealFactory.Create(18, 90m, 55m),
            DoorSplitDealFactory.Create(19, 60m),
            FlatFeeDealFactory.Create(20, 165m),
            PostedVenueHireAppDeal,                                                 // 21
            FlatFeeDealFactory.Create(22, 175m),
            DoorSplitDealFactory.Create(23, 70m),
            VersusDealFactory.Create(24, 110m, 60m),
            FlatFeeDealFactory.Create(25, 185m),
            FlatFeeDealFactory.Create(26, 195m),
            DoorSplitDealFactory.Create(27, 65m),
            VenueHireDealFactory.Create(28, 280m),
            VersusDealFactory.Create(29, 95m, 55m),
            FlatFeeDealFactory.Create(30, 160m),
            PostedFlatFeeAppDeal,                                                   // 31
            FlatFeeDealFactory.Create(32, 140m),
            AwaitingPaymentAppDeal,                                                 // 33
            DoorSplitDealFactory.Create(34, 70m),
            VersusDealFactory.Create(35, 100m, 60m),
            FlatFeeDealFactory.Create(36, 170m),
            VenueHireDealFactory.Create(37, 240m),
            DoorSplitDealFactory.Create(38, 60m),
            FlatFeeDealFactory.Create(39, 180m),
            VersusDealFactory.Create(40, 120m, 65m),
            FlatFeeDealFactory.Create(41, 155m),
            DoorSplitDealFactory.Create(42, 70m),
            VenueHireDealFactory.Create(43, 260m),
            FlatFeeDealFactory.Create(44, 190m),
            VersusDealFactory.Create(45, 105m, 55m),
            FlatFeeDealFactory.Create(46, 165m),
            DoorSplitDealFactory.Create(47, 65m),
            VenueHireDealFactory.Create(48, 290m),
            VersusDealFactory.Create(49, 85m, 50m),
            DoorSplitAppDeal,                                                       // 50
            VersusAppDeal,                                                          // 51
            VenueHireAppDeal,                                                       // 52
            PostedDoorSplitAppDeal,                                                 // 53
            PostedVersusAppDeal,                                                    // 54
            FlatFeeAppDeal,                                                         // 55
            DoorSplitDealFactory.Create(56, 70m),
            VersusDealFactory.Create(57, 110m, 65m),
            FlatFeeDealFactory.Create(58, 150m),
            VenueHireDealFactory.Create(59, 300m),
            FlatFeeDealFactory.Create(60, 200m),
            DoorSplitDealFactory.Create(61, 70m),
            VersusDealFactory.Create(62, 100m, 60m),
            VenueHireDealFactory.Create(63, 250m),
            PastVersusAppDeal,                                                      // 64
            PastFlatFeeAppDeal,                                                     // 65
            PastVenueHireAppDeal,                                                   // 66
            PastDoorSplitAppDeal,                                                   // 67
        };
        Deals = deals;

        var opps = new List<OpportunityEntity>();
        var oppSpecs = new (int VenueId, int DaysOffset)[]
        {
            (1, -60), (2, -55), (3, -50), (4, -45), (5, -40),
            (6, -35), (7, -30), (8, -25), (9, -20), (10, -15),
            (1, -10), (2, -5), (3, 0), (4, 5), (5, 10),
            (6, 15), (7, 20), (8, 25), (9, 30), (10, 35),
            (1, -40), (2, 45), (3, 50), (4, 55), (5, 60),
            (6, 65), (7, 70), (8, 75), (9, 80), (10, 85),
            (1, -85), (1, 85), (1, 2), (1, 4), (1, 6),
            (2, 8), (2, 10), (2, 12), (3, 14), (3, 16),
            (3, 18), (4, 22), (5, 24), (6, 26), (1, 30),
            (1, 32), (1, 34), (1, 36), (1, 38), (1, -60),
            (1, -90), (1, 120), (1, 150), (1, 180), (1, 200),
            (1, 210), (1, 220), (1, 15), (1, 20), (1, 40),
            (1, 42), (1, 44), (1, 46), (1, -120), (1, -85),
            (1, -40), (1, -60),
        };
        for (int i = 0; i < oppSpecs.Length; i++)
        {
            var (venueId, days) = oppSpecs[i];
            var hours = i == 31 ? 5 : 3;
            opps.Add(OpportunityFactory.Create(
                i + 1,
                venueId,
                new DateRange(now.AddDays(days), now.AddDays(days).AddHours(hours)),
                dealId: Deals[i].Id));
        }
        Opportunities = opps;

        // Artists get a tenant too (they own no Bucket-A rows) so Payment provisions their Connect account off PayoutOwnerRegisteredEvent.
        // The "no venue"/"no artist" operators registered but never set up their organization, so their tenants stay
        // tax-incomplete (no tax details captured) — the pre-org-setup state the organization read + gate tests rely on.
        var bareTenantUserIds = new HashSet<Guid> { VenueManagerNoVenue.Id, ArtistManagerNoArtist.Id };
        Tenants = SeedUsers.Managers
            .Select(m => TenantFactory.Create(
                m.Id, m.Email, m.Kind == ManagerKind.Venue ? TenantType.Venue : TenantType.Artist, now,
                taxComplianceComplete: !bareTenantUserIds.Contains(m.Id)))
            .ToList();
        Verifications = SeedUsers.Managers
            .Zip(Tenants)
            .Where(pair => !bareTenantUserIds.Contains(pair.First.Id))
            .Select(pair => VerificationFactory.Approved(pair.Second.Id, now))
            .ToList();

        // UnverifiedTenant (built above, alongside UnverifiedVenueManager) is not one of SeedUsers.Managers,
        // so it needs its own Tenants entry and founding-owner membership.
        Tenants = [.. Tenants, UnverifiedTenant];
        var memberships = SeedUsers.Managers
            .Select(m => MembershipFactory.FoundingOwner(m.TenantId, m.Id, now))
            .ToList();
        memberships.Add(MembershipFactory.FoundingOwner(UnverifiedTenant.Id, UnverifiedTenantUserId, now));
        // VenueManager3 is also a member of VenueManager1's tenant, giving one tenant two members for the group-inbox tests.
        memberships.Add(MembershipFactory.Member(
            TenantSeedIds.For(VenueManager1.Id), VenueManager3.Id, TenantRole.Manager, invitedBy: VenueManager1.Id, now));
        Memberships = memberships;
        var tenantByVenueId = Venues.ToDictionary(v => v.Id, v => TenantSeedIds.For(v.UserId));
        foreach (var venue in Venues)
            venue.TenantId = tenantByVenueId[venue.Id];
        foreach (var opportunity in Opportunities)
            opportunity.TenantId = tenantByVenueId[opportunity.VenueId];
        var tenantByDealId = Opportunities
            .GroupBy(o => o.DealId)
            .ToDictionary(g => g.Key, g => g.First().TenantId);
        foreach (var deal in Deals)
            if (tenantByDealId.TryGetValue(deal.Id, out var tenantId))
                deal.TenantId = tenantId;

        ConfirmedApp = Link(1, ApplicationFactory.Accepted(1, 6));
        PostedDoorSplitApp = Link(2, ApplicationFactory.Accepted(1, 53));
        PostedVersusApp = Link(3, ApplicationFactory.Accepted(2, 54));
        PostedFlatFeeApp = Link(4, ApplicationFactory.Accepted(2, 31));
        PostedVenueHireApp = Link(5, ApplicationFactory.AcceptedPrepaid(1, 21));
        AwaitingPaymentApp = Link(6, ApplicationFactory.Accepted(1, 33));
        FinishedDoorSplitApp = Link(7, ApplicationFactory.Accepted(1, 50));
        FinishedVersusApp = Link(8, ApplicationFactory.Accepted(1, 51));
        PastVersusApp = Link(9, ApplicationFactory.Accepted(1, Opportunities[63].Id));
        PastFlatFeeApp = Link(10, ApplicationFactory.Accepted(1, Opportunities[64].Id));
        PastVenueHireApp = Link(11, ApplicationFactory.AcceptedPrepaid(1, Opportunities[65].Id));
        PastDoorSplitApp = Link(12, ApplicationFactory.Accepted(1, Opportunities[66].Id));
        UpcomingFlatFeeApp = Link(13, ApplicationFactory.Accepted(2, 58));
        UpcomingVenueHireApp = Link(14, ApplicationFactory.AcceptedPrepaid(1, 59));

        DoorSplitApp = ApplicationFactory.Create(1, Opportunities[55].Id, Deals[55].DealType);
        VersusApp = ApplicationFactory.Create(1, Opportunities[56].Id, Deals[56].DealType);
        VenueHireApp = ApplicationFactory.CreatePrepaid(1, Opportunities[51].Id, Deals[51].DealType);
        FlatFeeApp = ApplicationFactory.Create(1, Opportunities[54].Id, Deals[54].DealType);
        InProgressApplication = ApplicationFactory.Create(1, Opportunities[12].Id, Deals[12].DealType);

        Applications =
        [
            Link(15, ApplicationFactory.Accepted(1, 1)),
            Link(16, ApplicationFactory.Accepted(2, 1)),
            Link(17, ApplicationFactory.Accepted(3, 1)),
            Link(18, ApplicationFactory.Accepted(4, 1)),
            Link(19, ApplicationFactory.Accepted(1, 2)),
            Link(20, ApplicationFactory.Accepted(2, 2)),
            Link(21, ApplicationFactory.Accepted(5, 2)),
            Link(22, ApplicationFactory.Accepted(6, 2)),
            Link(23, ApplicationFactory.Accepted(1, 3)),
            Link(24, ApplicationFactory.Accepted(2, 3)),
            Link(25, ApplicationFactory.Accepted(7, 3)),
            Link(26, ApplicationFactory.Accepted(8, 3)),
            Link(27, ApplicationFactory.Accepted(1, 4)),
            Link(28, ApplicationFactory.Accepted(2, 4)),
            Link(29, ApplicationFactory.Accepted(9, 4)),
            Link(30, ApplicationFactory.Accepted(10, 4)),
            Link(31, ApplicationFactory.Accepted(1, 5)),
            Link(32, ApplicationFactory.Accepted(2, 5)),
            Link(33, ApplicationFactory.Accepted(11, 5)),
            Link(34, ApplicationFactory.Accepted(12, 5)),
            ConfirmedApp,
            Link(35, ApplicationFactory.Accepted(2, 6)),
            Link(36, ApplicationFactory.Accepted(13, 6)),
            Link(37, ApplicationFactory.Accepted(14, 6)),
            Link(38, ApplicationFactory.Accepted(1, 7)),
            Link(39, ApplicationFactory.Accepted(2, 7)),
            ApplicationFactory.Create(15, 7),
            ApplicationFactory.Create(16, 7),
            ApplicationFactory.Create(1, 8),
            ApplicationFactory.Create(2, 8),
            ApplicationFactory.Create(17, 8),
            ApplicationFactory.Create(18, 8),
            ApplicationFactory.Create(17, 40),
            ApplicationFactory.Create(18, 41),
            Link(40, ApplicationFactory.Accepted(1, 14)),
            ApplicationFactory.Create(2, 14),
            ApplicationFactory.Create(3, 14),
            ApplicationFactory.Create(4, 14),
            PostedDoorSplitApp,
            DoorSplitApp,
            ApplicationFactory.Create(7, 15),
            Link(41, ApplicationFactory.Accepted(8, 15)),
            ApplicationFactory.CreatePrepaid(9, 16),
            ApplicationFactory.CreatePrepaid(10, 16),
            Link(42, ApplicationFactory.AcceptedPrepaid(11, 16)),
            ApplicationFactory.CreatePrepaid(12, 16),
            VersusApp,
            ApplicationFactory.Create(14, 17),
            PostedVersusApp,
            ApplicationFactory.Create(16, 17),
            ApplicationFactory.Create(1, 34),
            ApplicationFactory.Create(2, 34),
            ApplicationFactory.Create(19, 34),
            ApplicationFactory.Create(20, 34),
            ApplicationFactory.Create(1, 38),
            ApplicationFactory.Create(2, 38),
            ApplicationFactory.Create(12, 38),
            ApplicationFactory.Create(4, 38),
            ApplicationFactory.Create(1, 45),
            ApplicationFactory.Create(2, 46),
            ApplicationFactory.Create(3, 47),
            ApplicationFactory.CreatePrepaid(4, 48),
            ApplicationFactory.Create(5, 49),
            ApplicationFactory.Create(2, 50),
            ApplicationFactory.Create(2, 51),
            VenueHireApp,
            ApplicationFactory.CreatePrepaid(2, 52),
            FlatFeeApp,
            PostedFlatFeeApp,
            ApplicationFactory.Create(3, 31),
            ApplicationFactory.Create(1, 32),
            ApplicationFactory.Create(2, 32),
            ApplicationFactory.Create(3, 32),
            AwaitingPaymentApp,
            PostedVenueHireApp,
            FinishedDoorSplitApp,
            FinishedVersusApp,
            PastVersusApp,
            PastFlatFeeApp,
            PastVenueHireApp,
            PastDoorSplitApp,
            UpcomingFlatFeeApp,
            UpcomingVenueHireApp,
            Link(43, ApplicationFactory.Accepted(3, 34)),
            ApplicationFactory.Create(4, 34),
            ApplicationFactory.Create(5, 34),
            Link(44, ApplicationFactory.Accepted(1, 35)),
            ApplicationFactory.Create(2, 35),
            ApplicationFactory.Create(4, 35),
            ApplicationFactory.Create(5, 35),
            Link(45, ApplicationFactory.Accepted(4, 46)),
            ApplicationFactory.Create(5, 46),
            ApplicationFactory.Create(6, 46),
            Link(46, ApplicationFactory.Accepted(5, 47)),
            ApplicationFactory.Create(6, 47),
            ApplicationFactory.Create(7, 47),
            Link(47, ApplicationFactory.AcceptedPrepaid(6, 48)),
            ApplicationFactory.CreatePrepaid(7, 48),
            ApplicationFactory.CreatePrepaid(8, 48),
            InProgressApplication,
        ];

        for (var i = 0; i < Applications.Count; i++)
            Applications[i].WithId(i + 1);

        var nextDealId = deals.Max(deal => deal.Id) + 1;
        var nextOpportunityId = opps.Max(opportunity => opportunity.Id) + 1;
        foreach (var group in bookingApplications
                     .OrderBy(pair => pair.Key)
                     .GroupBy(pair => pair.Value.OpportunityId))
        {
            foreach (var duplicate in group.Skip(1))
            {
                var sourceOpportunity = opps.Single(opportunity => opportunity.Id == group.Key);
                var sourceDeal = deals.Single(deal => deal.Id == sourceOpportunity.DealId);
                var deal = DealFactory.Clone(nextDealId++, sourceDeal);
                var opportunity = OpportunityFactory.Create(
                    nextOpportunityId++,
                    sourceOpportunity.VenueId,
                    sourceOpportunity.Period,
                    deal.Id,
                    sourceOpportunity.Genres);
                opportunity.TenantId = sourceOpportunity.TenantId;
                deals.Add(deal);
                opps.Add(opportunity);
                duplicate.Value.With(nameof(ApplicationEntity.OpportunityId), opportunity.Id);
            }
        }

        var artistById = Artists.ToDictionary(artist => artist.Id);
        var venueById = Venues.ToDictionary(venue => venue.Id);
        var opportunityById = opps.ToDictionary(opportunity => opportunity.Id);
        var dealById = deals.ToDictionary(deal => deal.Id);
        ActiveVenueHireOpportunity = opps
            .Where(opportunity => opportunity.Period.Start >= now)
            .Where(opportunity => dealById[opportunity.DealId] is VenueHireDealEntity)
            .Where(opportunity => Applications.All(application =>
                application.OpportunityId != opportunity.Id))
            .OrderBy(opportunity => opportunity.Period.Start)
            .First();
        foreach (var application in Applications)
        {
            var opportunity = opportunityById[application.OpportunityId];
            ApplicationFactory.FinishConstruction(
                application,
                artistById[application.ArtistId],
                opportunity,
                dealById[opportunity.DealId],
                now);
        }

        var bookingAggregates = bookingApplications
            .OrderBy(pair => pair.Key)
            .Select(pair =>
            {
                var application = pair.Value;
                var opportunity = opportunityById[application.OpportunityId];
                var accepted = ApplicationFactory.ToAcceptedApplication(
                    application,
                    artistById[application.ArtistId],
                    venueById[opportunity.VenueId],
                    opportunity,
                    dealById[opportunity.DealId],
                    now,
                    OperationIdFor(pair.Key));
                return BookingFactory.Create(
                    pair.Key,
                    accepted,
                    now,
                    confirmed: pair.Key != 6);
            })
            .ToList();
        Bookings = bookingAggregates.Select(aggregate => aggregate.Booking).ToList();
        Contracts = bookingAggregates.Select(aggregate => aggregate.Contract).ToList();

        ConfirmedBooking = Bookings[0];
        PostedDoorSplitBooking = Bookings[1];
        PostedVersusBooking = Bookings[2];
        PostedFlatFeeBooking = Bookings[3];
        PostedVenueHireBooking = Bookings[4];
        AwaitingPaymentBooking = Bookings[5];
        FinishedDoorSplitBooking = Bookings[6];
        FinishedVersusBooking = Bookings[7];
        PastVersusBooking = Bookings[8];
        PastFlatFeeBooking = Bookings[9];
        PastVenueHireBooking = Bookings[10];
        PastDoorSplitBooking = Bookings[11];
        UpcomingFlatFeeBooking = Bookings[12];
        UpcomingVenueHireBooking = Bookings[13];

        Concerts = catalog.Concerts
            .Where(spec => spec.ConcertId != AwaitingPaymentBooking.Id)
            .Select(spec => ConcertFactory.Create(spec, Bookings[spec.ConcertId - 1]))
            .ToList();
        ConcertAvailabilities = Concerts.Select(concert => ConcertAvailabilityEntity.Create(
            concert.Id,
            concert.OpportunityId,
            concert.ArtistId,
            concert.VenueId,
            concert.VenueTenantId,
            concert.ArtistTenantId,
            concert.Period.Start)).ToList();
    }

    public ConcertEntity ConcertFor(BookingEntity booking) =>
        Concerts.Single(concert => concert.BookingId == booking.Id);

    private TApplication Link<TApplication>(int bookingId, TApplication application)
        where TApplication : ApplicationEntity
    {
        bookingApplications.Add(bookingId, application);
        return application;
    }

    private static Guid OperationIdFor(int bookingId) =>
        Guid.Parse($"00000000-0000-0000-0000-{bookingId:D12}");
}
