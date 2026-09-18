using System.Net;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Contracts.Enums;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.DataAccess.Application;
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
    public async Task ShareSummary_WhenCallerIsNotPrincipal_ReturnsForbidden()
    {
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking);
        var client = fixture.CreateClient(fixture.SeedState.VenueManager2);

        var response = await client.PostAsync(
            $"/api/concert/{concert.Id}/summary-shares",
            new
            {
                requestId = Guid.NewGuid(),
                recipientTenantId = TenantOf(fixture.SeedState.VenueManager3.Id),
                recipientMembershipId = (Guid?)null,
                expectedAccessVersion = concert.AccessVersion,
                validUntil = (DateTime?)null,
            });

        await response.ShouldBe(HttpStatusCode.Forbidden);
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
            .Include(value => value.AccessGrants)
            .SingleAsync(value => value.Id == concert.Id);
        Assert.Equal(
            [ConcertAccessScope.Summary, ConcertAccessScope.Operations],
            assigned.AccessGrants
                .Where(grant => grant.Kind == ResourceGrantKind.MemberAssignment
                                && grant.MembershipId == membership.Id
                                && grant.RevokedAt is null)
                .Select(grant => grant.Scope)
                .Order()
                .ToArray());

        var remove = await client.DeleteAsync(
            $"/api/concert/{concert.Id}/member-assignments/{membership.Id}");

        await remove.ShouldBe(HttpStatusCode.NoContent);
        var removed = await fixture.Concerts
            .Include(value => value.AccessGrants)
            .SingleAsync(value => value.Id == concert.Id);
        Assert.DoesNotContain(
            removed.AccessGrants,
            grant => grant.Kind == ResourceGrantKind.MemberAssignment
                     && grant.MembershipId == membership.Id
                     && grant.RevokedAt is null);
    }

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
