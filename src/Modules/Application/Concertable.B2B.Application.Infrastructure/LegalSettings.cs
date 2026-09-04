namespace Concertable.B2B.Application.Infrastructure;

internal sealed class LegalSettings
{
    public const string SectionName = "Legal";
    public string PlatformTermsVersion { get; set; } = null!;

    /// <summary>The variable-amount merchant-initiated mandate terms the payer accepts at payment-method
    /// setup. Payment's operation row is the authoritative consent evidence; the contract keeps B2B's
    /// frozen record of what was agreed.</summary>
    public string MandateTermsVersion { get; set; } = null!;
}
