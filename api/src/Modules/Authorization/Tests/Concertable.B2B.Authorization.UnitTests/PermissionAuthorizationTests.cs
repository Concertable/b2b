using System.Collections.Immutable;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Authorization.Infrastructure.Authorization;
using Reunion;

namespace Concertable.B2B.Authorization.UnitTests;

public sealed class PermissionAuthorizationTests
{
    private readonly MembershipSnapshot actor;
    private readonly MembershipContext membership;
    private readonly AuthorityResolver resolver;
    private readonly CommandContext context;
    private readonly PermissionAuthorization permissions;

    public PermissionAuthorizationTests()
    {
        this.actor = new MembershipSnapshot(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"), 3, 5,
            ImmutableDictionary<TenantPermission, ResourceAudience>.Empty.Add(
                TenantPermission.OperationsView, ResourceAudience.TenantResources));
        this.membership = new MembershipContext(this.actor);
        this.resolver = new AuthorityResolver(this.actor);
        this.context = new CommandContext();
        this.permissions = new PermissionAuthorization(new ActorAuthoritySession(
            this.membership, this.resolver, this.context));
    }

    [Theory]
    [InlineData(ResourceAudience.AssignedResources)]
    [InlineData(ResourceAudience.TenantResources)]
    public async Task Check_ResourceReadPermission_AllowsEitherGrantedAudience(ResourceAudience audience)
    {
        var actor = this.actor with
        {
            Permissions = this.actor.Permissions.Add(TenantPermission.TermsRead, audience),
        };
        this.membership.Membership = actor;
        this.resolver.Current = new AuthoritySnapshot(actor, AuthorizationCatalog.Revision);

        Assert.Equal(AuthorizationDecision.Allowed,
            await this.permissions.CheckAsync(TenantPermission.TermsRead));
    }

    [Theory]
    [InlineData(ResourceAudience.AssignedResources, AuthorizationDecision.Denied)]
    [InlineData(ResourceAudience.TenantResources, AuthorizationDecision.Allowed)]
    [InlineData(ResourceAudience.None, AuthorizationDecision.Denied)]
    public async Task Check_CreationPermission_RequiresTenantAudience(
        ResourceAudience audience, AuthorizationDecision expected)
    {
        var actor = this.actor with
        {
            Permissions = this.actor.Permissions.Add(TenantPermission.ApplicationsSubmit, audience),
        };
        this.membership.Membership = actor;
        this.resolver.Current = new AuthoritySnapshot(actor, AuthorizationCatalog.Revision);

        Assert.Equal(expected, await this.permissions.CheckAsync(
            TenantPermission.ApplicationsSubmit, ResourceAudience.TenantResources));
    }

    [Fact]
    public async Task Require_DeniedCreationAudience_PoisonsCommand()
    {
        var actor = this.actor with
        {
            Permissions = this.actor.Permissions.Add(
                TenantPermission.ApplicationsSubmit, ResourceAudience.AssignedResources),
        };
        this.membership.Membership = actor;
        this.resolver.Current = new AuthoritySnapshot(actor, AuthorizationCatalog.Revision);
        this.context.TransactionId = Guid.NewGuid();

        Assert.Equal(AuthorizationDecision.Denied, await this.permissions.RequireAsync(
            TenantPermission.ApplicationsSubmit, ResourceAudience.TenantResources));
        Assert.True(this.context.AuthorityFailed);
    }

    [Fact]
    public async Task Require_DoesNotUsePermissionAddedWithinSameCommand()
    {
        this.context.TransactionId = Guid.NewGuid();
        Assert.Equal(AuthorizationDecision.Allowed, await this.permissions.RequireAsync(
            TenantPermission.OperationsView, ResourceAudience.TenantResources));
        var changed = this.actor with
        {
            PermissionVersion = this.actor.PermissionVersion + 1,
            Permissions = this.actor.Permissions.Add(
                TenantPermission.ApplicationsSubmit, ResourceAudience.TenantResources),
        };
        this.membership.Membership = changed;
        this.resolver.Current = new AuthoritySnapshot(changed, AuthorizationCatalog.Revision);

        Assert.Equal(AuthorizationDecision.Denied, await this.permissions.RequireAsync(
            TenantPermission.ApplicationsSubmit, ResourceAudience.TenantResources));
        Assert.True(this.context.AuthorityFailed);
    }

    [Fact]
    public async Task Check_ChangedCatalog_ReturnsAuthorityChanged()
    {
        this.resolver.Current = new AuthoritySnapshot(this.actor, "stale");

        Assert.Equal(AuthorizationDecision.AuthorityChanged,
            await this.permissions.CheckAsync(TenantPermission.OperationsView));
    }

    [Theory]
    [InlineData(ResourceAudience.None)]
    [InlineData((ResourceAudience)99)]
    public async Task Require_InvalidAudience_PoisonsCommand(ResourceAudience audience)
    {
        this.context.TransactionId = Guid.NewGuid();

        Assert.Equal(AuthorizationDecision.Denied,
            await this.permissions.RequireAsync(TenantPermission.OperationsView, audience));
        Assert.True(this.context.AuthorityFailed);
    }

    private sealed class MembershipContext : IMembershipContext
    {
        public MembershipSnapshot? Membership { get; set; }
        public MembershipContext(MembershipSnapshot actor) => this.Membership = actor;
        public bool HasPermission(TenantPermission permission) => this.Membership!.HasPermission(permission);
        public ResourceAudience AudienceFor(TenantPermission permission) => this.Membership!.AudienceFor(permission);
    }

    private sealed class AuthorityResolver : IAuthorityResolver
    {
        public AuthoritySnapshot? Current { get; set; }
        public AuthorityResolver(MembershipSnapshot actor) =>
            this.Current = new AuthoritySnapshot(actor, AuthorizationCatalog.Revision);
        public Task<Option<AuthoritySnapshot>> ResolveAsync(MembershipSnapshot expected, CancellationToken ct = default) =>
            Task.FromResult(this.Current.ToOption());
        public Task<Option<AuthoritySnapshot>> ResolveForCommandAsync(
            MembershipSnapshot expected, CancellationToken ct = default) => this.ResolveAsync(expected, ct);
        public Task<bool> ValidateForCommitAsync(AuthoritySnapshot original, CancellationToken ct = default) =>
            Task.FromResult(this.Current is { } current && current.CatalogRevision == original.CatalogRevision
                && current.Actor.HasSameAuthorityAs(original.Actor));
    }

    private sealed class CommandContext : ICommandAuthorizationContext
    {
        public bool IsActive => this.TransactionId is not null;
        public Guid? TransactionId { get; set; }
        public bool AuthorityFailed { get; private set; }
        public void RegisterFailure<TResult>(Func<TResult> authorityFailure) { }
        public void RegisterValidator(Func<CancellationToken, Task<bool>> validator) { }
        public void MarkAuthorityFailed() => this.AuthorityFailed = true;
    }
}
