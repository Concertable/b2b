namespace Concertable.B2B.Venue.Contracts;

/// <summary>A venue tenant's display name and business email, for cross-module admin listing and
/// notification (verification review). Declared per module rather than shared, matching <see cref="DisplayNames"/>.</summary>
public sealed record TenantContact(string Name, string Email);
