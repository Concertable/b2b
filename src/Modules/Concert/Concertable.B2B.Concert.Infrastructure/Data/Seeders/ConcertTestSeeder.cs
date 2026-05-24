using Concertable.DataAccess;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.Seeding;
using Concertable.Seeding.Extensions;
using Concertable.Seeding.Factories;
using Concertable.Seeding.Fakers;
using Microsoft.EntityFrameworkCore;
using Concertable.Contracts;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Infrastructure.Data.Seeders;

internal class ConcertTestSeeder : ITestSeeder
{
    public int Order => 4;

    private readonly ConcertDbContext context;
    private readonly SeedData seed;
    private readonly TimeProvider timeProvider;

    public ConcertTestSeeder(ConcertDbContext context, SeedData seed, TimeProvider timeProvider)
    {
        this.context = context;
        this.seed = seed;
        this.timeProvider = timeProvider;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        await context.Opportunities.SeedIfEmptyAsync(async () =>
        {
            seed.Opportunities =
            [
                OpportunityFactory.Create(seed.Venue.Id, new DateRange(now.AddMonths(2), now.AddMonths(2).AddHours(3)), contractId: seed.FlatFeeAppContract.Id, [Genre.Rock]),
                OpportunityFactory.Create(seed.Venue.Id, new DateRange(now.AddMonths(3), now.AddMonths(3).AddHours(3)), contractId: seed.ConfirmedAppContract.Id, [Genre.Rock]),
                OpportunityFactory.Create(seed.Venue.Id, new DateRange(now.AddMonths(4), now.AddMonths(4).AddHours(3)), contractId: seed.AwaitingPaymentAppContract.Id, [Genre.Rock]),
                OpportunityFactory.Create(seed.Venue.Id, new DateRange(now.AddMonths(5), now.AddMonths(5).AddHours(3)), contractId: seed.VersusAppContract.Id, [Genre.Rock]),
                OpportunityFactory.Create(seed.Venue.Id, new DateRange(now.AddMonths(6), now.AddMonths(6).AddHours(3)), contractId: seed.DoorSplitAppContract.Id, [Genre.Rock]),
                OpportunityFactory.Create(seed.Venue.Id, new DateRange(now.AddMonths(7), now.AddMonths(7).AddHours(3)), contractId: seed.VenueHireAppContract.Id, [Genre.Rock]),
                OpportunityFactory.Create(seed.Venue.Id, new DateRange(now.AddMonths(8), now.AddMonths(8).AddHours(3)), contractId: seed.PostedFlatFeeAppContract.Id, [Genre.Rock]),
                OpportunityFactory.Create(seed.Venue.Id, new DateRange(now.AddMonths(9), now.AddMonths(9).AddHours(3)), contractId: seed.PostedDoorSplitAppContract.Id, [Genre.Rock]),
                OpportunityFactory.Create(seed.Venue.Id, new DateRange(now.AddMonths(10), now.AddMonths(10).AddHours(3)), contractId: seed.PostedVersusAppContract.Id, [Genre.Rock]),
                OpportunityFactory.Create(seed.Venue.Id, new DateRange(now.AddMonths(11), now.AddMonths(11).AddHours(3)), contractId: seed.PostedVenueHireAppContract.Id, [Genre.Rock]),
            ];

            context.Opportunities.AddRange(seed.Opportunities);
            await context.SaveChangesAsync(ct);
        });

        await context.Applications.SeedIfEmptyAsync(async () =>
        {
            var opps = seed.Opportunities;

            seed.ConfirmedBooking = BookingFactory.Confirmed(1);
            seed.AwaitingPaymentBooking = BookingFactory.AwaitingPayment(2);
            seed.PostedFlatFeeBooking = BookingFactory.Confirmed(3);
            seed.PostedDoorSplitBooking = BookingFactory.ConfirmedDeferred(4);
            seed.PostedVersusBooking = BookingFactory.ConfirmedDeferred(5);
            seed.PostedVenueHireBooking = BookingFactory.Confirmed(6);

            seed.Bookings = [
                seed.ConfirmedBooking,
                seed.AwaitingPaymentBooking,
                seed.PostedFlatFeeBooking,
                seed.PostedDoorSplitBooking,
                seed.PostedVersusBooking,
                seed.PostedVenueHireBooking,
            ];

            seed.FlatFeeApp = ApplicationFactory.Create(seed.Artist.Id, opps[0].Id, seed.FlatFeeAppContract.ContractType);
            seed.ConfirmedApp = ApplicationFactory.Accepted(seed.Artist.Id, opps[1].Id, seed.ConfirmedBooking);
            seed.AwaitingPaymentApp = ApplicationFactory.Accepted(seed.Artist.Id, opps[2].Id, seed.AwaitingPaymentBooking);
            seed.VersusApp = ApplicationFactory.Create(seed.Artist.Id, opps[3].Id, seed.VersusAppContract.ContractType);
            seed.DoorSplitApp = ApplicationFactory.Create(seed.Artist.Id, opps[4].Id, seed.DoorSplitAppContract.ContractType);
            seed.VenueHireApp = ApplicationFactory.CreatePrepaid(seed.Artist.Id, opps[5].Id, seed.VenueHireAppContract.ContractType);
            seed.PostedFlatFeeApp = ApplicationFactory.Accepted(seed.Artist.Id, opps[6].Id, seed.PostedFlatFeeBooking);
            seed.PostedDoorSplitApp = ApplicationFactory.Accepted(seed.Artist.Id, opps[7].Id, seed.PostedDoorSplitBooking);
            seed.PostedVersusApp = ApplicationFactory.Accepted(seed.Artist.Id, opps[8].Id, seed.PostedVersusBooking);
            seed.PostedVenueHireApp = ApplicationFactory.AcceptedPrepaid(seed.Artist.Id, opps[9].Id, seed.PostedVenueHireBooking);

            context.Applications.AddRange(
                seed.FlatFeeApp,
                seed.ConfirmedApp,
                seed.AwaitingPaymentApp,
                seed.VersusApp,
                seed.DoorSplitApp,
                seed.VenueHireApp,
                seed.PostedFlatFeeApp,
                seed.PostedDoorSplitApp,
                seed.PostedVersusApp,
                seed.PostedVenueHireApp);

            await context.SaveChangesAsync(ct);

            context.Concerts.AddRange(
                ConcertFaker.GetFaker(1, seed.ConfirmedBooking.Id, "Draft Concert", 0m, 100, seed.Artist.Id, seed.Venue.Id, opps[1].Period.Start, opps[1].Period.End).Generate(),
                ConcertFaker.GetFaker(2, seed.AwaitingPaymentBooking.Id, "Unsettled Concert", 0m, 100, seed.Artist.Id, seed.Venue.Id, opps[2].Period.Start, opps[2].Period.End).Generate(),
                ConcertFaker.GetFaker(3, seed.PostedFlatFeeBooking.Id, "Posted FlatFee Concert", 10.00m, 100, seed.Artist.Id, seed.Venue.Id, opps[6].Period.Start, opps[6].Period.End, [Genre.Rock]).Generate().With("DatePosted", now),
                ConcertFaker.GetFaker(4, seed.PostedDoorSplitBooking.Id, "Posted DoorSplit Concert", 10.00m, 100, seed.Artist.Id, seed.Venue.Id, opps[7].Period.Start, opps[7].Period.End, [Genre.Rock]).Generate().With("DatePosted", now),
                ConcertFaker.GetFaker(5, seed.PostedVersusBooking.Id, "Posted Versus Concert", 10.00m, 100, seed.Artist.Id, seed.Venue.Id, opps[8].Period.Start, opps[8].Period.End, [Genre.Rock]).Generate().With("DatePosted", now),
                ConcertFaker.GetFaker(6, seed.PostedVenueHireBooking.Id, "Posted VenueHire Concert", 10.00m, 100, seed.Artist.Id, seed.Venue.Id, opps[9].Period.Start, opps[9].Period.End, [Genre.Rock]).Generate().With("DatePosted", now));

            await context.SaveChangesAsync(ct);
        });
    }
}
