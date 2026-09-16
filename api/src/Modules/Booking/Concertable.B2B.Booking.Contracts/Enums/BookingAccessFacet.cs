namespace Concertable.B2B.Booking.Contracts.Enums;

/// <summary>
/// What a grant on a booking lets a tenant read. A facet is the whole of what it names: a summary grant never
/// widens into operations, and neither reaches the accepted terms, which are granted on the contract itself.
/// </summary>
public enum BookingAccessFacet
{
    /// <summary>When and where, who is performing, and the booking's own state. No money and no terms.</summary>
    Summary = 1,

    /// <summary>The operational detail a production member needs to do assigned work on the engagement.</summary>
    Operations = 2,
}
