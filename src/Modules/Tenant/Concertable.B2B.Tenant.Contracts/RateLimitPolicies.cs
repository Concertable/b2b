namespace Concertable.B2B.Tenant.Contracts;

public static class RateLimitPolicies
{
    public const string PublicRead = "public-read";
    public const string Upload = "upload";
    public const string Apply = "apply";
    public const string Messaging = "messaging";
    public const string Checkout = "checkout";
    public const string ProfileImage = "profile-image";

    public static readonly IReadOnlyList<string> All =
        [PublicRead, Upload, Apply, Messaging, Checkout, ProfileImage];
}
