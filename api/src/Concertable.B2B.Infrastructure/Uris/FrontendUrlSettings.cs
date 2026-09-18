namespace Concertable.B2B.Infrastructure.Uris;

internal sealed class FrontendUrlSettings
{
    public const string SectionName = "Urls";

    public Dictionary<FrontendSurface, string> Frontends { get; set; } = new();
}
