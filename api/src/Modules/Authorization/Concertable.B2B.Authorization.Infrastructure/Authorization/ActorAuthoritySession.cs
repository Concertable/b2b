using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Authorization.Infrastructure.Authorization;

internal sealed record ActorAuthorityResolution(
    AuthorizationDecision Decision,
    AuthoritySnapshot? Authority);

internal sealed class ActorAuthoritySession(
    IMembershipContext membership,
    IAuthorityResolver resolver,
    ICommandAuthorizationContext command)
{
    private Guid? transactionId;
    private AuthoritySnapshot? retained;

    public async Task<ActorAuthorityResolution> CheckAsync(CancellationToken ct = default)
    {
        if (membership.Membership is not { } requestActor)
            return new(AuthorizationDecision.Denied, null);

        var option = await resolver.ResolveAsync(requestActor, ct);
        if (!option.TryGetValue(out var authority)
            || !authority.Actor.HasSameAuthorityAs(requestActor)
            || authority.CatalogRevision != AuthorizationCatalog.Revision)
            return new(AuthorizationDecision.AuthorityChanged, null);

        return new(AuthorizationDecision.Allowed, authority);
    }

    public async Task<ActorAuthorityResolution> RequireAsync(CancellationToken ct = default)
    {
        if (!command.IsActive || command.TransactionId is not { } currentTransactionId)
            throw new InvalidOperationException("Authorization requires an active command.");

        if (transactionId != currentTransactionId)
        {
            retained = null;
            transactionId = currentTransactionId;
        }

        if (membership.Membership is not { } requestActor)
            return Fail(AuthorizationDecision.Denied);

        if (retained is null)
        {
            var option = await resolver.ResolveForCommandAsync(requestActor, ct);
            if (!option.TryGetValue(out var authority)
                || !authority.Actor.HasSameAuthorityAs(requestActor)
                || authority.CatalogRevision != AuthorizationCatalog.Revision)
                return Fail(AuthorizationDecision.AuthorityChanged);

            retained = authority;
            command.RegisterValidator(token => resolver.ValidateForCommitAsync(authority, token));
        }
        else if (retained.Actor.MembershipId != requestActor.MembershipId
                 || retained.Actor.TenantId != requestActor.TenantId
                 || retained.Actor.UserId != requestActor.UserId)
            return Fail(AuthorizationDecision.AuthorityChanged);

        return new(AuthorizationDecision.Allowed, retained);
    }

    public ActorAuthorityResolution Fail(AuthorizationDecision decision)
    {
        command.MarkAuthorityFailed();
        return new(decision, null);
    }
}
