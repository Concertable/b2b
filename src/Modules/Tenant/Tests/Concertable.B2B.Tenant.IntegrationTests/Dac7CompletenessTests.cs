using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Tenant.IntegrationTests;

/// <summary>
/// <see cref="ITenantModule.IsDac7CompleteAsync"/> — the single completeness rule the fail-closed payout gate and
/// the dashboard nag both consume, exposed across the module boundary. Fail-closed: bare or unknown = not complete.
/// </summary>
[Collection("Integration")]
public sealed class Dac7CompletenessTests : IAsyncLifetime
{
    private readonly TenantApiFixture fixture;

    public Dac7CompletenessTests(TenantApiFixture fixture, ITestOutputHelper output)
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
        return await module.IsDac7CompleteAsync(tenantId);
    }

    private Guid TenantOf(Guid userId) =>
        fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == userId).Id;

    [Fact]
    public async Task IsDac7Complete_OnboardedOperator_True() =>
        Assert.True(await IsCompleteAsync(TenantOf(fixture.SeedState.VenueManager1.Id)));

    [Fact]
    public async Task IsDac7Complete_OperatorWithoutComplianceCaptured_False() =>
        Assert.False(await IsCompleteAsync(TenantOf(fixture.SeedState.VenueManagerNoVenue.Id)));

    [Fact]
    public async Task IsDac7Complete_UnknownTenant_False() =>
        Assert.False(await IsCompleteAsync(Guid.NewGuid()));
}
