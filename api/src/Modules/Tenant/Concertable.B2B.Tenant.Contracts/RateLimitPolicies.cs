namespace Concertable.B2B.Tenant.Contracts;

public static class RateLimitPolicies
{
    public const string PublicRead = "public-read";
    public const string Upload = "upload";
    public const string Apply = "apply";
    public const string Messaging = "messaging";
    public const string Checkout = "checkout";
    public const string ProfileImage = "profile-image";

    /// <summary>Expensive or destructive authenticated operations — an admin export that fans across modules,
    /// an irreversible subject erasure. Throttled on the cost/blast-radius axis, not because the caller is
    /// untrusted, so it partitions per user. A tight cap is a safety net against a runaway loop or a
    /// compromised token; it costs legitimate low-volume use nothing.</summary>
    public const string Sensitive = "sensitive";

    public static readonly IReadOnlyList<string> All =
        [PublicRead, Upload, Apply, Messaging, Checkout, ProfileImage, Sensitive];
}
