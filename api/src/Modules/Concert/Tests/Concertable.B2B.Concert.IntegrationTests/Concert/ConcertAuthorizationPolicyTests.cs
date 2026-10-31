using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Contracts.Enums;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Repositories;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.DataAccess.Infrastructure;
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
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking);
        var tenantId = fixture.SeedState.VenueManager2.TenantId;
        var actor = await ActorAsync(
            context, tenantId, fixture.SeedState.VenueManager2.Id,
            TenantPermission.OperationsView, ResourceAccess.Assigned);
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow();
        var grant = ConcertAccessGrant.Issue(
            concert.Id, tenantId, actor.MembershipId, ConcertAccessScope.Operations,
            concert.VenueTenantId, null, ResourceGrantKind.MemberAssignment, now.UtcDateTime);
        context.ConcertAccessGrants.Add(grant);
        await context.SaveChangesAsync();

        Assert.True(await reads.IsAccessibleAsync(
            concert.Id, TenantPermission.OperationsView, ConcertAccessScope.Operations, actor, now));
        Assert.NotNull(await reads.GetOperationsByIdAsync(concert.Id, TenantPermission.OperationsView, actor, now));
        Assert.Equal(1, await context.Concerts.AsNoTracking()
            .AccessibleBy(actor, TenantPermission.OperationsView, ConcertAccessScope.Operations, now.UtcDateTime)
            .CountAsync(candidate => candidate.Id == concert.Id));
        Assert.Null(await reads.GetSummaryByIdAsync(concert.Id, TenantPermission.OperationsView, actor, now));
        Assert.Null(await reads.GetFinanceByIdAsync(
            concert.Id, TenantPermission.SettlementView, TenantPermission.SettlementView, actor, now));
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
        var tenantId = fixture.SeedState.VenueManager2.TenantId;
        var actor = await ActorAsync(
            context, tenantId, fixture.SeedState.VenueManager2.Id,
            TenantPermission.SettlementView, ResourceAccess.All);
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow();
        var finance = TenantPermission.SettlementView;
        var read = TenantPermission.SettlementView;
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
            concert.Id, TenantPermission.OperationsView, actor, now));

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
    public async Task MembershipAndGrantChangesDenyTheSameResource()
    {
        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ConcertPrivilegedDbContext>();
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking);
        var tenantId = fixture.SeedState.VenueManager2.TenantId;
        var actor = await ActorAsync(
            context, tenantId, fixture.SeedState.VenueManager2.Id,
            TenantPermission.OperationsView, ResourceAccess.Assigned);
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow();
        var grant = ConcertAccessGrant.Issue(
            concert.Id, tenantId, actor.MembershipId, ConcertAccessScope.Operations,
            concert.VenueTenantId, null, ResourceGrantKind.MemberAssignment,
            now.UtcDateTime.AddMinutes(-2));
        context.ConcertAccessGrants.Add(grant);
        await context.SaveChangesAsync();

        Assert.True(await IsVisibleAsync(context, concert.Id, actor, now));
        Assert.False(await IsVisibleAsync(
            context, concert.Id, actor with { PermissionVersion = actor.PermissionVersion + 1 }, now));
        Assert.False(await IsVisibleAsync(context, concert.Id, actor with { TenantId = Guid.NewGuid() }, now));
        Assert.False(await IsVisibleAsync(
            context, concert.Id,
            actor with { Permissions = EffectivePermissions.From(actor.Permissions
                .Where(grant => grant.Key != TenantPermission.OperationsView)
                .Append(KeyValuePair.Create(TenantPermission.OperationsView, ResourceAccess.None))) }, now));

        grant.Revoke(now.UtcDateTime);
        await context.SaveChangesAsync();
        Assert.False(await IsVisibleAsync(context, concert.Id, actor, now));

        context.ConcertAccessGrants.Add(ConcertAccessGrant.Issue(
            concert.Id, tenantId, actor.MembershipId, ConcertAccessScope.Operations,
            concert.VenueTenantId, null, ResourceGrantKind.MemberAssignment,
            now.UtcDateTime.AddHours(-2), now.UtcDateTime.AddHours(-1)));
        await context.SaveChangesAsync();
        Assert.False(await IsVisibleAsync(context, concert.Id, actor, now));

    }

    private async Task<MembershipSnapshot> ActorAsync(
        ConcertPrivilegedDbContext context, Guid tenantId, Guid userId,
        TenantPermission permission, ResourceAccess access)
    {
        var membership = fixture.SeedState.Memberships.Single(candidate =>
            candidate.TenantId == tenantId && candidate.UserId == userId);
        var authority = await context.MembershipAuthority.AsNoTracking()
            .SingleAsync(candidate => candidate.MembershipId == membership.Id);
        return new MembershipSnapshot(
            authority.MembershipId, authority.TenantId, authority.UserId,
            authority.PermissionVersion, authority.RolePolicyVersion,
            EffectivePermissions.From([KeyValuePair.Create(permission, access)]));
    }

    private static Task<bool> IsVisibleAsync(
        ConcertPrivilegedDbContext context, int id, MembershipSnapshot actor, DateTimeOffset now) =>
        context.Concerts.AsNoTracking()
            .AccessibleBy(actor, TenantPermission.OperationsView, ConcertAccessScope.Operations, now.UtcDateTime)
            .WhereCurrentMember(context.MembershipAuthority, actor)
            .AnyAsync(concert => concert.Id == id);
}
