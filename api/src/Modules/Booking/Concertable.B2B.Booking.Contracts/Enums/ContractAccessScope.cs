namespace Concertable.B2B.Booking.Contracts.Enums;

/// <summary>
/// What a grant on a contract lets a tenant read. Separate from the booking scopes on purpose: holding a
/// booking summary must never produce a contract download.
/// </summary>
public enum ContractAccessScope
{
    /// <summary>The agreed terms and their document.</summary>
    Terms = 1,
}
