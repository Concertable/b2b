using System.Net;
using System.Net.Http.Json;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Contracts.Enums;
using Concertable.B2B.Tenant.Domain.Enums;
using Xunit.Abstractions;

namespace Concertable.B2B.Tenant.IntegrationTests;

/// <summary>
/// Admin review of tenant verification submissions on <c>api/verification</c> — the pending queue
/// enriched with the owning venue/artist's contact, and the approve/reject actions (state transition,
/// notification, and the illegal-transition/not-found error paths). Mirrors <c>VenueApiTests</c>'
/// approve/pending-approval coverage.
/// </summary>
[Collection("Integration")]
public sealed class VerificationAdminApiTests : IAsyncLifetime
{
    private readonly TenantApiFixture fixture;

    public VerificationAdminApiTests(TenantApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    private Guid TenantOf(Guid userId) => fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == userId).Id;

    private static async Task<HttpResponseMessage> CreateArtistAsync(HttpClient client)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent("New Artist"), "Name" },
            { new StringContent("About the artist"), "About" },
            { new StringContent("51.5"), "Latitude" },
            { new StringContent("-0.1"), "Longitude" },
        };
        await content.AddFileAsync(ImageFileBuilder.Jpeg("Banner", "banner.jpg"), "Banner");
        await content.AddFileAsync(ImageFileBuilder.Jpeg("Avatar", "avatar.jpg"), "Avatar");

        return await client.PostAsync("/api/organization/artist", content);
    }

    private sealed record PendingVerificationPage(List<PendingVerificationDto> Data);

    #region GetPending

    [Fact]
    public async Task GetPending_ShouldReturn401_WhenUnauthenticated()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/verification/pending");

        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPending_ShouldReturn403_WhenNotAdmin()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync("/api/verification/pending");

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPending_ShouldReturn200_WithVenueContactEnrichment()
    {
        var owner = fixture.SeedState.UnverifiedVenueManager;
        var tenantId = TenantOf(owner.Id);
        var venue = fixture.SeedState.Venues.Single(v => v.TenantId == tenantId);
        await fixture.AddPendingVerificationAsync(
            tenantId, VerificationDocumentType.Licence, fixture.SeedNow.AddDays(-1));
        var admin = fixture.CreateClient(fixture.SeedState.Admin);

        var response = await admin.GetAsync("/api/verification/pending");

        await response.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadAsync<PendingVerificationPage>();
        var row = page!.Data.Single(r => r.TenantId == tenantId);
        Assert.Equal(TenantType.Venue, row.TenantType);
        Assert.Equal(new TenantContact(venue.Name, venue.Email), row.Contact);
    }

    [Fact]
    public async Task GetPending_ShouldReturn200_WithArtistContactEnrichment()
    {
        var owner = fixture.SeedState.ArtistManagerNoArtist;
        var tenantId = TenantOf(owner.Id);
        var ownerClient = fixture.CreateClient(owner);
        await (await CreateArtistAsync(ownerClient)).ShouldBe(HttpStatusCode.Created);
        await fixture.AddPendingVerificationAsync(
            tenantId, VerificationDocumentType.CompanyRegistration, fixture.SeedNow.AddDays(-1));
        var admin = fixture.CreateClient(fixture.SeedState.Admin);

        var response = await admin.GetAsync("/api/verification/pending");

        await response.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadAsync<PendingVerificationPage>();
        var row = page!.Data.Single(r => r.TenantId == tenantId);
        Assert.Equal(TenantType.Artist, row.TenantType);
        Assert.Equal(new TenantContact("New Artist", owner.Email), row.Contact);
    }

    [Fact]
    public async Task GetPending_ShouldReturn200_ExcludingApprovedAndRejected()
    {
        var admin = fixture.CreateClient(fixture.SeedState.Admin);

        var response = await admin.GetAsync("/api/verification/pending");

        await response.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadAsync<PendingVerificationPage>();
        Assert.DoesNotContain(page!.Data, r => r.TenantId == TenantOf(fixture.SeedState.VenueManager1.Id));
    }

    /// <summary>Pins that contact enrichment awaits sequentially: two pending rows sharing a
    /// <see cref="TenantType"/> would run concurrent queries against the same scoped Venue/ArtistReadDbContext
    /// instance if enrichment ran in parallel, which EF Core rejects.</summary>
    [Fact]
    public async Task GetPending_ShouldReturn200_WhenTwoPendingRowsShareTenantType()
    {
        var firstTenantId = TenantOf(fixture.SeedState.UnverifiedVenueManager.Id);
        var secondTenantId = TenantOf(fixture.SeedState.VenueManagerNoVenue.Id);
        var submittedAt = fixture.SeedNow.AddDays(-1);
        await fixture.AddPendingVerificationAsync(firstTenantId, VerificationDocumentType.Licence, submittedAt);
        await fixture.AddPendingVerificationAsync(secondTenantId, VerificationDocumentType.Licence, submittedAt);
        var admin = fixture.CreateClient(fixture.SeedState.Admin);

        var response = await admin.GetAsync("/api/verification/pending");

        await response.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadAsync<PendingVerificationPage>();
        Assert.Contains(page!.Data, r => r.TenantId == firstTenantId);
        Assert.Contains(page.Data, r => r.TenantId == secondTenantId);
    }

    #endregion

    #region Approve

    [Fact]
    public async Task Approve_ShouldReturn401_WhenUnauthenticated()
    {
        var client = fixture.CreateClient();

        var response = await client.PostAsync(
            $"/api/verification/{Guid.NewGuid()}/approve", null);

        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Approve_ShouldReturn403_WhenNotAdmin()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.PostAsync(
            $"/api/verification/{Guid.NewGuid()}/approve", null);

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Approve_ShouldReturn404_WhenNoSubmission()
    {
        var admin = fixture.CreateClient(fixture.SeedState.Admin);

        var response = await admin.PostAsync(
            $"/api/verification/{Guid.NewGuid()}/approve", null);

        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Approve_ShouldReturn409_WhenAlreadyApproved()
    {
        var tenantId = TenantOf(fixture.SeedState.VenueManager1.Id);
        var admin = fixture.CreateClient(fixture.SeedState.Admin);

        var response = await admin.PostAsync($"/api/verification/{tenantId}/approve", null);

        await response.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Approve_ShouldReturn204_ApproveVerification_AndNotifyContact()
    {
        var owner = fixture.SeedState.UnverifiedVenueManager;
        var tenantId = TenantOf(owner.Id);
        var venue = fixture.SeedState.Venues.Single(v => v.TenantId == tenantId);
        await fixture.AddPendingVerificationAsync(
            tenantId, VerificationDocumentType.Licence, fixture.SeedNow.AddDays(-1));
        var admin = fixture.CreateClient(fixture.SeedState.Admin);

        var response = await admin.PostAsync($"/api/verification/{tenantId}/approve", null);

        await response.ShouldBe(HttpStatusCode.NoContent);
        var verification = fixture.Verifications.Single(v => v.TenantId == tenantId);
        Assert.Equal(TenantVerificationStatus.Approved, verification.Status);
        Assert.Contains(fixture.EmailSender.Sent, e => e.To == venue.Email);
    }

    /// <summary>Mirrors <see cref="GetPending_ShouldReturn200_WithArtistContactEnrichment"/>'s proof that
    /// <see cref="Concertable.B2B.Seed.Infrastructure.SeedState.ArtistManagerNoArtist"/> owns no artist
    /// by default (that test explicitly creates one before asserting on it) — this test deliberately does not,
    /// so the tenant is provably contactless.</summary>
    [Fact]
    public async Task Approve_ShouldReturn204_AndSendNothing_WhenTenantOwnsNoProfile()
    {
        var owner = fixture.SeedState.ArtistManagerNoArtist;
        var tenantId = TenantOf(owner.Id);
        await fixture.AddPendingVerificationAsync(
            tenantId, VerificationDocumentType.CompanyRegistration, fixture.SeedNow.AddDays(-1));
        var admin = fixture.CreateClient(fixture.SeedState.Admin);
        var alreadySent = fixture.EmailSender.Sent.Count;

        var response = await admin.PostAsync($"/api/verification/{tenantId}/approve", null);

        await response.ShouldBe(HttpStatusCode.NoContent);
        var verification = fixture.Verifications.Single(v => v.TenantId == tenantId);
        Assert.Equal(TenantVerificationStatus.Approved, verification.Status);
        Assert.Equal(alreadySent, fixture.EmailSender.Sent.Count);
    }

    #endregion

    #region Reject

    [Fact]
    public async Task Reject_ShouldReturn401_WhenUnauthenticated()
    {
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/verification/{Guid.NewGuid()}/reject", new RejectVerificationRequest { Reason = "x" });

        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reject_ShouldReturn403_WhenNotAdmin()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.PostAsJsonAsync(
            $"/api/verification/{Guid.NewGuid()}/reject", new RejectVerificationRequest { Reason = "x" });

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reject_ShouldReturn404_WhenNoSubmission()
    {
        var admin = fixture.CreateClient(fixture.SeedState.Admin);

        var response = await admin.PostAsJsonAsync(
            $"/api/verification/{Guid.NewGuid()}/reject", new RejectVerificationRequest { Reason = "x" });

        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reject_ShouldReturn409_WhenAlreadyApproved()
    {
        var tenantId = TenantOf(fixture.SeedState.VenueManager1.Id);
        var admin = fixture.CreateClient(fixture.SeedState.Admin);

        var response = await admin.PostAsJsonAsync(
            $"/api/verification/{tenantId}/reject", new RejectVerificationRequest { Reason = "x" });

        await response.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Reject_ShouldReturn204_RejectVerification_WithReason_AndNotifyContact()
    {
        var owner = fixture.SeedState.UnverifiedVenueManager;
        var tenantId = TenantOf(owner.Id);
        var venue = fixture.SeedState.Venues.Single(v => v.TenantId == tenantId);
        await fixture.AddPendingVerificationAsync(
            tenantId, VerificationDocumentType.Licence, fixture.SeedNow.AddDays(-1));
        var admin = fixture.CreateClient(fixture.SeedState.Admin);

        var response = await admin.PostAsJsonAsync(
            $"/api/verification/{tenantId}/reject",
            new RejectVerificationRequest { Reason = "Illegible scan." });

        await response.ShouldBe(HttpStatusCode.NoContent);
        var verification = fixture.Verifications.Single(v => v.TenantId == tenantId);
        Assert.Equal(TenantVerificationStatus.Rejected, verification.Status);
        Assert.Equal("Illegible scan.", verification.RejectionReason);
        Assert.Contains(fixture.EmailSender.Sent, e => e.To == venue.Email);
    }

    #endregion
}
