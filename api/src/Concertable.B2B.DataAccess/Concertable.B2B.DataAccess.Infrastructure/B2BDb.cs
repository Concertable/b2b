namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>The B2B service's database connection string name — must match the AppHost resource name.</summary>
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
