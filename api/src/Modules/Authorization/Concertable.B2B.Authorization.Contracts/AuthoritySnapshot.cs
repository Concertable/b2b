namespace Concertable.B2B.Authorization.Contracts;

public sealed record AuthoritySnapshot(MembershipSnapshot Actor, string CatalogRevision)
{
    public bool IsCurrentFor(MembershipSnapshot requestActor) =>
        Actor.HasSameAuthorityAs(requestActor) && CatalogRevision == AuthorizationCatalog.Revision;
}
