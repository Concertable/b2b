using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Payment.Client;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Tenant.IntegrationTests;

/// <summary>
/// Phase 5 payout proxy. B2B fronts the four Stripe-account operations, gated on <c>PayoutsManage</c> and
/// resolving the owner as the ACTIVE TENANT (not the user) before calling Payment over gRPC. Drives the real
/// ASP.NET pipeline against a mocked payout client that records the owner id it received.
/// </summary>
[Collection("Integration")]
public sealed class StripeAccountProxyTests : IAsyncLifetime
{
    private readonly TenantApiFixture fixture;

    public StripeAccountProxyTests(TenantApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    private Guid TenantOf(Guid userId) => fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == userId).Id;

    [Fact]
    public async Task AccountStatus_ForOwner_PassesTheActiveTenantAsOwner()
    {
        var manager = fixture.SeedState.VenueManager1; // founding Owner, sole membership → default tenant

        var response = await fixture.CreateClient(manager).GetAsync("/api/stripeaccount/account-status");

        await response.ShouldBe(HttpStatusCode.OK);
        Assert.Equal(PayoutAccountStatus.Verified, await response.Content.ReadAsync<PayoutAccountStatus>());
        // The proxy keys Payment on the tenant, never the user — that's the whole point of the indirection.
        Assert.Equal(TenantOf(manager.Id), fixture.PayoutAccountClient.LastOwnerId);
        Assert.NotEqual(manager.Id, fixture.PayoutAccountClient.LastOwnerId);
    }

    [Fact]
    public async Task OnboardingLink_ForOwner_Returns200()
    {
        var response = await fixture.CreateClient(fixture.SeedState.VenueManager1)
            .GetAsync("/api/stripeaccount/onboarding-link");

        await response.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PaymentMethod_ForOwner_ReturnsTheSavedCard()
    {
        var response = await fixture.CreateClient(fixture.SeedState.VenueManager1)
            .GetAsync("/api/stripeaccount/payment-method");

        await response.ShouldBe(HttpStatusCode.OK);
        var card = await response.Content.ReadAsync<SavedCard>();
        Assert.Equal("4242", card!.Last4);
    }

    [Fact]
    public async Task SetupIntent_ForOwner_Returns200()
    {
        var response = await fixture.CreateClient(fixture.SeedState.VenueManager1)
            .PostAsync("/api/stripeaccount/setup-intent", null);

        await response.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NonPayoutRole_IsForbidden()
    {
        // Manager (no PayoutsManage in the matrix) acting in another tenant via the X-Tenant-Id header.
        var manager = fixture.SeedState.VenueManager1;
        var otherTenant = TenantOf(fixture.SeedState.VenueManager2.Id);
        await fixture.AddMembershipAsync(otherTenant, manager.Id, TenantRole.Manager);

        var client = fixture.CreateClient(manager);
        client.DefaultRequestHeaders.Add(TenantHeaders.TenantId, otherTenant.ToString());

        var response = await client.GetAsync("/api/stripeaccount/account-status");

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FinanceRole_CanManagePayouts()
    {
        // Finance holds PayoutsManage (money-only role), so the proxy serves it — keyed on that tenant.
        var manager = fixture.SeedState.VenueManager1;
        var otherTenant = TenantOf(fixture.SeedState.VenueManager2.Id);
        await fixture.AddMembershipAsync(otherTenant, manager.Id, TenantRole.Finance);

        var client = fixture.CreateClient(manager);
        client.DefaultRequestHeaders.Add(TenantHeaders.TenantId, otherTenant.ToString());

        var response = await client.GetAsync("/api/stripeaccount/account-status");

        await response.ShouldBe(HttpStatusCode.OK);
        Assert.Equal(otherTenant, fixture.PayoutAccountClient.LastOwnerId);
    }
}
