using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Seeding.Fixture;
using Concertable.B2B.Seeding.Simulator;
using Concertable.B2B.Venue.Contracts.Events;
using Concertable.Messaging.Application.Extensions;
using Concertable.Messaging.AzureServiceBus.Extensions;
using Concertable.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<B2BSeedFixture>();

builder.Services.AddAzureServiceBusTransport(
    opts =>
    {
        opts.ConnectionString = builder.Configuration.GetConnectionString("asb") ?? "";
        opts.ServiceName = "concertable-b2b-seeding-simulator";
    },
    reg => reg
        .Publishes<VenueChangedEvent>()
        .Publishes<ArtistChangedEvent>()
        .Publishes<ConcertChangedEvent>());

builder.Services.AddHostedService<SeedEventPublishingService>();

var app = builder.Build();
app.Run();
