namespace Concertable.B2B.Authorization.Contracts.Enums;

/// <summary>
/// Why a request-free caller is executing. Establishing one is the only way to reach the unfiltered
/// stance: the absence of an HTTP request grants nothing on its own.
/// </summary>
public enum ExecutionPurpose
{
    /// <summary>A background worker, outbox dispatcher or projection handler acting for the platform itself.</summary>
    System = 1,

    /// <summary>Seeding a database outside a request.</summary>
    Seeding = 2,

    /// <summary>Admin moderation, which additionally requires current admin authority on the acting human.</summary>
    Moderation = 3,
}
