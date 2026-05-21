using Concertable.Application.Interfaces.Geometry;
using Concertable.Shared.Infrastructure.Services.Geometry;
using Concertable.Seeding;
using Concertable.Seeding.Extensions;
using Concertable.Seeding.Fakers;
using Concertable.Venue.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Venue.Infrastructure.Data.Seeders;

internal class VenueDevSeeder : IDevSeeder
{
    public int Order => 2;

    private readonly VenueDbContext context;
    private readonly SeedData seed;
    private readonly IGeometryProvider geometryProvider;
    private readonly ILocationFaker locationFaker;

    public VenueDevSeeder(
        VenueDbContext context,
        SeedData seed,
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider,
        ILocationFaker locationFaker)
    {
        this.context = context;
        this.seed = seed;
        this.geometryProvider = geometryProvider;
        this.locationFaker = locationFaker;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var venueManagerIds = seed.VenueManagerIds;

        await context.Venues.SeedIfEmptyAsync(async () =>
        {
            seed.Venue = VenueFaker.GetFaker(
                seed.VenueManager1.Id,
                "The Grand Venue",
                "grandvenue.jpg",
                "avatar.jpg",
                geometryProvider.CreatePoint(51, 0),
                new Address("Test County", "Test Town"),
                seed.VenueManager1.Email).Generate();

            var venueData = new (string Name, string Banner)[]
            {
                ("Redhill Hall", "redhillhall.jpg"),
                ("Weybridge Pavilion", "weybridgepavilon.jpg"),
                ("Cobham Arts Centre", "cobhamarts.jpg"),
                ("Chertsey Arena", "chertseyarena.jpg"),
                ("Camden Electric Ballroom", "camdenballroom.jpg"),
                ("Manchester Night & Day Café", "manchesternightday.jpg"),
                ("Birmingham O2 Institute", "birminghamo2.jpg"),
                ("Edinburgh Usher Hall", "edinburghusher.jpg"),
                ("Liverpool Philharmonic Hall", "liverpoolphilharmonic.jpg"),
                ("Leeds Brudenell Social Club", "leedsbrudenell.jpg"),
                ("Glasgow Barrowland Ballroom", "glasgowbarrowland.jpg"),
                ("Sheffield Leadmill", "sheffieldleadmill.jpg"),
                ("Nottingham Rock City", "nottinghamrockcity.jpg"),
                ("Bristol Thekla", "bristolthekla.jpg"),
                ("Brighton Concorde 2", "brightonconcorde2.jpg"),
                ("Cardiff Tramshed", "cardifftramshed.jpg"),
                ("Newcastle O2 Academy", "newcastleo2.jpg"),
                ("Oxford O2 Academy", "oxfordo2.jpg"),
                ("Cambridge Corn Exchange", "cambridgecornexchange.jpg"),
                ("Bath Komedia", "bathkomedia.jpg"),
                ("Aberdeen The Lemon Tree", "aberdeenlemontree.jpg"),
                ("York Barbican", "yorkbarbican.jpg"),
                ("Belfast Limelight", "belfastlimelight.jpg"),
                ("Dublin Vicar Street", "dublinvicarstreet.jpg"),
                ("Norwich Waterfront", "norwichwaterfront.jpg"),
                ("Exeter Phoenix", "exeterphoenix.jpg"),
                ("Southampton Engine Rooms", "southamptonengine.jpg"),
                ("Hull The Welly Club", "hullwellyclub.jpg"),
                ("Plymouth Junction", "plymouthjunction.jpg"),
                ("Swansea Sin City", "swanseasincity.jpg"),
                ("Inverness Ironworks", "invernessironworks.jpg"),
                ("Stirling Albert Halls", "stirlingalberthalls.jpg"),
                ("Dundee Fat Sams", "dundeefatsams.jpg"),
                ("Coventry Empire", "coventryempire.jpg")
            };

            var venues = venueData.Select((v, i) =>
            {
                var loc = locationFaker.Next();
                return VenueFaker.GetFaker(
                    venueManagerIds[i + 1],
                    v.Name,
                    v.Banner,
                    "avatar.jpg",
                    geometryProvider.CreatePoint(loc.Latitude, loc.Longitude),
                    new Address(loc.County, loc.Town),
                    $"{v.Name.ToLowerInvariant().Replace(" ", "").Replace("&", "and").Replace("é", "e")}@test.com").Generate();
            }).ToArray();

            context.Venues.Add(seed.Venue);
            context.Venues.AddRange(venues);
            await context.SaveChangesAsync(ct);
        });
    }
}
