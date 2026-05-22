using Concertable.DataAccess;
using Concertable.Concert.Infrastructure.Data;
using Concertable.Seeding;
using Concertable.Seeding.Extensions;
using Concertable.Seeding.Factories;
using Concertable.Seeding.Fakers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Concert.Infrastructure.Data.Seeders;

internal class ConcertDevSeeder : IDevSeeder
{
    public int Order => 4;

    private readonly ConcertDbContext context;
    private readonly SeedData seed;
    private readonly TimeProvider timeProvider;

    public ConcertDevSeeder(ConcertDbContext context, SeedData seed, TimeProvider timeProvider)
    {
        this.context = context;
        this.seed = seed;
        this.timeProvider = timeProvider;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var artistManagerIds = seed.ArtistManagerIds;

        await context.Opportunities.SeedIfEmptyAsync(async () =>
        {
            var contracts = seed.Contracts;
            seed.Opportunities =
            [
                OpportunityFactory.Create(1, new DateRange(now.AddDays(-60), now.AddDays(-60).AddHours(3)), contractId: contracts[0].Id),
                OpportunityFactory.Create(2, new DateRange(now.AddDays(-55), now.AddDays(-55).AddHours(3)), contractId: contracts[1].Id),
                OpportunityFactory.Create(3, new DateRange(now.AddDays(-50), now.AddDays(-50).AddHours(3)), contractId: contracts[2].Id),
                OpportunityFactory.Create(4, new DateRange(now.AddDays(-45), now.AddDays(-45).AddHours(3)), contractId: contracts[3].Id),
                OpportunityFactory.Create(5, new DateRange(now.AddDays(-40), now.AddDays(-40).AddHours(3)), contractId: contracts[4].Id),
                OpportunityFactory.Create(6, new DateRange(now.AddDays(-35), now.AddDays(-35).AddHours(3)), contractId: contracts[5].Id),
                OpportunityFactory.Create(7, new DateRange(now.AddDays(-30), now.AddDays(-30).AddHours(3)), contractId: contracts[6].Id),
                OpportunityFactory.Create(8, new DateRange(now.AddDays(-25), now.AddDays(-25).AddHours(3)), contractId: contracts[7].Id),
                OpportunityFactory.Create(9, new DateRange(now.AddDays(-20), now.AddDays(-20).AddHours(3)), contractId: contracts[8].Id),
                OpportunityFactory.Create(10, new DateRange(now.AddDays(-15), now.AddDays(-15).AddHours(3)), contractId: contracts[9].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(-10), now.AddDays(-10).AddHours(3)), contractId: contracts[10].Id),
                OpportunityFactory.Create(2, new DateRange(now.AddDays(-5), now.AddDays(-5).AddHours(3)), contractId: contracts[11].Id),
                OpportunityFactory.Create(3, new DateRange(now, now.AddHours(3)), contractId: contracts[12].Id),
                OpportunityFactory.Create(4, new DateRange(now.AddDays(5), now.AddDays(5).AddHours(3)), contractId: contracts[13].Id),
                OpportunityFactory.Create(5, new DateRange(now.AddDays(10), now.AddDays(10).AddHours(3)), contractId: contracts[14].Id),
                OpportunityFactory.Create(6, new DateRange(now.AddDays(15), now.AddDays(15).AddHours(3)), contractId: contracts[15].Id),
                OpportunityFactory.Create(7, new DateRange(now.AddDays(20), now.AddDays(20).AddHours(3)), contractId: contracts[16].Id),
                OpportunityFactory.Create(8, new DateRange(now.AddDays(25), now.AddDays(25).AddHours(3)), contractId: contracts[17].Id),
                OpportunityFactory.Create(9, new DateRange(now.AddDays(30), now.AddDays(30).AddHours(3)), contractId: contracts[18].Id),
                OpportunityFactory.Create(10, new DateRange(now.AddDays(35), now.AddDays(35).AddHours(3)), contractId: contracts[19].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(-40), now.AddDays(-40).AddHours(3)), contractId: contracts[20].Id),
                OpportunityFactory.Create(2, new DateRange(now.AddDays(45), now.AddDays(45).AddHours(3)), contractId: contracts[21].Id),
                OpportunityFactory.Create(3, new DateRange(now.AddDays(50), now.AddDays(50).AddHours(3)), contractId: contracts[22].Id),
                OpportunityFactory.Create(4, new DateRange(now.AddDays(55), now.AddDays(55).AddHours(3)), contractId: contracts[23].Id),
                OpportunityFactory.Create(5, new DateRange(now.AddDays(60), now.AddDays(60).AddHours(3)), contractId: contracts[24].Id),
                OpportunityFactory.Create(6, new DateRange(now.AddDays(65), now.AddDays(65).AddHours(3)), contractId: contracts[25].Id),
                OpportunityFactory.Create(7, new DateRange(now.AddDays(70), now.AddDays(70).AddHours(3)), contractId: contracts[26].Id),
                OpportunityFactory.Create(8, new DateRange(now.AddDays(75), now.AddDays(75).AddHours(3)), contractId: contracts[27].Id),
                OpportunityFactory.Create(9, new DateRange(now.AddDays(80), now.AddDays(80).AddHours(3)), contractId: contracts[28].Id),
                OpportunityFactory.Create(10, new DateRange(now.AddDays(85), now.AddDays(85).AddHours(3)), contractId: contracts[29].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(-85), now.AddDays(-85).AddHours(3)), contractId: contracts[30].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(85), now.AddDays(85).AddHours(5)), contractId: contracts[31].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(2), now.AddDays(2).AddHours(3)), contractId: contracts[32].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(4), now.AddDays(4).AddHours(3)), contractId: contracts[33].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(6), now.AddDays(6).AddHours(3)), contractId: contracts[34].Id),
                OpportunityFactory.Create(2, new DateRange(now.AddDays(8), now.AddDays(8).AddHours(3)), contractId: contracts[35].Id),
                OpportunityFactory.Create(2, new DateRange(now.AddDays(10), now.AddDays(10).AddHours(3)), contractId: contracts[36].Id),
                OpportunityFactory.Create(2, new DateRange(now.AddDays(12), now.AddDays(12).AddHours(3)), contractId: contracts[37].Id),
                OpportunityFactory.Create(3, new DateRange(now.AddDays(14), now.AddDays(14).AddHours(3)), contractId: contracts[38].Id),
                OpportunityFactory.Create(3, new DateRange(now.AddDays(16), now.AddDays(16).AddHours(3)), contractId: contracts[39].Id),
                OpportunityFactory.Create(3, new DateRange(now.AddDays(18), now.AddDays(18).AddHours(3)), contractId: contracts[40].Id),
                OpportunityFactory.Create(4, new DateRange(now.AddDays(22), now.AddDays(22).AddHours(3)), contractId: contracts[41].Id),
                OpportunityFactory.Create(5, new DateRange(now.AddDays(24), now.AddDays(24).AddHours(3)), contractId: contracts[42].Id),
                OpportunityFactory.Create(6, new DateRange(now.AddDays(26), now.AddDays(26).AddHours(3)), contractId: contracts[43].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(30), now.AddDays(30).AddHours(3)), contractId: contracts[44].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(32), now.AddDays(32).AddHours(3)), contractId: contracts[45].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(34), now.AddDays(34).AddHours(3)), contractId: contracts[46].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(36), now.AddDays(36).AddHours(3)), contractId: contracts[47].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(38), now.AddDays(38).AddHours(3)), contractId: contracts[48].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(-60), now.AddDays(-60).AddHours(3)), contractId: contracts[49].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(-90), now.AddDays(-90).AddHours(3)), contractId: contracts[50].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(120), now.AddDays(120).AddHours(3)), contractId: contracts[51].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(150), now.AddDays(150).AddHours(3)), contractId: contracts[52].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(180), now.AddDays(180).AddHours(3)), contractId: contracts[53].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(200), now.AddDays(200).AddHours(3)), contractId: contracts[54].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(210), now.AddDays(210).AddHours(3)), contractId: contracts[55].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(220), now.AddDays(220).AddHours(3)), contractId: contracts[56].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(15), now.AddDays(15).AddHours(3)), contractId: contracts[57].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(20), now.AddDays(20).AddHours(3)), contractId: contracts[58].Id),

                OpportunityFactory.Create(1, new DateRange(now.AddDays(40), now.AddDays(40).AddHours(3)), contractId: contracts[59].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(42), now.AddDays(42).AddHours(3)), contractId: contracts[60].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(44), now.AddDays(44).AddHours(3)), contractId: contracts[61].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(46), now.AddDays(46).AddHours(3)), contractId: contracts[62].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(-120), now.AddDays(-120).AddHours(3)), contractId: contracts[63].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(-85), now.AddDays(-85).AddHours(3)), contractId: contracts[64].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(-40), now.AddDays(-40).AddHours(3)), contractId: contracts[65].Id),
                OpportunityFactory.Create(1, new DateRange(now.AddDays(-60), now.AddDays(-60).AddHours(3)), contractId: contracts[66].Id),
            ];

            context.Opportunities.AddRange(seed.Opportunities);
            await context.SaveChangesAsync(ct);
        });

        await context.Applications.SeedIfEmptyAsync(async () =>
        {
            seed.ConfirmedBooking = BookingFactory.Complete(ConcertFaker.GetFaker("Ultimate Dance Party", 27m, 160, 1, seed.Opportunities[5].VenueId, seed.Opportunities[5].Period.Start, seed.Opportunities[5].Period.End, now.AddDays(2)).Generate());
            seed.ConfirmedApp = ApplicationFactory.Accepted(1, 6, seed.ConfirmedBooking);

            seed.PostedDoorSplitBooking = BookingFactory.ConfirmedDeferred(ConcertFaker.GetFaker("Boogie Wonderland", 25m, 120, 1, seed.Opportunities[52].VenueId, seed.Opportunities[52].Period.Start, seed.Opportunities[52].Period.End, now.AddDays(150)).Generate());
            seed.PostedDoorSplitApp = ApplicationFactory.Accepted(1, 53, seed.PostedDoorSplitBooking);

            seed.PostedVersusBooking = BookingFactory.ConfirmedDeferred(ConcertFaker.GetFaker("Funk it up", 20m, 150, 2, seed.Opportunities[53].VenueId, seed.Opportunities[53].Period.Start, seed.Opportunities[53].Period.End, now.AddDays(180)).Generate());
            seed.PostedVersusApp = ApplicationFactory.Accepted(2, 54, seed.PostedVersusBooking);

            seed.PostedFlatFeeBooking = BookingFactory.Complete(ConcertFaker.GetFaker("Boogie it up!", 20m, 150, 2, seed.Opportunities[30].VenueId, seed.Opportunities[30].Period.Start, seed.Opportunities[30].Period.End, now.AddDays(-85)).Generate());
            seed.PostedFlatFeeApp = ApplicationFactory.Accepted(2, 31, seed.PostedFlatFeeBooking);

            seed.PostedVenueHireBooking = BookingFactory.Complete(ConcertFaker.GetFaker("VenueHire Spectacular", 30m, 200, 1, seed.Opportunities[20].VenueId, seed.Opportunities[20].Period.Start, seed.Opportunities[20].Period.End, now.AddDays(-40)).Generate());
            seed.PostedVenueHireApp = ApplicationFactory.AcceptedPrepaid(1, 21, seed.PostedVenueHireBooking);

            seed.FreshVenueHireOpportunity = seed.Opportunities[62];

            seed.DoorSplitApp = ApplicationFactory.Create(1, seed.Opportunities[55].Id, seed.Contracts[55].ContractType);
            seed.VersusApp = ApplicationFactory.Create(1, seed.Opportunities[56].Id, seed.Contracts[56].ContractType);
            seed.VenueHireApp = ApplicationFactory.CreatePrepaid(1, seed.Opportunities[51].Id, seed.Contracts[51].ContractType);
            seed.FlatFeeApp = ApplicationFactory.Create(1, seed.Opportunities[54].Id, seed.Contracts[54].ContractType);

            seed.AwaitingPaymentBooking = BookingFactory.AwaitingPayment(ConcertFaker.GetFaker("Awaiting Show", 15m, 100, 1, seed.Opportunities[32].VenueId, seed.Opportunities[32].Period.Start, seed.Opportunities[32].Period.End, now.AddDays(3)).Generate());
            seed.AwaitingPaymentApp = ApplicationFactory.Accepted(1, 33, seed.AwaitingPaymentBooking);

            seed.FinishedDoorSplitBooking = BookingFactory.CompleteDeferred(ConcertFaker.GetFaker("DoorSplit Settlement Show", 20m, 100, 1, seed.Opportunities[49].VenueId, seed.Opportunities[49].Period.Start, seed.Opportunities[49].Period.End, now.AddDays(-60)).Generate());
            seed.FinishedDoorSplitApp = ApplicationFactory.Accepted(1, 50, seed.FinishedDoorSplitBooking);

            seed.FinishedVersusBooking = BookingFactory.CompleteDeferred(ConcertFaker.GetFaker("Versus Settlement Show", 20m, 100, 1, seed.Opportunities[50].VenueId, seed.Opportunities[50].Period.Start, seed.Opportunities[50].Period.End, now.AddDays(-90)).Generate());
            seed.FinishedVersusApp = ApplicationFactory.Accepted(1, 51, seed.FinishedVersusBooking);

            seed.PastVersusBooking = BookingFactory.ConfirmedDeferred(ConcertFaker.GetFaker("Past Versus Show", 20m, 100, 1, seed.Opportunities[63].VenueId, seed.Opportunities[63].Period.Start, seed.Opportunities[63].Period.End, now.AddDays(-120)).Generate());
            seed.PastVersusApp = ApplicationFactory.Accepted(1, seed.Opportunities[63].Id, seed.PastVersusBooking);

            seed.PastFlatFeeBooking = BookingFactory.Confirmed(ConcertFaker.GetFaker("Past FlatFee Show", 20m, 100, 1, seed.Opportunities[64].VenueId, seed.Opportunities[64].Period.Start, seed.Opportunities[64].Period.End, now.AddDays(-85)).Generate());
            seed.PastFlatFeeApp = ApplicationFactory.Accepted(1, seed.Opportunities[64].Id, seed.PastFlatFeeBooking);

            seed.PastVenueHireBooking = BookingFactory.Confirmed(ConcertFaker.GetFaker("Past VenueHire Show", 30m, 100, 1, seed.Opportunities[65].VenueId, seed.Opportunities[65].Period.Start, seed.Opportunities[65].Period.End, now.AddDays(-40)).Generate());
            seed.PastVenueHireApp = ApplicationFactory.AcceptedPrepaid(1, seed.Opportunities[65].Id, seed.PastVenueHireBooking);

            seed.PastDoorSplitBooking = BookingFactory.ConfirmedDeferred(ConcertFaker.GetFaker("Past DoorSplit Show", 20m, 100, 1, seed.Opportunities[66].VenueId, seed.Opportunities[66].Period.Start, seed.Opportunities[66].Period.End, now.AddDays(-60)).Generate());
            seed.PastDoorSplitApp = ApplicationFactory.Accepted(1, seed.Opportunities[66].Id, seed.PastDoorSplitBooking);

            seed.UpcomingFlatFeeBooking = BookingFactory.Confirmed(ConcertFaker.GetFaker("Upcoming FlatFee Show", 20m, 150, 2, seed.Opportunities[57].VenueId, seed.Opportunities[57].Period.Start, seed.Opportunities[57].Period.End, now).Generate());
            seed.UpcomingFlatFeeApp = ApplicationFactory.Accepted(2, 58, seed.UpcomingFlatFeeBooking);

            seed.UpcomingVenueHireBooking = BookingFactory.Confirmed(ConcertFaker.GetFaker("Upcoming VenueHire Show", 30m, 200, 1, seed.Opportunities[58].VenueId, seed.Opportunities[58].Period.Start, seed.Opportunities[58].Period.End, now).Generate());
            seed.UpcomingVenueHireApp = ApplicationFactory.AcceptedPrepaid(1, 59, seed.UpcomingVenueHireBooking);

            var applications = new ApplicationEntity[]
            {
                // Apps 1-20: Complete (past concerts)
                ApplicationFactory.Accepted(1, 1, BookingFactory.Complete(ConcertFaker.GetFaker("Rockin' all Night", 15m, 120, 1, seed.Opportunities[0].VenueId, seed.Opportunities[0].Period.Start, seed.Opportunities[0].Period.End, now.AddDays(-58)).Generate())),
                ApplicationFactory.Accepted(2, 1, BookingFactory.Complete(ConcertFaker.GetFaker("Non Stop Party", 12m, 110, 2, seed.Opportunities[0].VenueId, seed.Opportunities[0].Period.Start, seed.Opportunities[0].Period.End, now.AddDays(-55)).Generate())),
                ApplicationFactory.Accepted(3, 1, BookingFactory.Complete(ConcertFaker.GetFaker("Super Mix", 18m, 130, 3, seed.Opportunities[0].VenueId, seed.Opportunities[0].Period.Start, seed.Opportunities[0].Period.End, now.AddDays(-52)).Generate())),
                ApplicationFactory.Accepted(4, 1, BookingFactory.Complete(ConcertFaker.GetFaker("Hip-Hop till you flip-flop", 10m, 100, 4, seed.Opportunities[0].VenueId, seed.Opportunities[0].Period.Start, seed.Opportunities[0].Period.End, now.AddDays(-49)).Generate())),
                ApplicationFactory.Accepted(1, 2, BookingFactory.Complete(ConcertFaker.GetFaker("Dance the night away", 25m, 140, 1, seed.Opportunities[1].VenueId, seed.Opportunities[1].Period.Start, seed.Opportunities[1].Period.End, now.AddDays(-46)).Generate())),
                ApplicationFactory.Accepted(2, 2, BookingFactory.Complete(ConcertFaker.GetFaker("Dizzy One", 20m, 150, 2, seed.Opportunities[1].VenueId, seed.Opportunities[1].Period.Start, seed.Opportunities[1].Period.End, now.AddDays(-43)).Generate())),
                ApplicationFactory.Accepted(5, 2, BookingFactory.Complete(ConcertFaker.GetFaker("Beers and Boombox", 30m, 170, 5, seed.Opportunities[1].VenueId, seed.Opportunities[1].Period.Start, seed.Opportunities[1].Period.End, now.AddDays(-40)).Generate())),
                ApplicationFactory.Accepted(6, 2, BookingFactory.Complete(ConcertFaker.GetFaker("Rockin' Tonight!", 16m, 130, 6, seed.Opportunities[1].VenueId, seed.Opportunities[1].Period.Start, seed.Opportunities[1].Period.End, now.AddDays(-37)).Generate())),
                ApplicationFactory.Accepted(1, 3, BookingFactory.Complete(ConcertFaker.GetFaker("Groovin' All Night", 14m, 115, 1, seed.Opportunities[2].VenueId, seed.Opportunities[2].Period.Start, seed.Opportunities[2].Period.End, now.AddDays(-34)).Generate())),
                ApplicationFactory.Accepted(2, 3, BookingFactory.Complete(ConcertFaker.GetFaker("Nonstop Vibes", 22m, 135, 2, seed.Opportunities[2].VenueId, seed.Opportunities[2].Period.Start, seed.Opportunities[2].Period.End, now.AddDays(-31)).Generate())),
                ApplicationFactory.Accepted(7, 3, BookingFactory.Complete(ConcertFaker.GetFaker("Electric Dreams", 13m, 125, 7, seed.Opportunities[2].VenueId, seed.Opportunities[2].Period.Start, seed.Opportunities[2].Period.End, now.AddDays(-28)).Generate())),
                ApplicationFactory.Accepted(8, 3, BookingFactory.Complete(ConcertFaker.GetFaker("Beat Drop Frenzy", 11m, 120, 8, seed.Opportunities[2].VenueId, seed.Opportunities[2].Period.Start, seed.Opportunities[2].Period.End, now.AddDays(-25)).Generate())),
                ApplicationFactory.Accepted(1, 4, BookingFactory.Complete(ConcertFaker.GetFaker("Summer Jam", 19m, 140, 1, seed.Opportunities[3].VenueId, seed.Opportunities[3].Period.Start, seed.Opportunities[3].Period.End, now.AddDays(-22)).Generate())),
                ApplicationFactory.Accepted(2, 4, BookingFactory.Complete(ConcertFaker.GetFaker("Midnight Madness", 17m, 135, 2, seed.Opportunities[3].VenueId, seed.Opportunities[3].Period.Start, seed.Opportunities[3].Period.End, now.AddDays(-19)).Generate())),
                ApplicationFactory.Accepted(9, 4, BookingFactory.Complete(ConcertFaker.GetFaker("Like a Boss", 21m, 145, 9, seed.Opportunities[3].VenueId, seed.Opportunities[3].Period.Start, seed.Opportunities[3].Period.End, now.AddDays(-16)).Generate())),
                ApplicationFactory.Accepted(10, 4, BookingFactory.Complete(ConcertFaker.GetFaker("Lights and Sound", 18m, 140, 10, seed.Opportunities[3].VenueId, seed.Opportunities[3].Period.Start, seed.Opportunities[3].Period.End, now.AddDays(-13)).Generate())),
                ApplicationFactory.Accepted(1, 5, BookingFactory.Complete(ConcertFaker.GetFaker("Rhythm Nation", 26m, 155, 1, seed.Opportunities[4].VenueId, seed.Opportunities[4].Period.Start, seed.Opportunities[4].Period.End, now.AddDays(-10)).Generate())),
                ApplicationFactory.Accepted(2, 5, BookingFactory.Complete(ConcertFaker.GetFaker("Bass Drop Party", 15m, 120, 2, seed.Opportunities[4].VenueId, seed.Opportunities[4].Period.Start, seed.Opportunities[4].Period.End, now.AddDays(-7)).Generate())),
                ApplicationFactory.Accepted(11, 5, BookingFactory.Complete(ConcertFaker.GetFaker("Chill & Thrill", 28m, 160, 11, seed.Opportunities[4].VenueId, seed.Opportunities[4].Period.Start, seed.Opportunities[4].Period.End, now.AddDays(-4)).Generate())),
                ApplicationFactory.Accepted(12, 5, BookingFactory.Complete(ConcertFaker.GetFaker("Vibin' till Night", 24m, 150, 12, seed.Opportunities[4].VenueId, seed.Opportunities[4].Period.Start, seed.Opportunities[4].Period.End, now.AddDays(-1)).Generate())),
                // Apps 21-26: Accepted (upcoming concerts)
                seed.ConfirmedApp,
                ApplicationFactory.Accepted(2, 6, BookingFactory.Complete(ConcertFaker.GetFaker("Rock Your Soul", 23m, 130, 2, seed.Opportunities[5].VenueId, seed.Opportunities[5].Period.Start, seed.Opportunities[5].Period.End, now.AddDays(5)).Generate())),
                ApplicationFactory.Accepted(13, 6, BookingFactory.Complete(ConcertFaker.GetFaker("Danceaway", 29m, 155, 13, seed.Opportunities[5].VenueId, seed.Opportunities[5].Period.Start, seed.Opportunities[5].Period.End, now.AddDays(8)).Generate())),
                ApplicationFactory.Accepted(14, 6, BookingFactory.Complete(ConcertFaker.GetFaker("Bassline Groove Beats", 10m, 110, 14, seed.Opportunities[5].VenueId, seed.Opportunities[5].Period.Start, seed.Opportunities[5].Period.End, now.AddDays(11)).Generate())),
                ApplicationFactory.Accepted(1, 7, BookingFactory.Complete(ConcertFaker.GetFaker("Once in a Lifetime!", 15m, 125, 1, seed.Opportunities[6].VenueId, seed.Opportunities[6].Period.Start, seed.Opportunities[6].Period.End, now.AddDays(14)).Generate())),
                ApplicationFactory.Accepted(2, 7, BookingFactory.Complete(ConcertFaker.GetFaker("Jungle Fever", 30m, 180, 2, seed.Opportunities[6].VenueId, seed.Opportunities[6].Period.Start, seed.Opportunities[6].Period.End, now.AddDays(17)).Generate())),
                // Apps 27-34: Pending (no concert)
                ApplicationFactory.Create(15, 7),
                ApplicationFactory.Create(16, 7),
                ApplicationFactory.Create(1, 8),
                ApplicationFactory.Create(2, 8),
                ApplicationFactory.Create(17, 8),
                ApplicationFactory.Create(18, 8),
                ApplicationFactory.Create(17, 40),
                ApplicationFactory.Create(18, 41),
                // App 35: Accepted (upcoming concert)
                ApplicationFactory.Accepted(1, 14, BookingFactory.Confirmed(ConcertFaker.GetFaker("Boogie Nights", 20m, 100, 1, seed.Opportunities[13].VenueId, seed.Opportunities[13].Period.Start, seed.Opportunities[13].Period.End, now.AddDays(6)).Generate())),
                // Apps 36-38: Pending (no concert)
                ApplicationFactory.Create(2, 14),
                ApplicationFactory.Create(3, 14),
                ApplicationFactory.Create(4, 14),
                // App 39: Accepted (upcoming concert)
                seed.PostedDoorSplitApp,
                // Apps 40-41: Pending (no concert)
                seed.DoorSplitApp,
                ApplicationFactory.Create(7, 15),
                // App 42: Accepted (upcoming concert)
                ApplicationFactory.Accepted(8, 15, BookingFactory.Confirmed(ConcertFaker.GetFaker("Bass in the Air", 30m, 140, 8, seed.Opportunities[14].VenueId, seed.Opportunities[14].Period.Start, seed.Opportunities[14].Period.End, now.AddDays(18)).Generate())),
                // Apps 43-44: Pending (no concert)
                ApplicationFactory.CreatePrepaid(9, 16),
                ApplicationFactory.CreatePrepaid(10, 16),
                // App 45: Accepted (upcoming concert)
                ApplicationFactory.AcceptedPrepaid(11, 16, BookingFactory.Confirmed(ConcertFaker.GetFaker("Jumpin and thumpin", 15m, 100, 11, seed.Opportunities[15].VenueId, seed.Opportunities[15].Period.Start, seed.Opportunities[15].Period.End, now.AddDays(22)).Generate())),
                // Apps 46-48: Pending (no concert)
                ApplicationFactory.CreatePrepaid(12, 16),
                seed.VersusApp,
                ApplicationFactory.Create(14, 17),
                // App 49: Accepted (upcoming concert)
                seed.PostedVersusApp,
                // Apps 50-70: Pending (no concert)
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
                seed.VenueHireApp,
                ApplicationFactory.CreatePrepaid(2, 52),
                seed.FlatFeeApp,
                // App 71: PostedFlatFeeApp (declared before array)
                seed.PostedFlatFeeApp,
                // Apps 72-75: Pending (no concert)
                ApplicationFactory.Create(3, 31),
                ApplicationFactory.Create(1, 32),
                ApplicationFactory.Create(2, 32),
                ApplicationFactory.Create(3, 32),
                // App 76: AwaitingPayment (concert 33)
                seed.AwaitingPaymentApp,
                // App 77: PostedVenueHireApp (concert 34)
                seed.PostedVenueHireApp,
                // App 78: FinishedDoorSplitApp (concert 35) — VenueId=1, DoorSplit 70%
                seed.FinishedDoorSplitApp,
                // App 79: FinishedVersusApp (concert 36) — VenueId=1, Versus 100+70%
                seed.FinishedVersusApp,
                // App 80: PastVersusApp — VenueId=1, Versus £100+70%, past start date
                seed.PastVersusApp,
                // App 81: PastFlatFeeApp — VenueId=1, FlatFee £200, past start date
                seed.PastFlatFeeApp,
                // App 82: PastVenueHireApp — VenueId=1, VenueHire £300, past start date
                seed.PastVenueHireApp,
                // App 83: PastDoorSplitApp — VenueId=1, DoorSplit 70%, past start date
                seed.PastDoorSplitApp,
                // App 84: UpcomingFlatFeeApp — VenueId=1, FlatFee (future)
                seed.UpcomingFlatFeeApp,
                // App 85: UpcomingVenueHireApp — VenueId=1, VenueHire (future)
                seed.UpcomingVenueHireApp,
                // Accepted + pending apps for opps 34, 35, 46, 47, 48
                ApplicationFactory.Accepted(3, 34, BookingFactory.Confirmed(ConcertFaker.GetFaker("Groove Night", 18m, 130, 3, seed.Opportunities[33].VenueId, seed.Opportunities[33].Period.Start, seed.Opportunities[33].Period.End, now.AddDays(-1)).Generate())),
                ApplicationFactory.Create(4, 34),
                ApplicationFactory.Create(5, 34),
                ApplicationFactory.Accepted(1, 35, BookingFactory.Confirmed(ConcertFaker.GetFaker("Electric Midnight", 22m, 140, 1, seed.Opportunities[34].VenueId, seed.Opportunities[34].Period.Start, seed.Opportunities[34].Period.End, now).Generate())),
                ApplicationFactory.Create(2, 35),
                ApplicationFactory.Create(4, 35),
                ApplicationFactory.Create(5, 35),
                ApplicationFactory.Accepted(4, 46, BookingFactory.Confirmed(ConcertFaker.GetFaker("Summer Haze", 20m, 150, 4, seed.Opportunities[45].VenueId, seed.Opportunities[45].Period.Start, seed.Opportunities[45].Period.End, now.AddDays(10)).Generate())),
                ApplicationFactory.Create(5, 46),
                ApplicationFactory.Create(6, 46),
                ApplicationFactory.Accepted(5, 47, BookingFactory.Confirmed(ConcertFaker.GetFaker("Night Drive", 25m, 160, 5, seed.Opportunities[46].VenueId, seed.Opportunities[46].Period.Start, seed.Opportunities[46].Period.End, now.AddDays(12)).Generate())),
                ApplicationFactory.Create(6, 47),
                ApplicationFactory.Create(7, 47),
                ApplicationFactory.AcceptedPrepaid(6, 48, BookingFactory.Confirmed(ConcertFaker.GetFaker("Weekend Rush", 15m, 120, 6, seed.Opportunities[47].VenueId, seed.Opportunities[47].Period.Start, seed.Opportunities[47].Period.End, now.AddDays(14)).Generate())),
                ApplicationFactory.CreatePrepaid(7, 48),
                ApplicationFactory.CreatePrepaid(8, 48),
            };
            context.Applications.AddRange(applications);
            await context.SaveChangesAsync(ct);
        });

    }
}
