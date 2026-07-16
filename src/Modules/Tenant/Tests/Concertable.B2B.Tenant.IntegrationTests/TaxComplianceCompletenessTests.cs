using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Tenant.IntegrationTests;

/// <summary>
/// <see cref="ITenantModule.IsTaxComplianceCompleteAsync"/> — the single completeness rule the fail-closed payout gate and
/// the dashboard nag both consume, exposed across the module boundary. Fail-closed: bare or unknown = not complete.
/// </summary>
[Collection("Integration")]
public sealed class TaxComplianceCompletenessTests : IAsyncLifetime
{
    private readonly TenantApiFixture fixture;

    public TaxComplianceCompletenessTests(TenantApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    private async Task<bool> IsCompleteAsync(Guid tenantId)
    {
        using var scope = fixture.Services.CreateScope();
        var module = scope.ServiceProvider.GetRequiredService<ITenantModule>();
        return await module.IsTaxComplianceCompleteAsync(tenantId);
    }

    private Guid TenantOf(Guid userId) =>
        fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == userId).Id;

    [Fact]
    public async Task IsTaxComplianceComplete_OnboardedOperator_True() =>
        Assert.True(await IsCompleteAsync(TenantOf(fixture.SeedState.VenueManager1.Id)));

    [Fact]
    public async Task IsTaxComplianceComplete_OperatorWithoutComplianceCaptured_False() =>
        Assert.False(await IsCompleteAsync(TenantOf(fixture.SeedState.VenueManagerNoVenue.Id)));

    [Fact]
    public async Task IsTaxComplianceComplete_UnknownTenant_False() =>
        Assert.False(await IsCompleteAsync(Guid.NewGuid()));
}
