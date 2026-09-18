namespace Concertable.B2B.DataAccess.Application;

/// <summary>
/// Why a resource access grant exists. Kept on the row because revoking a share and a principal losing its
/// own access to a resource it took part in are different acts, and an audit has to tell them apart.
/// </summary>
public enum GrantOrigin
{
    /// <summary>Issued to a principal when the resource itself was created.</summary>
    ResourceCreation = 1,

    /// <summary>Issued by a member of a tenant whose sharing policy allows it to disclose this resource.</summary>
    ExplicitShare = 2,
}
