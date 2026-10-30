using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Contracts.Enums;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.DataAccess.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

[Collection("Integration")]
public sealed class ConcertAccessApiTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public ConcertAccessApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
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
    public async Task ShareSummary_AsArtistPrincipal_ReplaysTheRecordedGrant()
    {
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking);
        var client = CreateOwningArtistClient(concert.ArtistId);
        var recipientTenantId = TenantOf(fixture.SeedState.VenueManager2.Id);
        var request = new
        {
            requestId = Guid.NewGuid(),
            recipientTenantId,
            recipientMembershipId = (Guid?)null,
            expectedAccessVersion = concert.AccessVersion,
            validUntil = (DateTime?)null,
        };

        var first = await client.PostAsync($"/api/concert/{concert.Id}/summary-shares", request);
        var second = await client.PostAsync($"/api/concert/{concert.Id}/summary-shares", request);

        await first.ShouldBe(HttpStatusCode.OK);
        await second.ShouldBe(HttpStatusCode.OK);
        var firstShare = await first.Content.ReadAsync<ConcertSummaryShare>();
        var secondShare = await second.Content.ReadAsync<ConcertSummaryShare>();
        Assert.NotNull(firstShare);
        Assert.NotNull(secondShare);
        Assert.Equal(firstShare.GrantId, secondShare.GrantId);
        Assert.Equal(firstShare.AccessVersion, secondShare.AccessVersion);
    }

    [Fact]
    public async Task SharedSummary_ExposesOnlySummaryScope()
    {
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking);
        var owner = CreateOwningArtistClient(concert.ArtistId);
        var recipientTenantId = TenantOf(fixture.SeedState.VenueManager2.Id);
        var share = await owner.PostAsync(
            $"/api/concert/{concert.Id}/summary-shares",
            new
            {
                requestId = Guid.NewGuid(),
                recipientTenantId,
                recipientMembershipId = (Guid?)null,
                expectedAccessVersion = concert.AccessVersion,
                validUntil = (DateTime?)null,
            });
        await share.ShouldBe(HttpStatusCode.OK);
        var recipient = fixture.CreateClient(fixture.SeedState.VenueManager2);

        var summary = await recipient.GetAsync($"/api/concert/{concert.Id}/summary");

        await summary.ShouldBe(HttpStatusCode.OK);
        using var payload = JsonDocument.Parse(await summary.Content.ReadAsStringAsync());
        Assert.False(payload.RootElement.TryGetProperty("price", out _));
        Assert.False(payload.RootElement.TryGetProperty("ticketsSold", out _));
        Assert.False(payload.RootElement.TryGetProperty("doorRevenue", out _));
        Assert.False(payload.RootElement.TryGetProperty("actions", out _));
        await (await recipient.GetAsync($"/api/concert/{concert.Id}/operations"))
            .ShouldBe(HttpStatusCode.NotFound);
        await (await recipient.GetAsync($"/api/concert/{concert.Id}/finance"))
            .ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShareSummary_WhenCallerIsNotPrincipal_ReturnsForbidden()
    {
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking);
        var client = fixture.CreateClient(fixture.SeedState.VenueManager2);

        var response = await client.PostAsync(
            $"/api/concert/{concert.Id}/summary-shares",
            new
            {
                requestId = Guid.NewGuid(),
                recipientTenantId = Guid.NewGuid(),
                recipientMembershipId = (Guid?)null,
                expectedAccessVersion = concert.AccessVersion,
                validUntil = (DateTime?)null,
            });

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AccessMutations_WhenCallerIsNotPrincipal_ReturnForbidden()
    {
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking);
        var client = fixture.CreateClient(fixture.SeedState.VenueManager2);
        var membership = fixture.SeedState.Memberships.Single(value =>
            value.TenantId == TenantOf(fixture.SeedState.VenueManager2.Id)
            && value.UserId == fixture.SeedState.VenueManager2.Id);

        var revoke = await client.DeleteAsync(
            $"/api/concert/{concert.Id}/summary-shares/{Guid.NewGuid()}?expectedVersion={concert.AccessVersion}");
        var assign = await client.PostAsync(
            $"/api/concert/{concert.Id}/member-assignments",
            new
            {
                membershipId = membership.Id,
                expectedAccessVersion = concert.AccessVersion,
            });
        var remove = await client.DeleteAsync(
            $"/api/concert/{concert.Id}/member-assignments/{membership.Id}?expectedVersion={concert.AccessVersion}");

        await revoke.ShouldBe(HttpStatusCode.Forbidden);
        await assign.ShouldBe(HttpStatusCode.Forbidden);
        await remove.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShareSummary_WithInvalidValidity_DoesNotRevokeExpiredShare()
    {
        var issuer = fixture.SeedState.VenueManager1;
        var issuerTenantId = TenantOf(issuer.Id);
        var concert = fixture.SeedState.Concerts.First(value => value.VenueTenantId == issuerTenantId);
        var recipientTenantId = TenantOf(fixture.SeedState.VenueManager2.Id);
        var seeded = await fixture.AddExpiredSummaryShareAsync(
            concert.Id,
            issuerTenantId,
            issuer.Id,
            recipientTenantId);
        var client = CreateOwningVenueClient(concert.VenueId);

        var response = await client.PostAsync(
            $"/api/concert/{concert.Id}/summary-shares",
            new
            {
                requestId = Guid.NewGuid(),
                recipientTenantId,
                recipientMembershipId = (Guid?)null,
                expectedAccessVersion = seeded.AccessVersion,
                validUntil = seeded.Now,
            });

        await response.ShouldBe(HttpStatusCode.BadRequest);
        var persisted = await fixture.Concerts
            .AsNoTracking()
            .Include(value => value.AccessGrants)
            .SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(seeded.AccessVersion, persisted.AccessVersion);
        Assert.Contains(
            persisted.AccessGrants,
            grant => grant.Id == seeded.GrantId && grant.RevokedAt is null);
    }

    [Fact]
    public async Task ConcurrentSummaryShares_SerializeToOneGrant()
    {
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking);
        var client = CreateOwningVenueClient(concert.VenueId);
        var competitor = CreateOwningVenueClient(concert.VenueId);
        var request = new
        {
            requestId = Guid.NewGuid(),
            recipientTenantId = TenantOf(fixture.SeedState.VenueManager2.Id),
            recipientMembershipId = (Guid?)null,
            expectedAccessVersion = concert.AccessVersion,
            validUntil = (DateTime?)null,
        };
        var responses = await RaceAsync(
            () => client.PostAsync($"/api/concert/{concert.Id}/summary-shares", request),
            () => competitor.PostAsync($"/api/concert/{concert.Id}/summary-shares", request));

        foreach (var response in responses)
            await response.ShouldBe(HttpStatusCode.OK);
        var shares = await Task.WhenAll(
            responses.Select(response => response.Content.ReadAsync<ConcertSummaryShare>()));
        Assert.All(shares, Assert.NotNull);
        Assert.Single(shares.Select(share => share!.GrantId).Distinct());
        var persisted = await fixture.Concerts
            .Include(value => value.AccessGrants)
            .SingleAsync(value => value.Id == concert.Id);
        Assert.Single(
            persisted.AccessGrants,
            grant => grant.Kind == ResourceGrantKind.SharedSummary
                     && grant.IssuedByTenantId == concert.VenueTenantId
                     && grant.TenantId == request.recipientTenantId
                     && grant.RevokedAt is null);
    }

    [Fact]
    public async Task ConcurrentSummaryShares_WithOneRequestAcrossConcerts_RecoversReceiptConflict()
    {
        var concerts = fixture.SeedState.Concerts
            .GroupBy(value => value.VenueTenantId)
            .First(group => group.Count() >= 2)
            .Take(2)
            .ToArray();
        var client = CreateOwningVenueClient(concerts[0].VenueId);
        var competitor = CreateOwningVenueClient(concerts[1].VenueId);
        var requestId = Guid.NewGuid();
        var recipientTenantId = fixture.SeedState.Tenants
            .First(value => value.Id != concerts[0].VenueTenantId)
            .Id;

        Task<HttpResponseMessage> ShareAsync(
            HttpClient sender,
            ConcertEntity concert,
            CancellationToken cancellationToken) =>
            sender.PostAsJsonAsync(
                $"/api/concert/{concert.Id}/summary-shares",
                new
                {
                    requestId,
                    recipientTenantId,
                    recipientMembershipId = (Guid?)null,
                    expectedAccessVersion = concert.AccessVersion,
                    validUntil = (DateTime?)null,
                },
                cancellationToken);

        var responses = await fixture.RunWithReceiptInsertBarrierAsync(
            cancellationToken => RaceAsync(
                () => ShareAsync(client, concerts[0], cancellationToken),
                () => ShareAsync(competitor, concerts[1], cancellationToken)));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        var conflictIndex = Array.FindIndex(
            responses,
            response => response.StatusCode == HttpStatusCode.Conflict);
        Assert.NotEqual(-1, conflictIndex);
        var problem = await responses[conflictIndex].Content.ReadAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("concert.summary_share.request_conflict", problem.Extensions["code"]?.ToString());

        var subsequent = await (conflictIndex == 0 ? client : competitor).PostAsync(
            $"/api/concert/{concerts[conflictIndex].Id}/summary-shares",
            new
            {
                requestId = Guid.NewGuid(),
                recipientTenantId,
                recipientMembershipId = (Guid?)null,
                expectedAccessVersion = concerts[conflictIndex].AccessVersion,
                validUntil = (DateTime?)null,
            });
        await subsequent.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReceiptInsertBarrier_FailurePreservesExceptionAndRemovesDatabaseObjects()
    {
        var concerts = fixture.SeedState.Concerts
            .GroupBy(value => value.VenueTenantId)
            .First(group => group.Count() >= 2)
            .Take(2)
            .ToArray();
        var client = CreateOwningVenueClient(concerts[0].VenueId);
        var competitor = CreateOwningVenueClient(concerts[1].VenueId);
        var requestId = Guid.NewGuid();
        var recipientTenantId = fixture.SeedState.Tenants
            .First(value => value.Id != concerts[0].VenueTenantId)
            .Id;
        var injectedFailure = new InvalidOperationException("Injected receipt barrier failure.");
        var cancellationRegistration = default(CancellationTokenRegistration);

        Task<HttpResponseMessage> ShareAsync(
            HttpClient sender,
            ConcertEntity concert,
            CancellationToken cancellationToken) =>
            sender.PostAsJsonAsync(
                $"/api/concert/{concert.Id}/summary-shares",
                new
                {
                    requestId,
                    recipientTenantId,
                    recipientMembershipId = (Guid?)null,
                    expectedAccessVersion = concert.AccessVersion,
                    validUntil = (DateTime?)null,
                },
                cancellationToken);

        try
        {
            var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                fixture.RunWithReceiptInsertBarrierAsync(
                    cancellationToken =>
                    {
                        cancellationRegistration = cancellationToken.Register(
                            static () => throw new ApplicationException("Injected cancellation failure."));
                        return RaceAsync(
                            () => ShareAsync(client, concerts[0], cancellationToken),
                            () => ShareAsync(competitor, concerts[1], cancellationToken));
                    },
                    injectedFailure));

            Assert.Same(injectedFailure, thrown);
            Assert.Contains("RunWithReceiptInsertBarrierCoreAsync", thrown.StackTrace);
        }
        finally
        {
            await cancellationRegistration.DisposeAsync();
        }

        Assert.False(await fixture.HasReceiptInsertBarrierAsync());
    }

    [Fact]
    public async Task AssignAndRemoveMember_ChangesOnlyThePrincipalMembershipGrant()
    {
        var venueTenantId = TenantOf(fixture.SeedState.VenueManager1.Id);
        var concert = fixture.SeedState.Concerts.First(value => value.VenueTenantId == venueTenantId);
        var client = CreateOwningVenueClient(concert.VenueId);
        var membership = fixture.SeedState.Memberships.Single(value =>
            value.TenantId == concert.VenueTenantId
            && value.UserId == fixture.SeedState.VenueManager3.Id);

        var assign = await client.PostAsync(
            $"/api/concert/{concert.Id}/member-assignments",
            new
            {
                membershipId = membership.Id,
                expectedAccessVersion = concert.AccessVersion,
            });

        await assign.ShouldBe(HttpStatusCode.NoContent);
        var assigned = await fixture.Concerts
            .AsNoTracking()
            .Include(value => value.AccessGrants)
            .SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(concert.AccessVersion + 1, assigned.AccessVersion);
        Assert.Equal(
            [ConcertAccessScope.Summary, ConcertAccessScope.Operations],
            assigned.AccessGrants
                .Where(grant => grant.Kind == ResourceGrantKind.MemberAssignment
                                && grant.MembershipId == membership.Id
                                && grant.RevokedAt is null)
                .Select(grant => grant.Scope)
                .Order()
                .ToArray());

        var staleRemove = await client.DeleteAsync(
            $"/api/concert/{concert.Id}/member-assignments/{membership.Id}?expectedVersion={concert.AccessVersion}");

        await staleRemove.ShouldBe(HttpStatusCode.Conflict);
        var unchanged = await fixture.Concerts
            .AsNoTracking()
            .Include(value => value.AccessGrants)
            .SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(assigned.AccessVersion, unchanged.AccessVersion);
        Assert.Contains(
            unchanged.AccessGrants,
            grant => grant.Kind == ResourceGrantKind.MemberAssignment
                     && grant.MembershipId == membership.Id
                     && grant.RevokedAt is null);

        var remove = await client.DeleteAsync(
            $"/api/concert/{concert.Id}/member-assignments/{membership.Id}?expectedVersion={assigned.AccessVersion}");

        await remove.ShouldBe(HttpStatusCode.NoContent);
        var removed = await fixture.Concerts
            .AsNoTracking()
            .Include(value => value.AccessGrants)
            .SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(assigned.AccessVersion + 1, removed.AccessVersion);
        Assert.DoesNotContain(
            removed.AccessGrants,
            grant => grant.Kind == ResourceGrantKind.MemberAssignment
                     && grant.MembershipId == membership.Id
                     && grant.RevokedAt is null);

        var repeatedRemove = await client.DeleteAsync(
            $"/api/concert/{concert.Id}/member-assignments/{membership.Id}?expectedVersion={removed.AccessVersion}");

        await repeatedRemove.ShouldBe(HttpStatusCode.NotFound);
        var afterRepeatedRemove = await fixture.Concerts
            .AsNoTracking()
            .SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(removed.AccessVersion, afterRepeatedRemove.AccessVersion);
    }

    [Fact]
    public async Task OpsEdit_WhenTheMemberAssignmentIsRevokedBeforeItLocksTheConcert_IsDenied()
    {
        var race = await ArrangeAssignedStaffAsync();
        fixture.CommandPause.Arm(command => IsConcertRowLock(command, race.Concert.Id));
        var edit = race.Staff.PutAsync(
            $"/api/concert/{race.Concert.Id}",
            ConcertRequestBuilders.BuildPostRequest(name: "Edited by staff"));
        await fixture.CommandPause.WaitUntilPausedAsync();

        var revocation = await RevokeAssignmentAsync(race);
        fixture.CommandPause.Release();

        await revocation.ShouldBe(HttpStatusCode.NoContent);
        await (await edit).ShouldBe(HttpStatusCode.Forbidden);
        var persisted = await LoadWithGrantsAsync(race.Concert.Id);
        Assert.NotEqual("Edited by staff", persisted.Name);
        Assert.False(HasLiveAssignment(persisted, race.MembershipId));
    }

    [Fact]
    public async Task Revocation_WhileAnOpsEditHoldsTheConcertLock_WaitsForTheEditToCommit()
    {
        var race = await ArrangeAssignedStaffAsync();
        fixture.CommandPause.Arm(command => IsConcertUpdate(command, race.Concert.Id));
        var edit = race.Staff.PutAsync(
            $"/api/concert/{race.Concert.Id}",
            ConcertRequestBuilders.BuildPostRequest(name: "Edited by staff"));
        await fixture.CommandPause.WaitUntilPausedAsync();

        var revocation = RevokeAssignmentAsync(race);
        await PostgresLockWaits.WaitForWaiterAsync(fixture.ConnectionString, """%concert."Concerts"%FOR UPDATE%""");
        var revokedWhileLocked = revocation.IsCompleted;
        fixture.CommandPause.Release();

        await (await edit).ShouldBe(HttpStatusCode.OK);
        await (await revocation).ShouldBe(HttpStatusCode.NoContent);
        Assert.False(revokedWhileLocked);
        var persisted = await LoadWithGrantsAsync(race.Concert.Id);
        Assert.Equal("Edited by staff", persisted.Name);
        Assert.False(HasLiveAssignment(persisted, race.MembershipId));
    }

    private async Task<AssignmentRace> ArrangeAssignedStaffAsync()
    {
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking);
        var owner = CreateOwningVenueClient(concert.VenueId);
        var staffUser = fixture.SeedState.VenueManager3;
        var membershipId = fixture.SeedState.Memberships.Single(value =>
            value.TenantId == concert.VenueTenantId && value.UserId == staffUser.Id).Id;

        var demotion = await owner.PutAsJsonAsync(
            $"/api/organization/members/{staffUser.Id}/roles",
            new { roleIds = new[] { SystemPresetIds.For(concert.VenueTenantId, "Staff") } });
        await demotion.ShouldBe(HttpStatusCode.NoContent);
        var assignment = await owner.PostAsync(
            $"/api/concert/{concert.Id}/member-assignments",
            new { membershipId, expectedAccessVersion = concert.AccessVersion });
        await assignment.ShouldBe(HttpStatusCode.NoContent);

        var staff = fixture.CreateClient(staffUser);
        staff.DefaultRequestHeaders.Add(TenantHeaders.TenantId, concert.VenueTenantId.ToString());
        return new AssignmentRace(concert, owner, staff, membershipId, concert.AccessVersion + 1);
    }

    private static Task<HttpResponseMessage> RevokeAssignmentAsync(AssignmentRace race) =>
        race.Owner.DeleteAsync(
            $"/api/concert/{race.Concert.Id}/member-assignments/{race.MembershipId}?expectedVersion={race.AssignedAccessVersion}");

    private Task<ConcertEntity> LoadWithGrantsAsync(int concertId) =>
        fixture.Concerts.AsNoTracking()
            .Include(value => value.AccessGrants)
            .SingleAsync(value => value.Id == concertId);

    private static bool HasLiveAssignment(ConcertEntity concert, Guid membershipId) =>
        concert.AccessGrants.Any(grant => grant.Kind == ResourceGrantKind.MemberAssignment
                                          && grant.MembershipId == membershipId
                                          && grant.RevokedAt is null);

    private static bool IsConcertRowLock(DbCommand command, int concertId) =>
        command.CommandText.Contains("FROM concert.\"Concerts\"", StringComparison.Ordinal)
        && command.CommandText.Contains("FOR UPDATE", StringComparison.OrdinalIgnoreCase)
        && command.HasParameter(concertId);

    private static bool IsConcertUpdate(DbCommand command, int concertId) =>
        command.CommandText.Contains("UPDATE concert.\"Concerts\"", StringComparison.Ordinal)
        && command.HasParameter(concertId);

    private sealed record AssignmentRace(
        ConcertEntity Concert,
        HttpClient Owner,
        HttpClient Staff,
        Guid MembershipId,
        long AssignedAccessVersion);

    private Guid TenantOf(Guid userId) =>
        fixture.SeedState.Tenants.Single(tenant => tenant.CreatedByUserId == userId).Id;

    private HttpClient CreateOwningVenueClient(int venueId) =>
        fixture.CreateClient(fixture.SeedState.VenueManagers.Single(manager =>
            manager.Id == fixture.SeedState.Venues.Single(venue => venue.Id == venueId).UserId));

    private HttpClient CreateOwningArtistClient(int artistId) =>
        fixture.CreateClient(fixture.SeedState.ArtistManagers.Single(manager =>
            manager.Id == fixture.SeedState.Artists.Single(artist => artist.Id == artistId).UserId));

    private static async Task<HttpResponseMessage[]> RaceAsync(
        Func<Task<HttpResponseMessage>> first,
        Func<Task<HttpResponseMessage>> second)
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<HttpResponseMessage> RunAsync(Func<Task<HttpResponseMessage>> request)
        {
            await start.Task;
            return await request();
        }

        var firstTask = RunAsync(first);
        var secondTask = RunAsync(second);
        start.SetResult();
        return await Task.WhenAll(firstTask, secondTask);
    }
}
