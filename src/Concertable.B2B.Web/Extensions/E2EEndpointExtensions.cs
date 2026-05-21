using Concertable.DataAccess;
using Concertable.Concert.Application.Interfaces;
using Concertable.Seeding;
using Concertable.Shared;

namespace Concertable.B2B.Web.Extensions;

public static class E2EEndpointExtensions
{
    public static void MapE2EEndpoints(this WebApplication app)
    {
        app.MapPost("/e2e/reseed", async (IDbInitializer dbInitializer, SeedData seedData) =>
        {
            await dbInitializer.InitializeAsync();
            return Results.Ok(new SeedDataResponse
            {
                TestPassword = SeedData.TestPassword,
                Customer = new SeedUser { Id = seedData.Customer.Id, Email = seedData.Customer.Email, StripeCustomerId = seedData.CustomerStripeCustomerId },
                VenueManager1 = new SeedVenueManager { Id = seedData.VenueManager1.Id, Email = seedData.VenueManager1.Email, StripeAccountId = seedData.VenueManager1StripeAccountId, StripeCustomerId = seedData.VenueManager1StripeCustomerId, VenueId = seedData.Venue.Id },
                ArtistManager1 = new SeedArtistManager { Id = seedData.ArtistManager1.Id, Email = seedData.ArtistManager1.Email, StripeAccountId = seedData.ArtistManager1StripeAccountId, StripeCustomerId = seedData.ArtistManager1StripeCustomerId },
                FreshVenueHireOpportunity = new SeedOpportunity { OpportunityId = seedData.FreshVenueHireOpportunity.Id, VenueId = seedData.FreshVenueHireOpportunity.VenueId },
                PendingFlatFeeApp = new SeedApplication { ApplicationId = seedData.FlatFeeApp.Id },
                PendingVenueHireApp = new SeedApplication { ApplicationId = seedData.VenueHireApp.Id },
                PendingDoorSplitApp = new SeedApplication { ApplicationId = seedData.DoorSplitApp.Id },
                PendingVersusApp = new SeedApplication { ApplicationId = seedData.VersusApp.Id },
                PostedFlatFeeApp = new SeedApplication { ApplicationId = seedData.PostedFlatFeeApp.Id, ConcertId = seedData.PostedFlatFeeApp.Booking!.Concert!.Id },
                PostedDoorSplitApp = new SeedApplication { ApplicationId = seedData.PostedDoorSplitApp.Id, ConcertId = seedData.PostedDoorSplitApp.Booking!.Concert!.Id },
                PostedVersusApp = new SeedApplication { ApplicationId = seedData.PostedVersusApp.Id, ConcertId = seedData.PostedVersusApp.Booking!.Concert!.Id },
                PostedVenueHireApp = new SeedApplication { ApplicationId = seedData.PostedVenueHireApp.Id, ConcertId = seedData.PostedVenueHireApp.Booking!.Concert!.Id },
                FinishedDoorSplitApp = new SeedApplication { ApplicationId = seedData.FinishedDoorSplitApp.Id, ConcertId = seedData.FinishedDoorSplitApp.Booking!.Concert!.Id },
                FinishedVersusApp = new SeedApplication { ApplicationId = seedData.FinishedVersusApp.Id, ConcertId = seedData.FinishedVersusApp.Booking!.Concert!.Id },
                PastVersusApp = new SeedApplication { ApplicationId = seedData.PastVersusApp.Id, ConcertId = seedData.PastVersusApp.Booking!.Concert!.Id },
                PastFlatFeeApp = new SeedApplication { ApplicationId = seedData.PastFlatFeeApp.Id, ConcertId = seedData.PastFlatFeeApp.Booking!.Concert!.Id },
                PastVenueHireApp = new SeedApplication { ApplicationId = seedData.PastVenueHireApp.Id, ConcertId = seedData.PastVenueHireApp.Booking!.Concert!.Id },
                PastDoorSplitApp = new SeedApplication { ApplicationId = seedData.PastDoorSplitApp.Id, ConcertId = seedData.PastDoorSplitApp.Booking!.Concert!.Id },
                UpcomingFlatFeeApp = new SeedApplication { ApplicationId = seedData.UpcomingFlatFeeApp.Id, ConcertId = seedData.UpcomingFlatFeeApp.Booking!.Concert!.Id },
                UpcomingVenueHireApp = new SeedApplication { ApplicationId = seedData.UpcomingVenueHireApp.Id, ConcertId = seedData.UpcomingVenueHireApp.Booking!.Concert!.Id },
                FlatFeeUpcomingConcert = new SeedConcert { Id = seedData.UpcomingFlatFeeBooking.Concert!.Id },
                VenueHireUpcomingConcert = new SeedConcert { Id = seedData.UpcomingVenueHireBooking.Concert!.Id },
                DoorSplitUpcomingConcert = new SeedConcert { Id = seedData.PostedDoorSplitBooking.Concert!.Id },
                VersusUpcomingConcert = new SeedConcert { Id = seedData.PostedVersusBooking.Concert!.Id },
            });
        });

        app.MapPost("/e2e/finish/{concertId:int}", async (int concertId, ICompletionDispatcher CompletionDispatcher) =>
        {
            var result = await CompletionDispatcher.FinishAsync(concertId);
            return result.IsFailed
                ? Results.BadRequest(result.Errors.SelectMessages())
                : Results.Ok();
        });

        app.MapPost("/e2e/run-completion", async (IConcertCompletionRunner runner) =>
        {
            await runner.RunAsync();
            return Results.Ok();
        });
    }
}
