namespace Concertable.B2B.Concert.Infrastructure;

/// <summary>
/// Legal/compliance configuration for the Concert module, bound from the <c>Legal</c> config section.
/// Currently holds only the platform terms &amp; conditions version in force (<see cref="PlatformTermsVersion"/>).
/// </summary>
public sealed class LegalSettings
{
    /// <summary>
    /// Identifier of the platform terms &amp; conditions in force (e.g. "2026-07"). Stamped onto each
    /// <c>ContractEntity</c> when it is signed and frozen there — the agreement keeps the value
    /// that applied at signing, so bumping this config later doesn't rewrite historical agreements.
    /// Rendered on the agreement PDF as the legal record of which terms the parties agreed to.
    /// </summary>
    public string PlatformTermsVersion { get; set; } = null!;
}
