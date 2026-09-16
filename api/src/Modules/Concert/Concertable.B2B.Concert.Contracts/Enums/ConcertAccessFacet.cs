namespace Concertable.B2B.Concert.Contracts.Enums;

/// <summary>
/// What a grant on a concert lets a tenant read. Finance is always granted separately: an operational
/// participant runs the show without seeing what anyone is paid.
/// </summary>
public enum ConcertAccessFacet
{
    /// <summary>When and where, who is on, and the concert's own state. No money.</summary>
    Summary = 1,

    /// <summary>The operational detail an assigned production member needs on the day.</summary>
    Operations = 2,

    /// <summary>Fees, settlement figures and the financial actions on the engagement.</summary>
    Finance = 3,
}
