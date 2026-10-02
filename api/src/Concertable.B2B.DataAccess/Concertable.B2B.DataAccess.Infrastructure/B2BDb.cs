namespace Concertable.B2B.DataAccess.Infrastructure;

public static class B2BDb
{
    public const string Name = "B2BDb";

    public static IReadOnlyList<string> Schemas { get; } =
    [
        "user",
        "tenant",
        "admin",
        "artist",
        "venue",
        "opportunity",
        "application",
        "booking",
        "concert",
        "deal",
        "conversations",
        "messaging",
    ];
}
