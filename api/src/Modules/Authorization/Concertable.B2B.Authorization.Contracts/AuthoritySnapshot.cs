using Reunion;

namespace Concertable.B2B.Authorization.Contracts;

public sealed record AuthoritySnapshot(MembershipSnapshot Actor, string CatalogRevision);

public interface IAuthorityResolver
{
    Task<Option<AuthoritySnapshot>> ResolveAsync(
        MembershipSnapshot requestActor, CancellationToken ct = default);

    Task<Option<AuthoritySnapshot>> ResolveForUnitOfWorkAsync(
        MembershipSnapshot requestActor, CancellationToken ct = default);

    Task<bool> ValidateForCommitAsync(
        AuthoritySnapshot original, CancellationToken ct = default);
}
