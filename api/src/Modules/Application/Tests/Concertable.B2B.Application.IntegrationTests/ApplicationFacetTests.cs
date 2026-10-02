using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Concertable.B2B.Tenant.Contracts;
using Xunit.Abstractions;

namespace Concertable.B2B.Application.IntegrationTests;

[Collection("Integration")]
public sealed class ApplicationFacetTests : IAsyncLifetime
{
    private readonly ApplicationApiFixture fixture;

    public ApplicationFacetTests(ApplicationApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task TermsReaderCanSeeProposalAndContractWithoutOperations()
    {
        var tenantId = fixture.SeedState.PostedFlatFeeApp.VenueTenantId;
        var owner = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var roleId = await CreateRoleAsync(owner, "Terms reader", "terms.read", "TenantResources");
        await (await owner.PutAsJsonAsync(
            $"/api/organization/members/{fixture.SeedState.VenueManager3.Id}/roles",
            new { roleIds = new[] { roleId } })).ShouldBe(HttpStatusCode.NoContent);

        var reader = fixture.CreateClient(fixture.SeedState.VenueManager3);
        reader.DefaultRequestHeaders.Add(TenantHeaders.TenantId, tenantId.ToString());
        var proposal = await reader.GetAsync($"/api/application/{fixture.SeedState.PostedFlatFeeApp.Id}/proposal");
        await proposal.ShouldBe(HttpStatusCode.OK);
        var body = await proposal.Content.ReadFromJsonAsync<JsonElement>();
        var actions = body.GetProperty("actions");
        Assert.True(actions.TryGetProperty("contract", out _));
        Assert.False(actions.TryGetProperty("accept", out _));
        await (await reader.GetAsync($"/api/application/{fixture.SeedState.PostedFlatFeeApp.Id}/summary"))
            .ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DecisionRoleCanStartCheckoutWithoutTermsRead()
    {
        var application = fixture.SeedState.FlatFeeApp;
        var owner = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var roleId = await CreateRoleAsync(owner, "Decision maker", "applications.decide", "TenantResources");
        await (await owner.PutAsJsonAsync(
            $"/api/organization/members/{fixture.SeedState.VenueManager3.Id}/roles",
            new { roleIds = new[] { roleId } })).ShouldBe(HttpStatusCode.NoContent);

        var decider = fixture.CreateClient(fixture.SeedState.VenueManager3);
        decider.DefaultRequestHeaders.Add(TenantHeaders.TenantId, application.VenueTenantId.ToString());
        await (await decider.GetAsync($"/api/application/{application.Id}/proposal"))
            .ShouldBe(HttpStatusCode.Forbidden);
        await (await decider.PostAsync($"/api/application/{application.Id}/checkout"))
            .ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AssignedReaderCannotUseTenantWideGrant()
    {
        var application = fixture.SeedState.FlatFeeApp;
        var owner = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var roleId = await CreateRoleAsync(owner, "Assigned reader", "operations.view", "AssignedResources");
        await (await owner.PutAsJsonAsync(
            $"/api/organization/members/{fixture.SeedState.VenueManager3.Id}/roles",
            new { roleIds = new[] { roleId } })).ShouldBe(HttpStatusCode.NoContent);

        var reader = fixture.CreateClient(fixture.SeedState.VenueManager3);
        reader.DefaultRequestHeaders.Add(TenantHeaders.TenantId, application.VenueTenantId.ToString());
        await (await reader.GetAsync($"/api/application/{application.Id}/summary"))
            .ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ApplyWaitingBehindAcceptanceCannotCreateASecondLiveApplication()
    {
        var application = fixture.SeedState.FlatFeeApp;
        using var scope = fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationReadDbContext>();
        await using var control = new NpgsqlConnection(context.Database.GetConnectionString());
        await control.OpenAsync();
        await using var transaction = await control.BeginTransactionAsync();
        await using (var takeLock = control.CreateCommand())
        {
            takeLock.Transaction = transaction;
            takeLock.CommandText = "SELECT pg_advisory_xact_lock(@namespace, @opportunityId)";
            takeLock.Parameters.AddWithValue("namespace", 10241001);
            takeLock.Parameters.AddWithValue("opportunityId", application.OpportunityId);
            await takeLock.ExecuteNonQueryAsync();
        }

        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var secondArtist = fixture.CreateClient(fixture.SeedState.ArtistManagers[1]);
        var acceptance = venue.PostAsync($"/api/application/{application.Id}/accept",
            new { eSignature = new { signatoryName = "Venue Signatory" } });
        var released = false;
        try
        {
            await WaitForWaitersAsync(control, transaction, application.OpportunityId, 1);
            var apply = secondArtist.PostAsync($"/api/application/{application.OpportunityId}",
                new { eSignature = new { signatoryName = "Second Artist" } });
            await WaitForWaitersAsync(control, transaction, application.OpportunityId, 2);
            await transaction.CommitAsync();
            released = true;

            await (await acceptance).ShouldBe(HttpStatusCode.NoContent);
            var applyResponse = await apply;
            Assert.NotEqual(HttpStatusCode.Created, applyResponse.StatusCode);
            Assert.False(await fixture.Applications.AnyAsync(candidate =>
                candidate.OpportunityId == application.OpportunityId
                && candidate.ArtistTenantId != application.ArtistTenantId));
        }
        finally
        {
            if (!released)
                await transaction.RollbackAsync();
        }
    }

    private static async Task WaitForWaitersAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, int opportunityId, long expected)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            await using var count = connection.CreateCommand();
            count.Transaction = transaction;
            count.CommandText = """
                SELECT COUNT(*) FROM pg_locks
                WHERE locktype = 'advisory' AND NOT granted
                  AND classid::text = @namespace
                  AND objid::text = @opportunityId
                """;
            count.Parameters.AddWithValue("namespace", "10241001");
            count.Parameters.AddWithValue("opportunityId", opportunityId.ToString());
            if ((long)(await count.ExecuteScalarAsync(timeout.Token))! >= expected)
                return;
            await Task.Delay(25, timeout.Token);
        }
    }

    private static async Task<Guid> CreateRoleAsync(
        HttpClient owner, string name, string permission, string audience)
    {
        var created = await owner.PostAsJsonAsync("/api/organization/roles", new
        {
            name,
            isInvitationAssignable = true,
            permissions = new[] { new { permission, audience } }
        });
        await created.ShouldBe(HttpStatusCode.Created);
        var role = await created.Content.ReadFromJsonAsync<JsonElement>();
        return role.GetProperty("id").GetGuid();
    }
}
