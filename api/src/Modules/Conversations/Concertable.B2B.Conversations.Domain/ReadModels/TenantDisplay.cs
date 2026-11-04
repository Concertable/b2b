namespace Concertable.B2B.Conversations.Domain.ReadModels;

public sealed class TenantDisplay
{
    private TenantDisplay() { }

    public Guid TenantId { get; private set; }
    public long DisplayVersion { get; private set; }
    public string DisplayName { get; private set; } = null!;

    public static TenantDisplay Create(Guid tenantId, long displayVersion, string displayName) => new()
    {
        TenantId = tenantId,
        DisplayVersion = displayVersion,
        DisplayName = displayName
    };

    public bool Apply(long displayVersion, string displayName)
    {
        if (displayVersion <= DisplayVersion)
            return false;

        DisplayVersion = displayVersion;
        DisplayName = displayName;
        return true;
    }
}
