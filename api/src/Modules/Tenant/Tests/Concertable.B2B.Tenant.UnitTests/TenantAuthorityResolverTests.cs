using System.Collections.Immutable;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.Tenant.Infrastructure.Authorization;
using Concertable.B2B.Tenant.Infrastructure.Resolvers;
using Moq;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TenantAuthorityResolverTests
{
    [Fact]
    public async Task SelfDowngrade_ContinuousVersionTransitionRetainsOriginalProof()
    {
        var fixture = new Fixture();
        Assert.NotNull(await fixture.Authority.ProveAdministrationAsync(
            fixture.Tenant.Id, TenantPermission.MembersManageRoles, true));
        Assert.True(await fixture.Authority.TrackMembershipAsync(fixture.Member));

        var before = fixture.Member.PermissionVersion;
        fixture.Member.ReplaceRoles([Guid.NewGuid()], fixture.Member.Id, DateTime.UtcNow);
        fixture.Authority.RecordMembershipVersion(fixture.Member, before);
        fixture.CurrentPermissions = ImmutableDictionary<TenantPermission, ResourceAudience>.Empty;

        var retained = await fixture.Authority.ResolveForCommandAsync(fixture.Original);
        Assert.True(retained.TryGetValue(out var authority));
        Assert.True(authority.Actor.HasPermission(TenantPermission.MembersManageRoles));
        Assert.True(await fixture.Authority.ValidateForCommitAsync(fixture.OriginalAuthority));
    }

    [Fact]
    public async Task ResolveMany_AfterCheckedSelfDowngradeUsesOriginalActor()
    {
        var fixture = new Fixture();
        Assert.NotNull(await fixture.Authority.ProveAdministrationAsync(
            fixture.Tenant.Id, TenantPermission.MembersManageRoles, true));
        Assert.True(await fixture.Authority.TrackMembershipAsync(fixture.Member));
        var before = fixture.Member.PermissionVersion;
        fixture.Member.ReplaceRoles([Guid.NewGuid()], fixture.Member.Id, DateTime.UtcNow);
        fixture.Authority.RecordMembershipVersion(fixture.Member, before);
        fixture.CurrentPermissions = ImmutableDictionary<TenantPermission, ResourceAudience>.Empty;

        var resolved = await fixture.Resolver.ResolveManyAsync(fixture.Original, [fixture.Tenant.Id]);

        Assert.True(resolved.TryGetValue(out var value));
        Assert.True(value.Actor.HasPermission(TenantPermission.MembersManageRoles));
        Assert.Contains(fixture.Tenant.Id, value.ExistingTenantIds);
    }

    [Fact]
    public async Task MembershipVersionTransition_RejectsAContinuityGap()
    {
        var fixture = new Fixture();
        Assert.NotNull(await fixture.Authority.ProveAdministrationAsync(
            fixture.Tenant.Id, TenantPermission.MembersManageRoles, true));
        Assert.True(await fixture.Authority.TrackMembershipAsync(fixture.Member));

        var before = fixture.Member.PermissionVersion;
        fixture.Member.ReplaceRoles([Guid.NewGuid()], fixture.Member.Id, DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            fixture.Authority.RecordMembershipVersion(fixture.Member, before + 1));
    }

    [Fact]
    public async Task SelfRemoval_CheckedRemovalKeepsTheOriginalProof()
    {
        var fixture = new Fixture();
        Assert.NotNull(await fixture.Authority.ProveAdministrationAsync(
            fixture.Tenant.Id, TenantPermission.MembersRemove, true));
        Assert.True(await fixture.Authority.TrackMembershipAsync(fixture.Member));

        fixture.Authority.RecordMembershipRemoval(fixture.Member);
        fixture.MemberPresent = false;

        Assert.True(await fixture.Authority.ValidateForCommitAsync(fixture.OriginalAuthority));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ValidateForCommit_MembershipChangeLeavesNoOwner_Rejects(bool remove)
    {
        var fixture = new Fixture();
        Assert.NotNull(await fixture.Authority.ProveAdministrationAsync(
            fixture.Tenant.Id, TenantPermission.MembersManageRoles, true));
        Assert.True(await fixture.Authority.TrackMembershipAsync(fixture.Member));
        if (remove)
        {
            fixture.Authority.RecordMembershipRemoval(fixture.Member);
            fixture.MemberPresent = false;
        }
        else
        {
            var before = fixture.Member.PermissionVersion;
            fixture.Member.ReplaceRoles([Guid.NewGuid()], fixture.Member.Id, DateTime.UtcNow);
            fixture.Authority.RecordMembershipVersion(fixture.Member, before);
        }
        fixture.OwnerCount = 0;

        Assert.False(await fixture.Authority.ValidateForCommitAsync(fixture.OriginalAuthority));
    }

    private sealed class Fixture
    {
        private readonly Mock<ITenantRepository> tenants = new();
        private readonly Mock<IMembershipRepository> memberships = new();
        private readonly Mock<IRoleRepository> roles = new();
        private readonly Mock<IInvitationRepository> invitations = new();
        private readonly Mock<IMembershipContext> membershipContext = new();
        private readonly Mock<ICommandAuthorizationContext> command = new();

        public Fixture()
        {
            var tenantId = Guid.NewGuid();
            Tenant = TenantEntity.Create("Acme", "contact@acme.test", Guid.NewGuid(), DateTime.UtcNow, tenantId);
            Member = TenantMembershipEntity.Create(tenantId, Guid.NewGuid(),
                [Guid.NewGuid()], null, DateTime.UtcNow);
            CurrentPermissions = AuthorizationCatalog.Presets["Owner"].Permissions.ToImmutableDictionary();
            Original = Snapshot();
            OriginalAuthority = new AuthoritySnapshot(Original, AuthorizationCatalog.Revision);
            membershipContext.SetupGet(value => value.Membership).Returns(Original);
            command.SetupGet(value => value.IsActive).Returns(true);
            command.SetupGet(value => value.TransactionId).Returns(Guid.NewGuid());
            tenants.Setup(value => value.GetExistingIdsForShareAsync(
                    It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                    (IReadOnlySet<Guid>)ids.Where(id => id == tenantId).ToHashSet());
            tenants.Setup(value => value.GetAuthorizationCatalogRevisionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(AuthorizationCatalog.Revision);
            tenants.Setup(value => value.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Tenant);
            memberships.Setup(value => value.GetSnapshotsByIdsForShareAsync(
                    It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                .Returns((IReadOnlyCollection<Guid> _, CancellationToken _) =>
                    Task.FromResult<IReadOnlyList<MembershipSnapshot>>([Snapshot()]));
            memberships.Setup(value => value.GetSnapshotsByIdsForUpdateAsync(
                    It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                .Returns((IReadOnlyCollection<Guid> _, CancellationToken _) =>
                    Task.FromResult<IReadOnlyList<MembershipSnapshot>>([Snapshot()]));
            memberships.Setup(value => value.GetSnapshotByMembershipIdAsync(
                    Member.Id, It.IsAny<CancellationToken>()))
                .Returns((Guid _, CancellationToken _) =>
                    Task.FromResult<MembershipSnapshot?>(MemberPresent ? Snapshot() : null));
            memberships.Setup(value => value.IsMemberAsync(tenantId, Member.UserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => MemberPresent);
            memberships.Setup(value => value.CountOwnersAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => OwnerCount);
            roles.Setup(value => value.HasProtectedOwnerAsync(tenantId, Member.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            Authority = new TenantAuthorityResolver(tenants.Object, memberships.Object,
                roles.Object, invitations.Object, membershipContext.Object, command.Object,
                TimeProvider.System);
            Resolver = new TenantResolver(tenants.Object, memberships.Object, Authority);
        }

        public TenantEntity Tenant { get; }
        public TenantMembershipEntity Member { get; }
        public MembershipSnapshot Original { get; }
        public AuthoritySnapshot OriginalAuthority { get; }
        public TenantAuthorityResolver Authority { get; }
        public TenantResolver Resolver { get; }
        public bool MemberPresent { get; set; } = true;
        public int OwnerCount { get; set; } = 1;
        public ImmutableDictionary<TenantPermission, ResourceAudience> CurrentPermissions { get; set; }

        private MembershipSnapshot Snapshot() =>
            new(Member.Id, Member.TenantId, Member.UserId, Member.PermissionVersion,
                Tenant.RolePolicyVersion, CurrentPermissions);
    }
}
