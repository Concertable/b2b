using System.Data;
using System.Security.Cryptography;
using System.Text;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.DataAccess.Application;
using Concertable.Kernel;
using Dapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Respawn;
using Respawn.Graph;

namespace Concertable.B2B.E2ETests.Server;

public static class E2EAdminExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddB2BE2EAdmin(
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            E2EAdminSecurity.RequireE2EEnvironment(environment);
            var adminKey = configuration["E2E:AdminKey"];
            if (string.IsNullOrWhiteSpace(adminKey))
                throw new InvalidOperationException("E2E:AdminKey is required by the B2B E2E host.");

            services.AddSingleton(new E2EAdminOptions(
                adminKey,
                configuration.GetConnectionString("B2BDb")
                    ?? throw new InvalidOperationException("Connection string 'B2BDb' is required by the B2B E2E host.")));
            services.AddScoped<B2BDatabaseResetter>();
            return services;
        }
    }

    extension(WebApplication app)
    {
        public WebApplication MapB2BE2EAdmin()
        {
            E2EAdminSecurity.RequireE2EEnvironment(app.Environment);
            var group = app.MapGroup("/_e2e")
                .AddEndpointFilter(AuthorizeAsync);

            group.MapPost("/reset", ResetAsync);
            group.MapGet("/seed-state", GetSeedState);
            group.MapGet("/applications/{applicationId:int}/booking-id", GetBookingIdAsync);
            group.MapGet("/applications/{applicationId:int}/state", GetApplicationStateAsync);
            group.MapGet("/venues/{venueId:int}/opportunities/newest-id", GetNewestOpportunityIdAsync);
            group.MapPost("/concerts/{concertId:int}/door-revenue", DeclareDoorRevenueAsync);
            return app;
        }
    }

    private static async ValueTask<object?> AuthorizeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var options = context.HttpContext.RequestServices.GetRequiredService<E2EAdminOptions>();
        if (!E2EAdminSecurity.IsAuthorized(options.AdminKey, context.HttpContext.Request.Headers))
        {
            return Results.NotFound();
        }

        return await next(context);
    }

    private static async Task<IResult> ResetAsync(
        B2BDatabaseResetter resetter,
        IDbInitializer initializer,
        CancellationToken cancellationToken)
    {
        await resetter.ResetAsync(cancellationToken);
        await initializer.InitializeAsync();
        return Results.NoContent();
    }

    private static IResult GetSeedState(SeedState seed) => Results.Ok(new
    {
        ArtistManager1 = User(seed.ArtistManager1),
        VenueManager1 = User(seed.VenueManager1),
        VenueManager2 = User(seed.VenueManager2),
        VenueManager3 = User(seed.VenueManager3),
        Tenants = seed.Tenants.Select(tenant => new { tenant.Id, tenant.CreatedByUserId }),
        Venue = new { seed.Venue.Id },
        FreshVenueHireOpportunity = new { seed.FreshVenueHireOpportunity.Id, seed.FreshVenueHireOpportunity.VenueId },
        FlatFeeApp = new { seed.FlatFeeApp.Id },
        DoorSplitApp = new { seed.DoorSplitApp.Id },
        VersusApp = new { seed.VersusApp.Id },
        VenueHireApp = new { seed.VenueHireApp.Id },
        PastFlatFeeApp = new { seed.PastFlatFeeApp.Id },
        PastVenueHireApp = new { seed.PastVenueHireApp.Id },
        PastDoorSplitBooking = Booking(seed.PastDoorSplitBooking),
        PastVersusBooking = Booking(seed.PastVersusBooking),
    });

    private static object User(Concertable.B2B.User.Domain.Entities.UserEntity user) =>
        new { user.Id, user.Email };

    private static object Booking(Concertable.B2B.Concert.Domain.Entities.BookingEntity booking) =>
        new
        {
            booking.Id,
            Concert = new
            {
                booking.Concert!.Id,
                booking.Concert.TicketsSold,
            },
        };

    private static async Task<IResult> GetBookingIdAsync(
        int applicationId,
        IDbConnection connection) =>
        Results.Ok(await connection.QuerySingleAsync<int>(
            "SELECT Id FROM concert.Bookings WHERE ApplicationId = @applicationId",
            new { applicationId }));

    private static async Task<IResult> GetApplicationStateAsync(
        int applicationId,
        IDbConnection connection) =>
        Results.Ok(await connection.QuerySingleAsync<int>(
            "SELECT State FROM concert.Applications WHERE Id = @applicationId",
            new { applicationId }));

    private static async Task<IResult> GetNewestOpportunityIdAsync(
        int venueId,
        IDbConnection connection) =>
        Results.Ok(await connection.QuerySingleAsync<int>(
            "SELECT MAX(Id) FROM concert.Opportunities WHERE VenueId = @venueId",
            new { venueId }));

    private static async Task<IResult> DeclareDoorRevenueAsync(
        int concertId,
        DeclareDoorRevenueRequest request,
        IDbConnection connection)
    {
        await connection.ExecuteAsync(
            "UPDATE concert.Concerts SET DoorRevenue = @doorRevenue WHERE Id = @concertId",
            new { concertId, request.DoorRevenue });
        return Results.NoContent();
    }

    private sealed record DeclareDoorRevenueRequest
    {
        public decimal DoorRevenue { get; init; }
    }
}

internal static class E2EAdminSecurity
{
    private const string AdminKeyHeader = "X-Concertable-E2E-Key";

    public static void RequireE2EEnvironment(IHostEnvironment environment)
    {
        if (!environment.IsE2E())
            throw new InvalidOperationException("B2B E2E admin endpoints can only be enabled in the E2E environment.");
    }

    public static bool IsAuthorized(string expected, IHeaderDictionary headers)
    {
        if (string.IsNullOrWhiteSpace(expected)
            || !headers.TryGetValue(AdminKeyHeader, out var suppliedValues))
        {
            return false;
        }

        var supplied = suppliedValues.ToString();
        if (string.IsNullOrWhiteSpace(supplied))
            return false;

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        return expectedBytes.Length == suppliedBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }
}

internal sealed record E2EAdminOptions(string AdminKey, string ConnectionString);

internal sealed class B2BDatabaseResetter
{
    private readonly E2EAdminOptions options;

    public B2BDatabaseResetter(E2EAdminOptions options)
    {
        this.options = options;
    }

    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        var respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            TablesToIgnore =
            [
                "__EFMigrationsHistory",
                new Table("user", "Users"),
                new Table("admin", "AdminProfiles"),
                new Table("messaging", "Inbox"),
            ],
            DbAdapter = DbAdapter.SqlServer,
            WithReseed = true,
        });
        await respawner.ResetAsync(connection);
    }
}
