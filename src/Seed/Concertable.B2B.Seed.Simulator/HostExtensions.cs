using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Seed.Contracts;
using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.B2B.Venue.Contracts.Events;
using Concertable.Kernel;
using Concertable.Messaging.Application.Extensions;
using Concertable.Messaging.AzureServiceBus.Extensions;
using Concertable.ServiceDefaults;

namespace Concertable.B2B.Seed.Simulator;

public static class HostExtensions
{
    extension(HostApplicationBuilder builder)
    {
        public HostApplicationBuilder AddSeedSimulatorHost()
        {
            builder.AddServiceDefaults();
            builder.Configuration.AddEnvironmentVariables();
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton<SeedCatalog>();
            builder.Services.AddAzureServiceBusTransport(
                opts =>
                {
                    opts.ConnectionString = builder.Configuration.GetConnectionString("asb")
                        ?? (builder.Environment.IsIntegration() ? null!
                            : throw new InvalidOperationException("Connection string 'asb' is required."));
                    opts.ServiceName = "concertable-b2b-seeding-simulator";
                },
                reg => reg
                    .Publishes<PayoutOwnerRegisteredEvent>()
                    .Publishes<VenueChangedEvent>()
                    .Publishes<ArtistChangedEvent>()
                    .Publishes<ConcertChangedEvent>());
            builder.Services.AddHostedService<SeedEventPublisher>();
            return builder;
        }
    }
}
