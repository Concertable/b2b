namespace Concertable.B2B.Authorization.Contracts;

public sealed record AuthoritySnapshot(MembershipSnapshot Actor, string CatalogRevision);
