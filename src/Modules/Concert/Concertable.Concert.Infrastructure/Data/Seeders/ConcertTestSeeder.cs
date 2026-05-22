using Concertable.DataAccess;
using Concertable.Concert.Infrastructure.Data;
using Concertable.Seeding;
using Concertable.Seeding.Extensions;
using Concertable.Seeding.Factories;
using Concertable.Seeding.Fakers;
using Concertable.Shared;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Concert.Infrastructure.Data.Seeders;

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

            seed.FlatFeeApp = ApplicationFactory.Create(seed.Artist.Id, opps[0].Id, seed.FlatFeeAppContract.ContractType);

            seed.ConfirmedBooking = BookingFactory.Confirmed(ConcertFaker.GetFaker("Draft Concert", 0m, 100, seed.Artist.Id, seed.Venue.Id, opps[1].Period.Start, opps[1].Period.End).Generate());
            seed.ConfirmedApp = ApplicationFactory.Accepted(seed.Artist.Id, opps[1].Id, seed.ConfirmedBooking);

            seed.AwaitingPaymentBooking = BookingFactory.AwaitingPayment(ConcertFaker.GetFaker("Unsettled Concert", 0m, 100, seed.Artist.Id, seed.Venue.Id, opps[2].Period.Start, opps[2].Period.End).Generate());
            seed.AwaitingPaymentApp = ApplicationFactory.Accepted(seed.Artist.Id, opps[2].Id, seed.AwaitingPaymentBooking);

            seed.VersusApp = ApplicationFactory.Create(seed.Artist.Id, opps[3].Id, seed.VersusAppContract.ContractType);
            seed.DoorSplitApp = ApplicationFactory.Create(seed.Artist.Id, opps[4].Id, seed.DoorSplitAppContract.ContractType);
            seed.VenueHireApp = ApplicationFactory.CreatePrepaid(seed.Artist.Id, opps[5].Id, seed.VenueHireAppContract.ContractType);

            seed.PostedFlatFeeBooking = BookingFactory.Confirmed(ConcertFaker.GetFaker("Posted FlatFee Concert", 10.00m, 100, seed.Artist.Id, seed.Venue.Id, opps[6].Period.Start, opps[6].Period.End, now).Generate());
            seed.PostedFlatFeeBooking.Concert!.Genres.Add(Genre.Rock);
            seed.PostedFlatFeeApp = ApplicationFactory.Accepted(seed.Artist.Id, opps[6].Id, seed.PostedFlatFeeBooking);

            seed.PostedDoorSplitBooking = BookingFactory.ConfirmedDeferred(ConcertFaker.GetFaker("Posted DoorSplit Concert", 10.00m, 100, seed.Artist.Id, seed.Venue.Id, opps[7].Period.Start, opps[7].Period.End, now).Generate());
            seed.PostedDoorSplitBooking.Concert!.Genres.Add(Genre.Rock);
            seed.PostedDoorSplitApp = ApplicationFactory.Accepted(seed.Artist.Id, opps[7].Id, seed.PostedDoorSplitBooking);

            seed.PostedVersusBooking = BookingFactory.ConfirmedDeferred(ConcertFaker.GetFaker("Posted Versus Concert", 10.00m, 100, seed.Artist.Id, seed.Venue.Id, opps[8].Period.Start, opps[8].Period.End, now).Generate());
            seed.PostedVersusBooking.Concert!.Genres.Add(Genre.Rock);
            seed.PostedVersusApp = ApplicationFactory.Accepted(seed.Artist.Id, opps[8].Id, seed.PostedVersusBooking);

            seed.PostedVenueHireBooking = BookingFactory.Confirmed(ConcertFaker.GetFaker("Posted VenueHire Concert", 10.00m, 100, seed.Artist.Id, seed.Venue.Id, opps[9].Period.Start, opps[9].Period.End, now).Generate());
            seed.PostedVenueHireBooking.Concert!.Genres.Add(Genre.Rock);
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
        });
    }
}
