using System.Collections.Immutable;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Authorization.Infrastructure.Authorization;
using Reunion;

namespace Concertable.B2B.Authorization.UnitTests;

public sealed class ResourceAuthorizationTests
{
    private static readonly MembershipSnapshot Actor = new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, 5,
        ImmutableDictionary<TenantPermission, ResourceAudience>.Empty.Add(
            TenantPermission.OperationsView, ResourceAudience.TenantResources));

    [Fact]
    public void ResourceAddress_RejectsInvalidKindAndId()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ResourceAddress.Create(default, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ResourceAddress.Create(ResourceKind.Application, 0));
        Assert.Equal(ResourceAddress.Create(ResourceKind.Application, 7),
            ResourceAddress.Create(ResourceKind.Application, 7));
    }

    [Fact]
    public void Registry_RequiresExactFacetWhenBindingsAreAmbiguous()
    {
        var registry = new ResourceBindingRegistry(
            [Descriptor(
                new PermissionResourceBinding("booking", "Summary", "booking_grant", ["Summary"]),
                new PermissionResourceBinding("booking", "Operations", "booking_grant", ["Operations"]))],
            [new Evaluator(ResourceKind.Booking)]);

        Assert.False(registry.TryResolve(new AuthorizationRequest(
            TenantPermission.OperationsView, ResourceAddress.Create(ResourceKind.Booking, 1)), out _));
        Assert.True(registry.TryResolve(new AuthorizationRequest(
            TenantPermission.OperationsView, ResourceAddress.Create(ResourceKind.Booking, 1),
            ResourceFacet.Operations), out var binding));
        Assert.Equal<string>(["Operations"], binding!.RequiredScopes);
    }

    [Fact]
    public void Registry_FailsCompositionForMissingEvaluatorOrWrongPolicy()
    {
        Assert.Throws<InvalidOperationException>(() => new ResourceBindingRegistry(
            [Descriptor(new PermissionResourceBinding("application", "Summary", "application_grant", ["Summary"]))], []));
        Assert.Throws<InvalidOperationException>(() => new ResourceBindingRegistry(
            [Descriptor(new PermissionResourceBinding("application", "Summary", "invoice_grant", ["Summary"]))],
            [new Evaluator(ResourceKind.Application)]));
    }

    [Fact]
    public async Task Require_ReacquiresAuthorityForEachCommandInSameScope()
    {
        var context = new AuthorizationContext();
        var resolver = new AuthorityResolver();
        var service = new ResourceAuthorization(
            new ActorAuthoritySession(new MembershipContext(), resolver, context), context,
            new ResourceBindingRegistry(
                [Descriptor(new PermissionResourceBinding("application", "Summary", "application_grant", ["Summary"]))],
                [new Evaluator(ResourceKind.Application)]),
            TimeProvider.System);
        var request = new AuthorizationRequest(TenantPermission.OperationsView,
            ResourceAddress.Create(ResourceKind.Application, 1), ResourceFacet.Summary);

        context.UnitOfWorkId = Guid.NewGuid();
        Assert.Equal(AuthorizationDecision.Allowed, await service.RequireAsync(request));
        context.UnitOfWorkId = Guid.NewGuid();
        Assert.Equal(AuthorizationDecision.Allowed, await service.RequireAsync(request));

        Assert.Equal(2, resolver.UnitOfWorkResolutions);
        Assert.Equal(4, context.ValidatorCount);
    }

    [Fact]
    public async Task Check_DeniesEvidenceMissingItsRequiredScope()
    {
        var context = new AuthorizationContext();
        var service = new ResourceAuthorization(
            new ActorAuthoritySession(new MembershipContext(), new AuthorityResolver(), context), context,
            new ResourceBindingRegistry(
                [Descriptor(new PermissionResourceBinding("application", "Summary", "application_grant", ["Summary"]))],
                [new Evaluator(ResourceKind.Application, includeScope: false)]),
            TimeProvider.System);

        var decision = await service.CheckAsync(new AuthorizationRequest(
            TenantPermission.OperationsView, ResourceAddress.Create(ResourceKind.Application, 1),
            ResourceFacet.Summary));

        Assert.Equal(AuthorizationDecision.Denied, decision);
    }

    [Fact]
    public async Task TenantCapability_RequiresTenantAudienceAndSharesFrozenCommandAuthority()
    {
        var tenantBinding = new PermissionResourceBinding("tenant", null, "membership", []);
        var tenantDescriptor = new PermissionDescriptor(
            TenantPermission.MessagesSend, "Send messages", "Conversations", [tenantBinding],
            [ResourceAudience.AssignedResources, ResourceAudience.TenantResources], false);
        var actor = Actor with
        {
            Permissions = Actor.Permissions.Add(TenantPermission.MessagesSend, ResourceAudience.TenantResources),
        };
        var context = new AuthorizationContext { UnitOfWorkId = Guid.NewGuid() };
        var resolver = new AuthorityResolver();
        var session = new ActorAuthoritySession(new MembershipContext(actor), resolver, context);
        var resources = new ResourceAuthorization(session, context,
            new ResourceBindingRegistry(
                [Descriptor(new PermissionResourceBinding("application", "Summary", "application_grant", ["Summary"]))],
                [new Evaluator(ResourceKind.Application)]),
            TimeProvider.System);
        var tenant = new TenantCapabilityAuthorization(session, new TenantCapabilityRegistry([tenantDescriptor]));

        Assert.Equal(AuthorizationDecision.Allowed, await resources.RequireAsync(new AuthorizationRequest(
            TenantPermission.OperationsView, ResourceAddress.Create(ResourceKind.Application, 1),
            ResourceFacet.Summary)));
        Assert.Equal(AuthorizationDecision.Allowed, await tenant.RequireAsync(TenantPermission.MessagesSend));
        Assert.Equal(1, resolver.UnitOfWorkResolutions);
        Assert.Equal(2, context.ValidatorCount);

        var assignedActor = actor with
        {
            Permissions = actor.Permissions.SetItem(
                TenantPermission.MessagesSend, ResourceAudience.AssignedResources),
        };
        var assignedSession = new ActorAuthoritySession(
            new MembershipContext(assignedActor), new AuthorityResolver(), new AuthorizationContext());
        var assignedTenant = new TenantCapabilityAuthorization(
            assignedSession, new TenantCapabilityRegistry([tenantDescriptor]));
        Assert.Equal(AuthorizationDecision.Denied,
            await assignedTenant.CheckAsync(TenantPermission.MessagesSend));
    }

    private static PermissionDescriptor Descriptor(params PermissionResourceBinding[] bindings) =>
        new(TenantPermission.OperationsView, "Operations", "Tenant", bindings,
            [ResourceAudience.TenantResources], false);

    private sealed class MembershipContext(MembershipSnapshot? snapshot = null) : IMembershipContext
    {
        public MembershipSnapshot? Membership => snapshot ?? Actor;
        public bool HasPermission(TenantPermission permission) => Membership!.HasPermission(permission);
        public ResourceAudience AudienceFor(TenantPermission permission) => Membership!.AudienceFor(permission);
    }

    private sealed class AuthorityResolver : IAuthorityResolver
    {
        public int UnitOfWorkResolutions { get; private set; }

        public Task<Option<AuthoritySnapshot>> ResolveAsync(MembershipSnapshot actor, CancellationToken ct = default) =>
            Task.FromResult<Option<AuthoritySnapshot>>(new AuthoritySnapshot(actor, AuthorizationCatalog.Revision));

        public Task<Option<AuthoritySnapshot>> ResolveForUnitOfWorkAsync(
            MembershipSnapshot actor, CancellationToken ct = default)
        {
            UnitOfWorkResolutions++;
            return ResolveAsync(actor, ct);
        }

        public Task<bool> ValidateForCommitAsync(AuthoritySnapshot original, CancellationToken ct = default) =>
            Task.FromResult(true);
    }

    private sealed class AuthorizationContext : IAuthorizationContext
    {
        public bool IsActive => UnitOfWorkId is not null;
        public Guid? UnitOfWorkId { get; set; }
        public int ValidatorCount { get; private set; }
        public void RegisterFailure<TResult>(Func<TResult> authorityFailure) { }
        public void RegisterValidator(Func<CancellationToken, Task<bool>> validator) => ValidatorCount++;
        public void MarkAuthorityFailed() => throw new InvalidOperationException("Unexpected authorization denial.");
    }

    private sealed class Evaluator(ResourceKind kind, bool includeScope = true) : IResourceAuthorizationEvaluator
    {
        public ResourceKind Kind => kind;

        public Task<ResourceAuthorizationDecision> CheckAsync(
            AuthorizationRequest request, ResourcePolicyBinding binding, MembershipSnapshot actor,
            DateTimeOffset now, CancellationToken ct = default) =>
            Task.FromResult(ResourceAuthorizationDecision.Allow(null,
                includeScope
                    ? [new ResourceGrantSnapshot("Summary", Guid.NewGuid(), 1, now.AddMinutes(-1), now.AddMinutes(1))]
                    : []));

        public Task<ResourceAuthorizationDecision> RequireAsync(
            AuthorizationRequest request, ResourcePolicyBinding binding, MembershipSnapshot actor,
            DateTimeOffset now, CancellationToken ct = default) => CheckAsync(request, binding, actor, now, ct);

        public Task<bool> ValidateForCommitAsync(
            ResourceAuthorizationSnapshot snapshot, DateTimeOffset now, CancellationToken ct = default) =>
            Task.FromResult(true);
    }
}
