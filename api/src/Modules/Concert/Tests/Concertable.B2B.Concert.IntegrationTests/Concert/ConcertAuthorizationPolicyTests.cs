using System.Collections.Immutable;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Contracts.Enums;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Repositories;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.B2B.DataAccess.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

[Collection("Integration")]
public sealed class ConcertAuthorizationPolicyTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public ConcertAuthorizationPolicyTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync()
    {
        fixture.DetachOutput();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task OperationsAssignmentIsReadableWithoutSummary()
    {
        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ConcertPrivilegedDbContext>();
        var reads = services.GetRequiredService<IConcertPrivilegedReadRepository>();
        var evaluator = services.GetServices<IResourceAuthorizationEvaluator>()
            .Single(candidate => candidate.Kind == ResourceKind.Concert);
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking);
        var tenantId = TenantOf(fixture.SeedState.VenueManager2.Id);
        var actor = await ActorAsync(
            context, tenantId, fixture.SeedState.VenueManager2.Id,
            TenantPermission.OperationsView, ResourceAudience.AssignedResources);
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow();
        var grant = ConcertAccessGrant.Issue(
            concert.Id, tenantId, actor.MembershipId, ConcertAccessScope.Operations,
            concert.VenueTenantId, null, ResourceGrantKind.MemberAssignment, now.UtcDateTime);
        context.ConcertAccessGrants.Add(grant);
        await context.SaveChangesAsync();

        var operations = Binding(TenantPermission.OperationsView, ResourceKind.Concert, ResourceFacet.Operations);
        var summary = Binding(TenantPermission.OperationsView, ResourceKind.Concert, ResourceFacet.Summary);
        var request = new AuthorizationRequest(
            TenantPermission.OperationsView, ResourceAddress.Create(ResourceKind.Concert, concert.Id),
            ResourceFacet.Operations);

        Assert.True((await evaluator.CheckAsync(request, operations, actor, now)).IsAllowed);
        Assert.NotNull(await reads.GetOperationsByIdAsync(concert.Id, operations, actor, now));
        Assert.Equal(1, await context.Concerts.AsNoTracking()
            .Where(ConcertAuthorizationPolicy.Concerts(context, operations, actor, now))
            .CountAsync(candidate => candidate.Id == concert.Id));
        Assert.Null(await reads.GetSummaryByIdAsync(concert.Id, summary, actor, now));
        Assert.Null(await reads.GetFinanceByIdAsync(
            concert.Id,
            Binding(TenantPermission.SettlementView, ResourceKind.Concert, ResourceFacet.Finance),
            Binding(TenantPermission.SettlementView, ResourceKind.Invoice, ResourceFacet.Read),
            actor, now));
    }

    [Fact]
    public async Task FinanceAndInvoiceReadAreIndependent()
    {
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.PastFlatFeeBooking);
        await fixture.FinishConcertAsync(concert.Id);

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ConcertPrivilegedDbContext>();
        var concertReads = services.GetRequiredService<IConcertPrivilegedReadRepository>();
        var invoiceReads = services.GetRequiredService<IInvoicePrivilegedReadRepository>();
        var invoice = await context.Invoices.AsNoTracking()
            .SingleAsync(candidate => candidate.ConcertId == concert.Id);
        var tenantId = TenantOf(fixture.SeedState.VenueManager2.Id);
        var actor = await ActorAsync(
            context, tenantId, fixture.SeedState.VenueManager2.Id,
            TenantPermission.SettlementView, ResourceAudience.TenantResources);
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow();
        var finance = Binding(TenantPermission.SettlementView, ResourceKind.Concert, ResourceFacet.Finance);
        var read = Binding(TenantPermission.SettlementView, ResourceKind.Invoice, ResourceFacet.Read);
        context.ConcertAccessGrants.Add(ConcertAccessGrant.Issue(
            concert.Id, tenantId, actor.MembershipId, ConcertAccessScope.Finance,
            concert.VenueTenantId, null, ResourceGrantKind.MemberAssignment, now.UtcDateTime));
        await context.SaveChangesAsync();

        var financeWithoutInvoice = await concertReads.GetFinanceByIdAsync(
            concert.Id, finance, read, actor, now);
        Assert.NotNull(financeWithoutInvoice);
        Assert.Null(financeWithoutInvoice.InvoiceId);
        Assert.Null(await invoiceReads.GetByIdAsync(invoice.Id, read, actor, now));
        Assert.Null(await concertReads.GetSummaryByIdAsync(
            concert.Id,
            Binding(TenantPermission.OperationsView, ResourceKind.Concert, ResourceFacet.Summary),
            actor, now));

        context.InvoiceAccessGrants.Add(InvoiceAccessGrant.Issue(
            invoice.Id, tenantId, actor.MembershipId, InvoiceAccessScope.Read,
            concert.VenueTenantId, null, ResourceGrantKind.MemberAssignment, now.UtcDateTime));
        await context.SaveChangesAsync();

        Assert.NotNull(await invoiceReads.GetByIdAsync(invoice.Id, read, actor, now));
        Assert.NotNull(await invoiceReads.GetByConcertIdAsync(concert.Id, read, actor, now));
        var financeWithInvoice = await concertReads.GetFinanceByIdAsync(
            concert.Id, finance, read, actor, now);
        Assert.Equal(invoice.Id, financeWithInvoice?.InvoiceId);
    }

    [Fact]
    public async Task MembershipAndPinnedGrantChangesDenyTheSameResource()
    {
        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ConcertPrivilegedDbContext>();
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking);
        var tenantId = TenantOf(fixture.SeedState.VenueManager2.Id);
        var actor = await ActorAsync(
            context, tenantId, fixture.SeedState.VenueManager2.Id,
            TenantPermission.OperationsView, ResourceAudience.AssignedResources);
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow();
        var binding = Binding(TenantPermission.OperationsView, ResourceKind.Concert, ResourceFacet.Operations);
        var grant = ConcertAccessGrant.Issue(
            concert.Id, tenantId, actor.MembershipId, ConcertAccessScope.Operations,
            concert.VenueTenantId, null, ResourceGrantKind.MemberAssignment,
            now.UtcDateTime.AddMinutes(-2));
        context.ConcertAccessGrants.Add(grant);
        await context.SaveChangesAsync();

        var snapshot = new ResourceAuthorizationSnapshot(
            new AuthorizationRequest(
                binding.Permission, ResourceAddress.Create(ResourceKind.Concert, concert.Id), ResourceFacet.Operations),
            binding,
            new AuthoritySnapshot(actor, AuthorizationCatalog.Revision),
            null,
            [new ResourceGrantSnapshot("Operations", grant.Id, grant.Version,
                new DateTimeOffset(grant.ValidFrom, TimeSpan.Zero), null)]);
        Assert.True(await IsVisibleAsync(context, concert.Id, binding, actor, now, snapshot));
        Assert.False(await IsVisibleAsync(
            context, concert.Id, binding, actor with { PermissionVersion = actor.PermissionVersion + 1 },
            now, snapshot));
        Assert.False(await IsVisibleAsync(
            context, concert.Id, binding, actor with { TenantId = Guid.NewGuid() }, now, snapshot));
        Assert.False(await IsVisibleAsync(
            context, concert.Id, binding,
            actor with { Permissions = actor.Permissions.SetItem(
                TenantPermission.OperationsView, ResourceAudience.None) }, now, snapshot));

        grant.Revoke(now.UtcDateTime);
        await context.SaveChangesAsync();
        Assert.False(await IsVisibleAsync(context, concert.Id, binding, actor, now, snapshot));

        context.ConcertAccessGrants.Add(ConcertAccessGrant.Issue(
            concert.Id, tenantId, actor.MembershipId, ConcertAccessScope.Operations,
            concert.VenueTenantId, null, ResourceGrantKind.MemberAssignment,
            now.UtcDateTime.AddHours(-2), now.UtcDateTime.AddHours(-1)));
        await context.SaveChangesAsync();
        Assert.False(await context.Concerts.AsNoTracking()
            .Where(ConcertAuthorizationPolicy.Concerts(context, binding, actor, now))
            .AnyAsync(candidate => candidate.Id == concert.Id));

    }

    private async Task<MembershipSnapshot> ActorAsync(
        ConcertPrivilegedDbContext context, Guid tenantId, Guid userId,
        TenantPermission permission, ResourceAudience audience)
    {
        var membership = fixture.SeedState.Memberships.Single(candidate =>
            candidate.TenantId == tenantId && candidate.UserId == userId);
        var authority = await context.MembershipAuthority.AsNoTracking()
            .SingleAsync(candidate => candidate.MembershipId == membership.Id);
        return new MembershipSnapshot(
            authority.MembershipId, authority.TenantId, authority.UserId,
            authority.PermissionVersion, authority.RolePolicyVersion,
            ImmutableDictionary<TenantPermission, ResourceAudience>.Empty.Add(permission, audience));
    }

    private Guid TenantOf(Guid userId) =>
        fixture.SeedState.Tenants.Single(tenant => tenant.CreatedByUserId == userId).Id;

    private static ResourcePolicyBinding Binding(
        TenantPermission permission, ResourceKind kind, ResourceFacet facet)
    {
        var source = AuthorizationCatalog.Permissions[permission].ResourceBindings
            .Single(candidate => candidate.Resource.Equals(kind.ToString(), StringComparison.OrdinalIgnoreCase)
                && candidate.Facet == facet.ToString());
        return new ResourcePolicyBinding(
            permission, kind, facet, source.Policy, source.RequiresScopes.ToImmutableArray());
    }

    private static Task<bool> IsVisibleAsync(
        ConcertPrivilegedDbContext context, int id, ResourcePolicyBinding binding,
        MembershipSnapshot actor, DateTimeOffset now, ResourceAuthorizationSnapshot snapshot) =>
        context.Concerts.AsNoTracking()
            .Where(ConcertAuthorizationPolicy.Concerts(context, binding, actor, now, snapshot))
            .AnyAsync(concert => concert.Id == id);
}
