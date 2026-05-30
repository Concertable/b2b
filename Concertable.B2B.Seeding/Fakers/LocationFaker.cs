namespace Concertable.Seeding.Fakers;

public class Location
{
    public required string County { get; set; }
    public required string Town { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class LocationFaker : ILocationFaker
{
    private int _index = 0;

    private static readonly Location[] Locations =
    [
        new Location { County = "Leicestershire",    Town = "Loughborough", Latitude = 52.7721, Longitude = -1.2062 },
        new Location { County = "Greater London",    Town = "London",       Latitude = 51.5074, Longitude = -0.1278 },
        new Location { County = "Greater Manchester",Town = "Manchester",   Latitude = 53.4808, Longitude = -2.2426 },
        new Location { County = "Surrey",            Town = "Guildford",    Latitude = 51.2362, Longitude = -0.5704 },
        new Location { County = "West Yorkshire",    Town = "Leeds",        Latitude = 53.8008, Longitude = -1.5491 },
        new Location { County = "West Midlands",     Town = "Birmingham",   Latitude = 52.4862, Longitude = -1.8904 },
        new Location { County = "Tyne and Wear",     Town = "Newcastle",    Latitude = 54.9783, Longitude = -1.6178 },
        new Location { County = "South Yorkshire",   Town = "Sheffield",    Latitude = 53.3811, Longitude = -1.4701 },
        new Location { County = "Merseyside",        Town = "Liverpool",    Latitude = 53.4084, Longitude = -2.9916 },
        new Location { County = "Bristol",           Town = "Bristol",      Latitude = 51.4545, Longitude = -2.5879 },
        new Location { County = "Nottinghamshire",   Town = "Nottingham",   Latitude = 52.9548, Longitude = -1.1581 },
        new Location { County = "Hampshire",         Town = "Southampton",  Latitude = 50.9097, Longitude = -1.4043 },
        new Location { County = "Lancashire",        Town = "Preston",      Latitude = 53.7632, Longitude = -2.7031 },
        new Location { County = "Cambridgeshire",    Town = "Cambridge",    Latitude = 52.2053, Longitude =  0.1218 },
        new Location { County = "Oxfordshire",       Town = "Oxford",       Latitude = 51.7520, Longitude = -1.2577 },
    ];

    public Location Next() => Locations[_index++ % Locations.Length];
}
