namespace Concertable.B2B.DataAccess.Application;

/// <summary>
/// Why a resource access grant exists. Kept on the row because a principal's own entitlement, a disclosure to
/// another business and a member's operational assignment are different acts with different revocation rules,
/// and an audit has to tell them apart.
/// </summary>
public enum ResourceGrantKind
{
    /// <summary>Issued to a principal when the resource itself was created. Neither share nor assignment management can revoke it.</summary>
    Principal = 1,

    /// <summary>Issued by a principal disclosing the resource's summary to another business.</summary>
    SharedSummary = 2,

    /// <summary>Issued by a principal to one of its own current memberships, for assigned operational work.</summary>
    MemberAssignment = 3,
}
