namespace Concertable.B2B.Artist.Contracts;

/// <summary>An artist tenant's display name and business email, for cross-module admin listing and
/// notification (verification review). Declared per module rather than shared, matching <see cref="DisplayNames"/>.</summary>
public readonly record struct TenantContact(string Name, string Email);
