namespace Concertable.B2B.Hosting;

public static class B2BLocalSpaSurfaces
{
    public static SpaSurface Venue { get; } = new("venue", 5175);
    public static SpaSurface Artist { get; } = new("artist", 5176);
    public static SpaSurface Business { get; } = new("business", 5177);
    public static SpaSurface Admin { get; } = new("admin", 5178);

    public static IReadOnlyList<SpaSurface> All { get; } =
        Array.AsReadOnly([Venue, Artist, Business, Admin]);

    public static IReadOnlyList<(SpaSurface Surface, string ClientName)> AuthClients { get; } =
        Array.AsReadOnly<(SpaSurface Surface, string ClientName)>([
            new(Venue, "Venue"),
            new(Artist, "Artist"),
            new(Admin, "Admin")
        ]);
}
