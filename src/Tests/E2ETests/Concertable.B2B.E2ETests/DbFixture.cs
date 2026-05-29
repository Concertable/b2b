using Aspire.Hosting;
using Respawn;
using Respawn.Graph;

namespace Concertable.B2B.E2ETests;

public sealed class DbFixture
{
    private readonly DistributedApplication app;
    private readonly RespawnableDb b2b = new();
    private readonly PaymentDbFixture payment = new();

    public OpportunityDb Opportunity { get; private set; } = null!;
    public BookingDb Booking { get; private set; } = null!;
    public PaymentDb Payment => payment.Payment;

    public DbFixture(DistributedApplication app) => this.app = app;

    public async Task InitializeAsync()
    {
        await b2b.InitializeAsync(app, AppHostConstants.Databases.B2B, new RespawnerOptions
        {
            TablesToIgnore =
            [
                "__EFMigrationsHistory",
                new Table("Users", "user"),
                new Table("ArtistManagerProfiles", "user"),
                new Table("VenueManagerProfiles", "user"),
                new Table("AdminProfiles", "user"),
                new Table("Inbox", "messaging"),
            ],
            DbAdapter = DbAdapter.SqlServer,
            WithReseed = true
        });
        await payment.InitializeAsync(app);
        Opportunity = new OpportunityDb(b2b.Connection);
        Booking = new BookingDb(b2b.Connection);
    }

    public async Task ResetAsync()
    {
        await b2b.ResetAsync();
        await payment.ResetAsync();
    }

    public async Task DisposeAsync()
    {
        await b2b.DisposeAsync();
        await payment.DisposeAsync();
    }

}
