namespace Concertable.B2B.Conversations.Infrastructure;

/// <summary>
/// Online Safety Act configuration for the Conversations module, bound from the <c>Safety</c> config
/// section.
/// </summary>
public sealed class SafetySettings
{
    public const string SectionName = "Safety";

    /// <summary>
    /// The inbox illegal-content reports are delivered to. Overridden per environment; the committed
    /// default deliberately uses a reserved <c>.invalid</c> domain so a mis-deployed environment cannot
    /// mail a real stranger.
    /// </summary>
    public string ReportInboxEmail { get; set; } = null!;
}
