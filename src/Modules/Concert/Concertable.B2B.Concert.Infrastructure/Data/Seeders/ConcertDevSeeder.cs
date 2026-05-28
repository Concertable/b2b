using Concertable.DataAccess;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.Seeding;
using Concertable.Seeding.Extensions;
using Concertable.B2B.Seeding;
using Concertable.B2B.Seeding.Factories;
using Concertable.B2B.Seeding.Fakers;
using Microsoft.EntityFrameworkCore;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;

namespace Concertable.B2B.Concert.Infrastructure.Data.Seeders;

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

        await context.VenueReadModels.SeedIfEmptyAsync(async () =>
        {
            context.VenueReadModels.AddRange(seed.Venues.Select(v => new VenueReadModel
            {
                Id = v.Id,
                UserId = v.UserId,
                Name = v.Name,
                About = v.About,
                County = v.Address.County,
                Town = v.Address.Town,
                Location = v.Location
            }));
            await context.SaveChangesAsync(ct);
        });

        await context.ArtistReadModels.SeedIfEmptyAsync(async () =>
        {
            context.ArtistReadModels.AddRange(seed.Artists.Select(a => new ArtistReadModel
            {
                Id = a.Id,
                UserId = a.UserId,
                Name = a.Name,
                Avatar = a.Avatar,
                BannerUrl = a.BannerUrl,
                County = a.Address.County,
                Town = a.Address.Town,
                Email = a.Email,
                Genres = a.Genres.Select(g => new ArtistReadModelGenre { ArtistReadModelId = a.Id, Genre = g }).ToList()
            }));
            await context.SaveChangesAsync(ct);
        });

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
            var opps = seed.Opportunities;

            // ====== Step 1: Bookings ======

            seed.ConfirmedBooking = BookingFactory.Complete(1);
            seed.PostedDoorSplitBooking = BookingFactory.ConfirmedDeferred(2);
            seed.PostedVersusBooking = BookingFactory.ConfirmedDeferred(3);
            seed.PostedFlatFeeBooking = BookingFactory.Complete(4);
            seed.PostedVenueHireBooking = BookingFactory.Complete(5);
            seed.AwaitingPaymentBooking = BookingFactory.AwaitingPayment(6);
            seed.FinishedDoorSplitBooking = BookingFactory.CompleteDeferred(7);
            seed.FinishedVersusBooking = BookingFactory.CompleteDeferred(8);
            seed.PastVersusBooking = BookingFactory.ConfirmedDeferred(9);
            seed.PastFlatFeeBooking = BookingFactory.Confirmed(10);
            seed.PastVenueHireBooking = BookingFactory.Confirmed(11);
            seed.PastDoorSplitBooking = BookingFactory.ConfirmedDeferred(12);
            seed.UpcomingFlatFeeBooking = BookingFactory.Confirmed(13);
            seed.UpcomingVenueHireBooking = BookingFactory.Confirmed(14);

            seed.Bookings = [
                seed.ConfirmedBooking,        // [0]  id=1
                seed.PostedDoorSplitBooking,  // [1]  id=2
                seed.PostedVersusBooking,     // [2]  id=3
                seed.PostedFlatFeeBooking,    // [3]  id=4
                seed.PostedVenueHireBooking,  // [4]  id=5
                seed.AwaitingPaymentBooking,  // [5]  id=6
                seed.FinishedDoorSplitBooking,// [6]  id=7
                seed.FinishedVersusBooking,   // [7]  id=8
                seed.PastVersusBooking,       // [8]  id=9
                seed.PastFlatFeeBooking,      // [9]  id=10
                seed.PastVenueHireBooking,    // [10] id=11
                seed.PastDoorSplitBooking,    // [11] id=12
                seed.UpcomingFlatFeeBooking,  // [12] id=13
                seed.UpcomingVenueHireBooking,// [13] id=14
                BookingFactory.Complete(15), BookingFactory.Complete(16), BookingFactory.Complete(17), BookingFactory.Complete(18),
                BookingFactory.Complete(19), BookingFactory.Complete(20), BookingFactory.Complete(21), BookingFactory.Complete(22),
                BookingFactory.Complete(23), BookingFactory.Complete(24), BookingFactory.Complete(25), BookingFactory.Complete(26),
                BookingFactory.Complete(27), BookingFactory.Complete(28), BookingFactory.Complete(29), BookingFactory.Complete(30),
                BookingFactory.Complete(31), BookingFactory.Complete(32), BookingFactory.Complete(33), BookingFactory.Complete(34),
                BookingFactory.Complete(35), BookingFactory.Complete(36), BookingFactory.Complete(37), BookingFactory.Complete(38),
                BookingFactory.Complete(39), BookingFactory.Confirmed(40), BookingFactory.Confirmed(41), BookingFactory.Confirmed(42),
                BookingFactory.Confirmed(43), BookingFactory.Confirmed(44), BookingFactory.Confirmed(45), BookingFactory.Confirmed(46),
                BookingFactory.Confirmed(47),
            ];

            // ====== Step 2: Applications ======

            seed.ConfirmedApp = ApplicationFactory.Accepted(1, 6, seed.Bookings[0]);
            seed.PostedDoorSplitApp = ApplicationFactory.Accepted(1, 53, seed.Bookings[1]);
            seed.PostedVersusApp = ApplicationFactory.Accepted(2, 54, seed.Bookings[2]);
            seed.PostedFlatFeeApp = ApplicationFactory.Accepted(2, 31, seed.Bookings[3]);
            seed.PostedVenueHireApp = ApplicationFactory.AcceptedPrepaid(1, 21, seed.Bookings[4]);
            seed.AwaitingPaymentApp = ApplicationFactory.Accepted(1, 33, seed.Bookings[5]);
            seed.FinishedDoorSplitApp = ApplicationFactory.Accepted(1, 50, seed.Bookings[6]);
            seed.FinishedVersusApp = ApplicationFactory.Accepted(1, 51, seed.Bookings[7]);
            seed.PastVersusApp = ApplicationFactory.Accepted(1, opps[63].Id, seed.Bookings[8]);
            seed.PastFlatFeeApp = ApplicationFactory.Accepted(1, opps[64].Id, seed.Bookings[9]);
            seed.PastVenueHireApp = ApplicationFactory.AcceptedPrepaid(1, opps[65].Id, seed.Bookings[10]);
            seed.PastDoorSplitApp = ApplicationFactory.Accepted(1, opps[66].Id, seed.Bookings[11]);
            seed.UpcomingFlatFeeApp = ApplicationFactory.Accepted(2, 58, seed.Bookings[12]);
            seed.UpcomingVenueHireApp = ApplicationFactory.AcceptedPrepaid(1, 59, seed.Bookings[13]);

            seed.FreshVenueHireOpportunity = opps[62];

            seed.DoorSplitApp = ApplicationFactory.Create(1, opps[55].Id, seed.Contracts[55].ContractType);
            seed.VersusApp = ApplicationFactory.Create(1, opps[56].Id, seed.Contracts[56].ContractType);
            seed.VenueHireApp = ApplicationFactory.CreatePrepaid(1, opps[51].Id, seed.Contracts[51].ContractType);
            seed.FlatFeeApp = ApplicationFactory.Create(1, opps[54].Id, seed.Contracts[54].ContractType);

            var applications = new ApplicationEntity[]
            {
                // Apps 1-20: Complete (past concerts)
                ApplicationFactory.Accepted(1, 1, seed.Bookings[14]),
                ApplicationFactory.Accepted(2, 1, seed.Bookings[15]),
                ApplicationFactory.Accepted(3, 1, seed.Bookings[16]),
                ApplicationFactory.Accepted(4, 1, seed.Bookings[17]),
                ApplicationFactory.Accepted(1, 2, seed.Bookings[18]),
                ApplicationFactory.Accepted(2, 2, seed.Bookings[19]),
                ApplicationFactory.Accepted(5, 2, seed.Bookings[20]),
                ApplicationFactory.Accepted(6, 2, seed.Bookings[21]),
                ApplicationFactory.Accepted(1, 3, seed.Bookings[22]),
                ApplicationFactory.Accepted(2, 3, seed.Bookings[23]),
                ApplicationFactory.Accepted(7, 3, seed.Bookings[24]),
                ApplicationFactory.Accepted(8, 3, seed.Bookings[25]),
                ApplicationFactory.Accepted(1, 4, seed.Bookings[26]),
                ApplicationFactory.Accepted(2, 4, seed.Bookings[27]),
                ApplicationFactory.Accepted(9, 4, seed.Bookings[28]),
                ApplicationFactory.Accepted(10, 4, seed.Bookings[29]),
                ApplicationFactory.Accepted(1, 5, seed.Bookings[30]),
                ApplicationFactory.Accepted(2, 5, seed.Bookings[31]),
                ApplicationFactory.Accepted(11, 5, seed.Bookings[32]),
                ApplicationFactory.Accepted(12, 5, seed.Bookings[33]),
                // Apps 21-26: Accepted (upcoming concerts)
                seed.ConfirmedApp,
                ApplicationFactory.Accepted(2, 6, seed.Bookings[34]),
                ApplicationFactory.Accepted(13, 6, seed.Bookings[35]),
                ApplicationFactory.Accepted(14, 6, seed.Bookings[36]),
                ApplicationFactory.Accepted(1, 7, seed.Bookings[37]),
                ApplicationFactory.Accepted(2, 7, seed.Bookings[38]),
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
                ApplicationFactory.Accepted(1, 14, seed.Bookings[39]),
                // Apps 36-38: Pending (no concert)
                ApplicationFactory.Create(2, 14),
                ApplicationFactory.Create(3, 14),
                ApplicationFactory.Create(4, 14),
                // App 39
                seed.PostedDoorSplitApp,
                // Apps 40-41: Pending (no concert)
                seed.DoorSplitApp,
                ApplicationFactory.Create(7, 15),
                // App 42
                ApplicationFactory.Accepted(8, 15, seed.Bookings[40]),
                // Apps 43-44: Pending
                ApplicationFactory.CreatePrepaid(9, 16),
                ApplicationFactory.CreatePrepaid(10, 16),
                // App 45
                ApplicationFactory.AcceptedPrepaid(11, 16, seed.Bookings[41]),
                // Apps 46-48: Pending
                ApplicationFactory.CreatePrepaid(12, 16),
                seed.VersusApp,
                ApplicationFactory.Create(14, 17),
                // App 49
                seed.PostedVersusApp,
                // Apps 50-70: Pending
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
                seed.PostedFlatFeeApp,
                ApplicationFactory.Create(3, 31),
                ApplicationFactory.Create(1, 32),
                ApplicationFactory.Create(2, 32),
                ApplicationFactory.Create(3, 32),
                seed.AwaitingPaymentApp,
                seed.PostedVenueHireApp,
                seed.FinishedDoorSplitApp,
                seed.FinishedVersusApp,
                seed.PastVersusApp,
                seed.PastFlatFeeApp,
                seed.PastVenueHireApp,
                seed.PastDoorSplitApp,
                seed.UpcomingFlatFeeApp,
                seed.UpcomingVenueHireApp,
                // End-batch accepted + pending
                ApplicationFactory.Accepted(3, 34, seed.Bookings[42]),
                ApplicationFactory.Create(4, 34),
                ApplicationFactory.Create(5, 34),
                ApplicationFactory.Accepted(1, 35, seed.Bookings[43]),
                ApplicationFactory.Create(2, 35),
                ApplicationFactory.Create(4, 35),
                ApplicationFactory.Create(5, 35),
                ApplicationFactory.Accepted(4, 46, seed.Bookings[44]),
                ApplicationFactory.Create(5, 46),
                ApplicationFactory.Create(6, 46),
                ApplicationFactory.Accepted(5, 47, seed.Bookings[45]),
                ApplicationFactory.Create(6, 47),
                ApplicationFactory.Create(7, 47),
                ApplicationFactory.AcceptedPrepaid(6, 48, seed.Bookings[46]),
                ApplicationFactory.CreatePrepaid(7, 48),
                ApplicationFactory.CreatePrepaid(8, 48),
            };

            context.Applications.AddRange(applications);
            await context.SaveChangesAsync(ct);

            // ====== Step 3: Concerts ======

            context.Concerts.AddRange(
                ConcertFaker.GetFaker(1,  seed.Bookings[0].Id,  "Ultimate Dance Party",      27m, 160, 1,  opps[5].VenueId,  opps[5].Period.Start,  opps[5].Period.End).Generate().With("DatePosted", now.AddDays(2)),
                ConcertFaker.GetFaker(2,  seed.Bookings[1].Id,  "Boogie Wonderland",          25m, 120, 1,  opps[52].VenueId, opps[52].Period.Start, opps[52].Period.End, [Genre.Rock, Genre.Indie]).Generate(),
                ConcertFaker.GetFaker(3,  seed.Bookings[2].Id,  "Funk it up",                 20m, 150, 2,  opps[53].VenueId, opps[53].Period.Start, opps[53].Period.End, [Genre.Rock, Genre.Indie]).Generate(),
                ConcertFaker.GetFaker(4,  seed.Bookings[3].Id,  "Boogie it up!",              20m, 150, 2,  opps[30].VenueId, opps[30].Period.Start, opps[30].Period.End).Generate().With("DatePosted", now.AddDays(-85)),
                ConcertFaker.GetFaker(5,  seed.Bookings[4].Id,  "VenueHire Spectacular",      30m, 200, 1,  opps[20].VenueId, opps[20].Period.Start, opps[20].Period.End).Generate().With("DatePosted", now.AddDays(-40)),
                ConcertFaker.GetFaker(6,  seed.Bookings[5].Id,  "Awaiting Show",              15m, 100, 1,  opps[32].VenueId, opps[32].Period.Start, opps[32].Period.End).Generate().With("DatePosted", now.AddDays(3)),
                ConcertFaker.GetFaker(7,  seed.Bookings[6].Id,  "DoorSplit Settlement Show",  20m, 100, 1,  opps[49].VenueId, opps[49].Period.Start, opps[49].Period.End).Generate().With("DatePosted", now.AddDays(-60)),
                ConcertFaker.GetFaker(8,  seed.Bookings[7].Id,  "Versus Settlement Show",     20m, 100, 1,  opps[50].VenueId, opps[50].Period.Start, opps[50].Period.End).Generate().With("DatePosted", now.AddDays(-90)),
                ConcertFaker.GetFaker(9,  seed.Bookings[8].Id,  "Past Versus Show",           20m, 100, 1,  opps[63].VenueId, opps[63].Period.Start, opps[63].Period.End).Generate().With("DatePosted", now.AddDays(-120)),
                ConcertFaker.GetFaker(10, seed.Bookings[9].Id,  "Past FlatFee Show",          20m, 100, 1,  opps[64].VenueId, opps[64].Period.Start, opps[64].Period.End).Generate().With("DatePosted", now.AddDays(-85)),
                ConcertFaker.GetFaker(11, seed.Bookings[10].Id, "Past VenueHire Show",        30m, 100, 1,  opps[65].VenueId, opps[65].Period.Start, opps[65].Period.End).Generate().With("DatePosted", now.AddDays(-40)),
                ConcertFaker.GetFaker(12, seed.Bookings[11].Id, "Past DoorSplit Show",        20m, 100, 1,  opps[66].VenueId, opps[66].Period.Start, opps[66].Period.End).Generate().With("DatePosted", now.AddDays(-60)),
                ConcertFaker.GetFaker(13, seed.Bookings[12].Id, "Upcoming FlatFee Show",      20m, 150, 2,  opps[57].VenueId, opps[57].Period.Start, opps[57].Period.End, [Genre.Rock, Genre.Indie]).Generate(),
                ConcertFaker.GetFaker(14, seed.Bookings[13].Id, "Upcoming VenueHire Show",    30m, 200, 1,  opps[58].VenueId, opps[58].Period.Start, opps[58].Period.End, [Genre.Rock, Genre.Indie]).Generate(),
                ConcertFaker.GetFaker(15, seed.Bookings[14].Id, "Rockin' all Night",          15m, 120, 1,  opps[0].VenueId,  opps[0].Period.Start,  opps[0].Period.End).Generate().With("DatePosted", now.AddDays(-58)),
                ConcertFaker.GetFaker(16, seed.Bookings[15].Id, "Non Stop Party",             12m, 110, 2,  opps[0].VenueId,  opps[0].Period.Start,  opps[0].Period.End).Generate().With("DatePosted", now.AddDays(-55)),
                ConcertFaker.GetFaker(17, seed.Bookings[16].Id, "Super Mix",                  18m, 130, 3,  opps[0].VenueId,  opps[0].Period.Start,  opps[0].Period.End).Generate().With("DatePosted", now.AddDays(-52)),
                ConcertFaker.GetFaker(18, seed.Bookings[17].Id, "Hip-Hop till you flip-flop", 10m, 100, 4,  opps[0].VenueId,  opps[0].Period.Start,  opps[0].Period.End).Generate().With("DatePosted", now.AddDays(-49)),
                ConcertFaker.GetFaker(19, seed.Bookings[18].Id, "Dance the night away",       25m, 140, 1,  opps[1].VenueId,  opps[1].Period.Start,  opps[1].Period.End).Generate().With("DatePosted", now.AddDays(-46)),
                ConcertFaker.GetFaker(20, seed.Bookings[19].Id, "Dizzy One",                  20m, 150, 2,  opps[1].VenueId,  opps[1].Period.Start,  opps[1].Period.End).Generate().With("DatePosted", now.AddDays(-43)),
                ConcertFaker.GetFaker(21, seed.Bookings[20].Id, "Beers and Boombox",          30m, 170, 5,  opps[1].VenueId,  opps[1].Period.Start,  opps[1].Period.End).Generate().With("DatePosted", now.AddDays(-40)),
                ConcertFaker.GetFaker(22, seed.Bookings[21].Id, "Rockin' Tonight!",           16m, 130, 6,  opps[1].VenueId,  opps[1].Period.Start,  opps[1].Period.End).Generate().With("DatePosted", now.AddDays(-37)),
                ConcertFaker.GetFaker(23, seed.Bookings[22].Id, "Groovin' All Night",         14m, 115, 1,  opps[2].VenueId,  opps[2].Period.Start,  opps[2].Period.End).Generate().With("DatePosted", now.AddDays(-34)),
                ConcertFaker.GetFaker(24, seed.Bookings[23].Id, "Nonstop Vibes",              22m, 135, 2,  opps[2].VenueId,  opps[2].Period.Start,  opps[2].Period.End).Generate().With("DatePosted", now.AddDays(-31)),
                ConcertFaker.GetFaker(25, seed.Bookings[24].Id, "Electric Dreams",            13m, 125, 7,  opps[2].VenueId,  opps[2].Period.Start,  opps[2].Period.End).Generate().With("DatePosted", now.AddDays(-28)),
                ConcertFaker.GetFaker(26, seed.Bookings[25].Id, "Beat Drop Frenzy",           11m, 120, 8,  opps[2].VenueId,  opps[2].Period.Start,  opps[2].Period.End).Generate().With("DatePosted", now.AddDays(-25)),
                ConcertFaker.GetFaker(27, seed.Bookings[26].Id, "Summer Jam",                 19m, 140, 1,  opps[3].VenueId,  opps[3].Period.Start,  opps[3].Period.End).Generate().With("DatePosted", now.AddDays(-22)),
                ConcertFaker.GetFaker(28, seed.Bookings[27].Id, "Midnight Madness",           17m, 135, 2,  opps[3].VenueId,  opps[3].Period.Start,  opps[3].Period.End).Generate().With("DatePosted", now.AddDays(-19)),
                ConcertFaker.GetFaker(29, seed.Bookings[28].Id, "Like a Boss",                21m, 145, 9,  opps[3].VenueId,  opps[3].Period.Start,  opps[3].Period.End).Generate().With("DatePosted", now.AddDays(-16)),
                ConcertFaker.GetFaker(30, seed.Bookings[29].Id, "Lights and Sound",           18m, 140, 10, opps[3].VenueId,  opps[3].Period.Start,  opps[3].Period.End).Generate().With("DatePosted", now.AddDays(-13)),
                ConcertFaker.GetFaker(31, seed.Bookings[30].Id, "Rhythm Nation",              26m, 155, 1,  opps[4].VenueId,  opps[4].Period.Start,  opps[4].Period.End).Generate().With("DatePosted", now.AddDays(-10)),
                ConcertFaker.GetFaker(32, seed.Bookings[31].Id, "Bass Drop Party",            15m, 120, 2,  opps[4].VenueId,  opps[4].Period.Start,  opps[4].Period.End).Generate().With("DatePosted", now.AddDays(-7)),
                ConcertFaker.GetFaker(33, seed.Bookings[32].Id, "Chill & Thrill",             28m, 160, 11, opps[4].VenueId,  opps[4].Period.Start,  opps[4].Period.End).Generate().With("DatePosted", now.AddDays(-4)),
                ConcertFaker.GetFaker(34, seed.Bookings[33].Id, "Vibin' till Night",          24m, 150, 12, opps[4].VenueId,  opps[4].Period.Start,  opps[4].Period.End).Generate().With("DatePosted", now.AddDays(-1)),
                ConcertFaker.GetFaker(35, seed.Bookings[34].Id, "Rock Your Soul",             23m, 130, 2,  opps[5].VenueId,  opps[5].Period.Start,  opps[5].Period.End).Generate().With("DatePosted", now.AddDays(5)),
                ConcertFaker.GetFaker(36, seed.Bookings[35].Id, "Danceaway",                  29m, 155, 13, opps[5].VenueId,  opps[5].Period.Start,  opps[5].Period.End).Generate().With("DatePosted", now.AddDays(8)),
                ConcertFaker.GetFaker(37, seed.Bookings[36].Id, "Bassline Groove Beats",      10m, 110, 14, opps[5].VenueId,  opps[5].Period.Start,  opps[5].Period.End).Generate().With("DatePosted", now.AddDays(11)),
                ConcertFaker.GetFaker(38, seed.Bookings[37].Id, "Once in a Lifetime!",        15m, 125, 1,  opps[6].VenueId,  opps[6].Period.Start,  opps[6].Period.End).Generate().With("DatePosted", now.AddDays(14)),
                ConcertFaker.GetFaker(39, seed.Bookings[38].Id, "Jungle Fever",               30m, 180, 2,  opps[6].VenueId,  opps[6].Period.Start,  opps[6].Period.End).Generate().With("DatePosted", now.AddDays(17)),
                ConcertFaker.GetFaker(40, seed.Bookings[39].Id, "Boogie Nights",              20m, 100, 1,  opps[13].VenueId, opps[13].Period.Start, opps[13].Period.End).Generate().With("DatePosted", now.AddDays(6)),
                ConcertFaker.GetFaker(41, seed.Bookings[40].Id, "Bass in the Air",            30m, 140, 8,  opps[14].VenueId, opps[14].Period.Start, opps[14].Period.End).Generate().With("DatePosted", now.AddDays(18)),
                ConcertFaker.GetFaker(42, seed.Bookings[41].Id, "Jumpin and thumpin",         15m, 100, 11, opps[15].VenueId, opps[15].Period.Start, opps[15].Period.End).Generate().With("DatePosted", now.AddDays(22)),
                ConcertFaker.GetFaker(43, seed.Bookings[42].Id, "Groove Night",               18m, 130, 3,  opps[33].VenueId, opps[33].Period.Start, opps[33].Period.End).Generate().With("DatePosted", now.AddDays(-1)),
                ConcertFaker.GetFaker(44, seed.Bookings[43].Id, "Electric Midnight",          22m, 140, 1,  opps[34].VenueId, opps[34].Period.Start, opps[34].Period.End).Generate().With("DatePosted", now),
                ConcertFaker.GetFaker(45, seed.Bookings[44].Id, "Summer Haze",                20m, 150, 4,  opps[45].VenueId, opps[45].Period.Start, opps[45].Period.End).Generate().With("DatePosted", now.AddDays(10)),
                ConcertFaker.GetFaker(46, seed.Bookings[45].Id, "Night Drive",                25m, 160, 5,  opps[46].VenueId, opps[46].Period.Start, opps[46].Period.End).Generate().With("DatePosted", now.AddDays(12)),
                ConcertFaker.GetFaker(47, seed.Bookings[46].Id, "Weekend Rush",               15m, 120, 6,  opps[47].VenueId, opps[47].Period.Start, opps[47].Period.End).Generate().With("DatePosted", now.AddDays(14)));

            await context.SaveChangesAsync(ct);

            // ====== Step 4: Post searchable concerts to trigger Search projection ======

            seed.PostedDoorSplitBooking.Concert!.Post(seed.PostedDoorSplitBooking.Concert.Name, seed.PostedDoorSplitBooking.Concert.About, seed.PostedDoorSplitBooking.Concert.Price, seed.PostedDoorSplitBooking.Concert.TotalTickets, now);
            seed.PostedVersusBooking.Concert!.Post(seed.PostedVersusBooking.Concert.Name, seed.PostedVersusBooking.Concert.About, seed.PostedVersusBooking.Concert.Price, seed.PostedVersusBooking.Concert.TotalTickets, now);
            seed.UpcomingFlatFeeBooking.Concert!.Post(seed.UpcomingFlatFeeBooking.Concert.Name, seed.UpcomingFlatFeeBooking.Concert.About, seed.UpcomingFlatFeeBooking.Concert.Price, seed.UpcomingFlatFeeBooking.Concert.TotalTickets, now);
            seed.UpcomingVenueHireBooking.Concert!.Post(seed.UpcomingVenueHireBooking.Concert.Name, seed.UpcomingVenueHireBooking.Concert.About, seed.UpcomingVenueHireBooking.Concert.Price, seed.UpcomingVenueHireBooking.Concert.TotalTickets, now);

            await context.SaveChangesAsync(ct);
        });

        seed.Applications = await context.Applications.ToListAsync(ct);
        seed.Concerts = await context.Concerts.ToListAsync(ct);
    }
}
