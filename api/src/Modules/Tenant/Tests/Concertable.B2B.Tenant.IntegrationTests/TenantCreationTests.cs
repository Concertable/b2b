using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Concertable.B2B.Tenant.IntegrationTests;

[Collection("Integration")]
public sealed class TenantCreationTests : IAsyncLifetime
{
    private readonly TenantApiFixture fixture;

    public TenantCreationTests(TenantApiFixture fixture, ITestOutputHelper output)
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
    public async Task Create_ConcurrentFirstRequests_CreateOnceAndReturnTypedConflict()
    {
        var userId = Guid.NewGuid();
        var email = $"{userId:N}@tenant.test";
        using var firstClient = fixture.CreateClient(userId, email);
        using var secondClient = fixture.CreateClient(userId, email);
        var request = new
        {
            displayName = "Concurrent tenant",
            contactEmail = email,
            activities = new[] { TenantBusinessActivityKind.VenueOperator.ToString() }
        };

        var responses = await fixture.RunWithTenantCreationBarrierAsync(
            userId,
            () => Task.WhenAll(
                firstClient.PostAsJsonAsync("/api/organization", request),
                secondClient.PostAsJsonAsync("/api/organization", request)));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        var conflict = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        var problem = await conflict.Content.ReadAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("You have already created an organization.", problem.Detail);
        Assert.Equal(1, await fixture.Tenants.CountAsync(tenant => tenant.CreatedByUserId == userId));
        Assert.Equal(1, await fixture.Memberships.CountAsync(membership => membership.UserId == userId));
    }
}
