namespace Concertable.B2B.User.Infrastructure.Settings;

internal sealed class AdminOptions
{
    public const string SectionName = "Admin";

    /// <summary>The one-time first-admin email. Registering via the admin client with this email grants
    /// <c>AdminProfileEntity</c> when no admin exists yet. Null disables bootstrap (Production default,
    /// until set as an operational step before launch).</summary>
    public string? BootstrapEmail { get; set; }
}
