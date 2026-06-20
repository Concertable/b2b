using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.B2B.Tenant.Infrastructure.Events;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Concertable.B2B.Tenant.IntegrationTests;

/// <summary>
/// Registration provisions a tenant + its founding Owner membership and fixes the persona from the
/// registration client-id. Drives the production trigger directly — the <see cref="TenantProvisioningHandler"/>
/// reacting to a <see cref="CredentialRegisteredEvent"/> — since integration tests have no Auth service to
/// publish it. The seeded operators exercise the idempotent existing-tenant branch.
/// </summary>
[Collection("Integration")]
public sealed class TenantProvisioningTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public TenantProvisioningTests(ApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    private async Task ProvisionAsync(CredentialRegisteredEvent e)
    {
        using var scope = fixture.Services.CreateScope();
        var handler = scope.ServiceProvider
            .GetServices<IIntegrationEventHandler<CredentialRegisteredEvent>>()
            .OfType<TenantProvisioningHandler>()
            .Single();
        await handler.HandleAsync(e, MessageEnvelope.Create<CredentialRegisteredEvent>(DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(ClientIds.VenueWeb, TenantType.Venue)]
    [InlineData(ClientIds.ArtistWeb, TenantType.Artist)]
    public async Task Registration_NewManager_ProvisionsTenantWithPersonaAndFoundingOwner(string clientId, TenantType expected)
    {
        var userId = Guid.NewGuid();
        await ProvisionAsync(new CredentialRegisteredEvent(userId, $"{Guid.NewGuid():N}@test.com", clientId));

        using var scope = fixture.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITenantRepository>();

        var membership = await repository.GetMembershipByUserIdAsync(userId);
        Assert.NotNull(membership);
        Assert.Equal(TenantRole.Owner, membership!.Role);
        Assert.Null(membership.InvitedByUserId);

        var tenant = await repository.GetByIdAsync(membership.TenantId);
        Assert.NotNull(tenant);
        Assert.Equal(expected, tenant!.Type);
        Assert.Equal(userId, tenant.CreatedByUserId);
    }

    [Fact]
    public async Task Registration_NonManagerClient_ProvisionsNothing()
    {
        var userId = Guid.NewGuid();
        await ProvisionAsync(new CredentialRegisteredEvent(userId, "customer@test.com", ClientIds.CustomerWeb));

        using var scope = fixture.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        Assert.Null(await repository.GetMembershipByUserIdAsync(userId));
    }

    [Fact]
    public async Task Registration_SeededOperator_IsIdempotent_SingleOwnerMembership()
    {
        var manager = fixture.SeedState.VenueManager1;

        // The seeder already created this operator's tenant + Owner membership; re-running the handler (as the
        // bus would on the real CredentialRegisteredEvent) must not duplicate either — the unique (TenantId,
        // UserId) index would throw on a duplicate insert, so a clean run is itself the dedup assertion.
        await ProvisionAsync(new CredentialRegisteredEvent(manager.Id, manager.Email, ClientIds.VenueWeb));

        using var scope = fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var ownerCount = await context.Memberships.CountAsync(m => m.UserId == manager.Id && m.Role == TenantRole.Owner);
        var tenantCount = await context.Tenants.CountAsync(t => t.CreatedByUserId == manager.Id);

        Assert.Equal(1, ownerCount);
        Assert.Equal(1, tenantCount);
    }
}
