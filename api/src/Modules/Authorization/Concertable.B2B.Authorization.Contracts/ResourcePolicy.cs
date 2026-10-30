using System.Text.Json;

namespace Concertable.B2B.Authorization.Contracts;

public enum ResourcePolicy
{
    Membership = 1,
    ApplicationGrant,
    BookingGrant,
    ConcertGrant,
    ContractGrant,
    ConversationGrant,
    InvoiceGrant,
    VenuePrincipal,
    ArtistPrincipal,
    EitherPrincipal,
    PrincipalAdministration,
}

public static class ResourcePolicyExtensions
{
    extension(ResourcePolicy policy)
    {
        public bool IsPrincipal => policy is ResourcePolicy.VenuePrincipal or ResourcePolicy.ArtistPrincipal
            or ResourcePolicy.EitherPrincipal or ResourcePolicy.PrincipalAdministration;

        public string WireName => JsonNamingPolicy.SnakeCaseLower.ConvertName(policy.ToString());
    }
}
