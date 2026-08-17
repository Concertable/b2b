namespace Concertable.B2B.Application.Infrastructure;

internal sealed class LegalSettings
{
    public const string SectionName = "Legal";
    public string PlatformTermsVersion { get; set; } = null!;
}
