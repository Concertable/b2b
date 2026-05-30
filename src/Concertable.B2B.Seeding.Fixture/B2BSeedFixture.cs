using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Venue.Contracts.Events;
using Concertable.Contracts;
using Concertable.Kernel;
using Concertable.Seeding.Identity;

namespace Concertable.B2B.Seeding.Fixture;

/// <summary>
/// Canonical wire-level data for the B2B-published seed set: every <see cref="VenueChangedEvent"/>,
/// <see cref="ArtistChangedEvent"/> and <see cref="ConcertChangedEvent"/> B2B's seeders would publish.
/// Both real B2B (via its own seeders + factories) and the seeding simulator project from these
/// lists so the downstream projection state is byte-identical regardless of who produced it.
/// </summary>
public static class B2BSeedFixture
{
    public const int UpcomingConcertId = 13;

    public static IReadOnlyList<ConcertChangedEvent> Concerts(DateTime now) => BuildConcerts(now);

    public static ConcertChangedEvent UpcomingConcert(DateTime now) =>
        BuildConcerts(now).First(c => c.ConcertId == UpcomingConcertId);

    private sealed record LocationData(string County, string Town, double Latitude, double Longitude);
    private sealed record VenueData(string Name, string BannerUrl);
    private sealed record BandData(string Name, string BannerUrl, Genre[] Genres);
    private sealed record OppSpec(int VenueId, int DaysOffset);
    private sealed record ConcertSpec(
        int Id,
        string Name,
        decimal Price,
        int TotalTickets,
        int ArtistId,
        int OppIndex,
        int DatePostedDaysOffset,
        Genre[]? Genres);

    private static readonly LocationData[] Locations =
    [
        new("Leicestershire",      "Loughborough", 52.7721, -1.2062),
        new("Greater London",      "London",       51.5074, -0.1278),
        new("Greater Manchester",  "Manchester",   53.4808, -2.2426),
        new("Surrey",              "Guildford",    51.2362, -0.5704),
        new("West Yorkshire",      "Leeds",        53.8008, -1.5491),
        new("West Midlands",       "Birmingham",   52.4862, -1.8904),
        new("Tyne and Wear",       "Newcastle",    54.9783, -1.6178),
        new("South Yorkshire",     "Sheffield",    53.3811, -1.4701),
        new("Merseyside",          "Liverpool",    53.4084, -2.9916),
        new("Bristol",             "Bristol",      51.4545, -2.5879),
        new("Nottinghamshire",     "Nottingham",   52.9548, -1.1581),
        new("Hampshire",           "Southampton",  50.9097, -1.4043),
        new("Lancashire",          "Preston",      53.7632, -2.7031),
        new("Cambridgeshire",      "Cambridge",    52.2053,  0.1218),
        new("Oxfordshire",         "Oxford",       51.7520, -1.2577),
    ];

    private static readonly BandData[] Bands =
    [
        new("The Rockers",            "rockers.jpg",            [Genre.Rock, Genre.Pop, Genre.Jazz]),
        new("Indie Vibes",            "indievibes.jpg",         [Genre.Rock, Genre.Electronic, Genre.HipHop]),
        new("Electronic Pulse",       "electronicpulse.jpg",    [Genre.Electronic, Genre.Jazz]),
        new("Hip-Hop Flow",           "hiphopflow.jpg",         [Genre.HipHop]),
        new("Jazz Masters",           "jazzmaster.jpg",         [Genre.Indie, Genre.Jazz]),
        new("Always Punks",           "alwayspunks.jpg",        [Genre.Rock, Genre.Indie]),
        new("The Hollow Frequencies", "hollowfrequencies.jpg",  [Genre.Pop]),
        new("Neon Foxes",             "neonfoxes.jpg",          [Genre.HipHop, Genre.Pop]),
        new("Velvet Static",          "velvetstatic.jpg",       [Genre.Electronic, Genre.Jazz]),
        new("Echo Bloom",             "echobloom.jpg",          [Genre.Rock, Genre.DnB]),
        new("The Wild Chords",        "wildchords.jpg",         [Genre.Indie, Genre.Rock]),
        new("Glitch & Glow",          "glitchandglow.jpg",      [Genre.Pop]),
        new("Sonic Mirage",           "sonicmirage.jpg",        [Genre.Indie, Genre.Electronic]),
        new("Neon Echoes",            "neonechoes.jpg",         [Genre.HipHop]),
        new("Dreamwave Collective",   "dreamwavecollective.jpg",[Genre.DnB]),
        new("Synth Pulse",            "synthpulse.jpg",         [Genre.Rock]),
        new("The Brass Poets",        "brasspoets.jpg",         [Genre.Jazz]),
        new("Groove Alchemy",         "groovealchemy.jpg",      [Genre.Indie]),
        new("Velvet Rhymes",          "velvetrhymes.jpg",       [Genre.HipHop]),
        new("The Lo-Fi Syndicate",    "lofisyndicate.jpg",      [Genre.DnB]),
        new("Beats & Blue Notes",     "beatsbluenotes.jpg",     [Genre.House]),
        new("Bass Pilots",            "basspilots.jpg",         [Genre.Rock]),
        new("The Digital Prophets",   "digitalprophets.jpg",    [Genre.Electronic]),
        new("Neon Bass Theory",       "neonbasstheory.jpg",     [Genre.Indie]),
        new("Wavelength 303",         "wavelength303.jpg",      [Genre.Pop]),
        new("Gravity Loops",          "gravityloops.jpg",       [Genre.Rock]),
        new("The Golden Reverie",     "goldenreverie.jpg",      [Genre.House]),
        new("Fable Sound",            "fablesound.jpg",         [Genre.Electronic]),
        new("Moonlight Static",       "moonlightstatic.jpg",    [Genre.DnB]),
        new("The Chromatics",         "thechromatics.jpg",      [Genre.Jazz]),
        new("Echo Reverberation",     "echoreverberation.jpg",  [Genre.Indie]),
        new("Midnight Reverie",       "midnightreverie.jpg",    [Genre.Rock]),
        new("Static Wolves",          "staticwolves.jpg",       [Genre.HipHop]),
        new("Echo Collapse",          "echocollapse.jpg",       [Genre.Pop]),
        new("Violet Sundown",         "violetsundown.jpg",      [Genre.House]),
    ];

    private static readonly VenueData[] VenueRows =
    [
        new("Redhill Hall",                "redhillhall.jpg"),
        new("Weybridge Pavilion",          "weybridgepavilon.jpg"),
        new("Cobham Arts Centre",          "cobhamarts.jpg"),
        new("Chertsey Arena",              "chertseyarena.jpg"),
        new("Camden Electric Ballroom",    "camdenballroom.jpg"),
        new("Manchester Night & Day Café", "manchesternightday.jpg"),
        new("Birmingham O2 Institute",     "birminghamo2.jpg"),
        new("Edinburgh Usher Hall",        "edinburghusher.jpg"),
        new("Liverpool Philharmonic Hall", "liverpoolphilharmonic.jpg"),
        new("Leeds Brudenell Social Club", "leedsbrudenell.jpg"),
        new("Glasgow Barrowland Ballroom", "glasgowbarrowland.jpg"),
        new("Sheffield Leadmill",          "sheffieldleadmill.jpg"),
        new("Nottingham Rock City",        "nottinghamrockcity.jpg"),
        new("Bristol Thekla",              "bristolthekla.jpg"),
        new("Brighton Concorde 2",         "brightonconcorde2.jpg"),
        new("Cardiff Tramshed",            "cardifftramshed.jpg"),
        new("Newcastle O2 Academy",        "newcastleo2.jpg"),
        new("Oxford O2 Academy",           "oxfordo2.jpg"),
        new("Cambridge Corn Exchange",     "cambridgecornexchange.jpg"),
        new("Bath Komedia",                "bathkomedia.jpg"),
        new("Aberdeen The Lemon Tree",     "aberdeenlemontree.jpg"),
        new("York Barbican",               "yorkbarbican.jpg"),
        new("Belfast Limelight",           "belfastlimelight.jpg"),
        new("Dublin Vicar Street",         "dublinvicarstreet.jpg"),
        new("Norwich Waterfront",          "norwichwaterfront.jpg"),
        new("Exeter Phoenix",              "exeterphoenix.jpg"),
        new("Southampton Engine Rooms",    "southamptonengine.jpg"),
        new("Hull The Welly Club",         "hullwellyclub.jpg"),
        new("Plymouth Junction",           "plymouthjunction.jpg"),
        new("Swansea Sin City",            "swanseasincity.jpg"),
        new("Inverness Ironworks",         "invernessironworks.jpg"),
        new("Stirling Albert Halls",       "stirlingalberthalls.jpg"),
        new("Dundee Fat Sams",             "dundeefatsams.jpg"),
        new("Coventry Empire",             "coventryempire.jpg"),
    ];

    private static readonly OppSpec[] Opportunities =
    [
        new(1, -60),  new(2, -55),  new(3, -50),  new(4, -45),  new(5, -40),
        new(6, -35),  new(7, -30),  new(8, -25),  new(9, -20),  new(10, -15),
        new(1, -10),  new(2, -5),   new(3, 0),    new(4, 5),    new(5, 10),
        new(6, 15),   new(7, 20),   new(8, 25),   new(9, 30),   new(10, 35),
        new(1, -40),  new(2, 45),   new(3, 50),   new(4, 55),   new(5, 60),
        new(6, 65),   new(7, 70),   new(8, 75),   new(9, 80),   new(10, 85),
        new(1, -85),  new(1, 85),   new(1, 2),    new(1, 4),    new(1, 6),
        new(2, 8),    new(2, 10),   new(2, 12),   new(3, 14),   new(3, 16),
        new(3, 18),   new(4, 22),   new(5, 24),   new(6, 26),   new(1, 30),
        new(1, 32),   new(1, 34),   new(1, 36),   new(1, 38),   new(1, -60),
        new(1, -90),  new(1, 120),  new(1, 150),  new(1, 180),  new(1, 200),
        new(1, 210),  new(1, 220),  new(1, 15),   new(1, 20),   new(1, 40),
        new(1, 42),   new(1, 44),   new(1, 46),   new(1, -120), new(1, -85),
        new(1, -40),  new(1, -60),
    ];

    // Contracts where the payee is the artist (VenueHire type). Indices into Opportunities[].
    private static readonly HashSet<int> VenueHireOppIndices = [9, 15, 20, 27, 36, 42, 47, 51, 58, 62, 65];

    // Opportunities at these indices use a 5-hour duration instead of 3 (matches SeedData's hour exception at index 31).
    private static readonly HashSet<int> FiveHourOppIndices = [31];

    private static readonly ConcertSpec[] ConcertRows =
    [
        new(1,  "Ultimate Dance Party",       27m, 160, 1,  5,   2,    null),
        new(2,  "Boogie Wonderland",          25m, 120, 1,  52,  0,    [Genre.Rock, Genre.Indie]),
        new(3,  "Funk it up",                 20m, 150, 2,  53,  0,    [Genre.Rock, Genre.Indie]),
        new(4,  "Boogie it up!",              20m, 150, 2,  30,  -85,  null),
        new(5,  "VenueHire Spectacular",      30m, 200, 1,  20,  -40,  null),
        new(6,  "Awaiting Show",              15m, 100, 1,  32,  3,    null),
        new(7,  "DoorSplit Settlement Show",  20m, 100, 1,  49,  -60,  null),
        new(8,  "Versus Settlement Show",     20m, 100, 1,  50,  -90,  null),
        new(9,  "Past Versus Show",           20m, 100, 1,  63,  -120, null),
        new(10, "Past FlatFee Show",          20m, 100, 1,  64,  -85,  null),
        new(11, "Past VenueHire Show",        30m, 100, 1,  65,  -40,  null),
        new(12, "Past DoorSplit Show",        20m, 100, 1,  66,  -60,  null),
        new(13, "Upcoming FlatFee Show",      20m, 150, 2,  57,  0,    [Genre.Rock, Genre.Indie]),
        new(14, "Upcoming VenueHire Show",    30m, 200, 1,  58,  0,    [Genre.Rock, Genre.Indie]),
        new(15, "Rockin' all Night",          15m, 120, 1,  0,   -58,  null),
        new(16, "Non Stop Party",             12m, 110, 2,  0,   -55,  null),
        new(17, "Super Mix",                  18m, 130, 3,  0,   -52,  null),
        new(18, "Hip-Hop till you flip-flop", 10m, 100, 4,  0,   -49,  null),
        new(19, "Dance the night away",       25m, 140, 1,  1,   -46,  null),
        new(20, "Dizzy One",                  20m, 150, 2,  1,   -43,  null),
        new(21, "Beers and Boombox",          30m, 170, 5,  1,   -40,  null),
        new(22, "Rockin' Tonight!",           16m, 130, 6,  1,   -37,  null),
        new(23, "Groovin' All Night",         14m, 115, 1,  2,   -34,  null),
        new(24, "Nonstop Vibes",              22m, 135, 2,  2,   -31,  null),
        new(25, "Electric Dreams",            13m, 125, 7,  2,   -28,  null),
        new(26, "Beat Drop Frenzy",           11m, 120, 8,  2,   -25,  null),
        new(27, "Summer Jam",                 19m, 140, 1,  3,   -22,  null),
        new(28, "Midnight Madness",           17m, 135, 2,  3,   -19,  null),
        new(29, "Like a Boss",                21m, 145, 9,  3,   -16,  null),
        new(30, "Lights and Sound",           18m, 140, 10, 3,   -13,  null),
        new(31, "Rhythm Nation",              26m, 155, 1,  4,   -10,  null),
        new(32, "Bass Drop Party",            15m, 120, 2,  4,   -7,   null),
        new(33, "Chill & Thrill",             28m, 160, 11, 4,   -4,   null),
        new(34, "Vibin' till Night",          24m, 150, 12, 4,   -1,   null),
        new(35, "Rock Your Soul",             23m, 130, 2,  5,   5,    null),
        new(36, "Danceaway",                  29m, 155, 13, 5,   8,    null),
        new(37, "Bassline Groove Beats",      10m, 110, 14, 5,   11,   null),
        new(38, "Once in a Lifetime!",        15m, 125, 1,  6,   14,   null),
        new(39, "Jungle Fever",               30m, 180, 2,  6,   17,   null),
        new(40, "Boogie Nights",              20m, 100, 1,  13,  6,    null),
        new(41, "Bass in the Air",            30m, 140, 8,  14,  18,   null),
        new(42, "Jumpin and thumpin",         15m, 100, 11, 15,  22,   null),
        new(43, "Groove Night",               18m, 130, 3,  33,  -1,   null),
        new(44, "Electric Midnight",          22m, 140, 1,  34,  0,    null),
        new(45, "Summer Haze",                20m, 150, 4,  45,  10,   null),
        new(46, "Night Drive",                25m, 160, 5,  46,  12,   null),
        new(47, "Weekend Rush",               15m, 120, 6,  47,  14,   null),
    ];

    public static IReadOnlyList<VenueChangedEvent> Venues { get; } = BuildVenues();
    public static IReadOnlyList<ArtistChangedEvent> Artists { get; } = BuildArtists();

    private static VenueChangedEvent BuildVenue1() =>
        new(
            VenueId:   1,
            UserId:    SeedUsers.VenueManagerId(1),
            Name:      "The Grand Venue",
            About:     "The Grand Venue is the canonical Test County seed venue.",
            Avatar:    "avatar.jpg",
            BannerUrl: "grandvenue.jpg",
            County:    "Test County",
            Town:      "Test Town",
            Latitude:  51.0,
            Longitude: 0.0,
            Email:     SeedUsers.VenueManagerEmail(1));

    private static IReadOnlyList<VenueChangedEvent> BuildVenues()
    {
        var result = new List<VenueChangedEvent>(1 + VenueRows.Length) { BuildVenue1() };
        var locIndex = Bands.Length;
        for (int i = 0; i < VenueRows.Length; i++)
        {
            var v = VenueRows[i];
            var loc = Locations[locIndex++ % Locations.Length];
            var venueId = i + 2;
            result.Add(new VenueChangedEvent(
                VenueId:   venueId,
                UserId:    SeedUsers.VenueManagerId(venueId),
                Name:      v.Name,
                About:     $"{v.Name} is a venue in {loc.Town}.",
                Avatar:    "avatar.jpg",
                BannerUrl: v.BannerUrl,
                County:    loc.County,
                Town:      loc.Town,
                Latitude:  loc.Latitude,
                Longitude: loc.Longitude,
                Email:     EmailFromVenueName(v.Name)));
        }
        return result;
    }

    private static IReadOnlyList<ArtistChangedEvent> BuildArtists()
    {
        var result = new List<ArtistChangedEvent>(Bands.Length);
        for (int i = 0; i < Bands.Length; i++)
        {
            var b = Bands[i];
            var loc = Locations[i % Locations.Length];
            var artistId = i + 1;
            result.Add(new ArtistChangedEvent(
                ArtistId:  artistId,
                UserId:    SeedUsers.ArtistManagerId(artistId),
                Name:      b.Name,
                About:     $"{b.Name} is an artist based in {loc.Town}.",
                Avatar:    "avatar.jpg",
                BannerUrl: b.BannerUrl,
                County:    loc.County,
                Town:      loc.Town,
                Latitude:  loc.Latitude,
                Longitude: loc.Longitude,
                Email:     EmailFromArtistName(b.Name),
                Genres:    b.Genres));
        }
        return result;
    }

    private static IReadOnlyList<ConcertChangedEvent> BuildConcerts(DateTime now)
    {
        var venues = BuildVenues();
        var artists = BuildArtists();
        var result = new List<ConcertChangedEvent>(ConcertRows.Length);
        foreach (var c in ConcertRows)
        {
            var opp = Opportunities[c.OppIndex];
            var hours = FiveHourOppIndices.Contains(c.OppIndex) ? 5 : 3;
            var period = new DateRange(now.AddDays(opp.DaysOffset), now.AddDays(opp.DaysOffset).AddHours(hours));
            var datePosted = now.AddDays(c.DatePostedDaysOffset);
            var venue = venues[opp.VenueId - 1];
            var artist = artists[c.ArtistId - 1];
            var isVenueHire = VenueHireOppIndices.Contains(c.OppIndex);
            var payeeUserId = isVenueHire ? artist.UserId : venue.UserId;
            result.Add(new ConcertChangedEvent(
                ConcertId:        c.Id,
                Name:             c.Name,
                About:            $"{c.Name} is a concert at {venue.Name}.",
                Avatar:           null,
                BannerUrl:        null,
                TotalTickets:     c.TotalTickets,
                AvailableTickets: c.TotalTickets,
                Price:            c.Price,
                Period:           period,
                DatePosted:       datePosted,
                ArtistId:         artist.ArtistId,
                ArtistName:       artist.Name,
                VenueId:          venue.VenueId,
                VenueName:        venue.Name,
                Latitude:         venue.Latitude,
                Longitude:        venue.Longitude,
                Genres:           c.Genres ?? [],
                PayeeUserId:      payeeUserId));
        }
        return result;
    }

    private static string EmailFromVenueName(string name) =>
        $"{name.ToLowerInvariant().Replace(" ", "").Replace("&", "and").Replace("é", "e")}@test.com";

    private static string EmailFromArtistName(string name) =>
        $"{name.ToLowerInvariant().Replace(" ", "")}@test.com";
}
