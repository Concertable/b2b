namespace Concertable.B2B.Hosting;

public static class B2BLocalSpaSurfaces
{
    public static SpaSurface Venue { get; } = new("venue", 5175, "Venue");
    public static SpaSurface Artist { get; } = new("artist", 5176, "Artist");
    public static SpaSurface Business { get; } = new("business", 5177, null);
    public static SpaSurface Admin { get; } = new("admin", 5178, "Admin");

    public static IReadOnlyList<SpaSurface> All { get; } =
        Array.AsReadOnly([Venue, Artist, Business, Admin]);
}
