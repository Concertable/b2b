namespace Concertable.B2B.Application.Contracts.Enums;

/// <summary>
/// What a grant on an application lets a tenant read. The proposal is separate from the summary because
/// knowing an act applied is not the same as reading what it asked for.
/// </summary>
public enum ApplicationAccessScope
{
    /// <summary>Who applied, to what, and the application state.</summary>
    Summary = 1,

    /// <summary>The proposed terms under consideration.</summary>
    Proposal = 2,
}
