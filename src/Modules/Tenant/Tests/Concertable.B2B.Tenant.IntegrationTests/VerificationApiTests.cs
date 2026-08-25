using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Xunit.Abstractions;

namespace Concertable.B2B.Tenant.IntegrationTests;

/// <summary>
/// Tenant-facing verification submission on <c>api/organization/verification</c> — the status read (no
/// row = 204) and the evidence-upload round trip (first submission, resubmission after rejection, the
/// eligibility gate while pending/approved, content-type/size validation, and the <c>TenantSettingsEdit</c>
/// permission boundary). Admin review (approve/reject) is a later phase and out of scope here.
/// </summary>
[Collection("Integration")]
public sealed class VerificationApiTests : IAsyncLifetime
{
    private readonly TenantApiFixture fixture;

    public VerificationApiTests(TenantApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    private Guid TenantOf(Guid userId) => fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == userId).Id;

    private static Task<HttpResponseMessage> Submit(
        HttpClient client, params (IFormFile File, VerificationDocumentType DocumentType)[] documents) =>
        SubmitAsync(client, documents);

    private static async Task<HttpResponseMessage> SubmitAsync(
        HttpClient client, IReadOnlyList<(IFormFile File, VerificationDocumentType DocumentType)> documents) =>
        await client.PostAsync("/api/organization/verification/documents", await documents.ToFormContent());

    #region Get

    [Fact]
    public async Task Get_NeverSubmitted_ReturnsNoContent()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync("/api/organization/verification");

        await response.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Get_AfterSubmission_ReturnsPendingWithDocuments()
    {
        var owner = fixture.SeedState.VenueManager1;
        var client = fixture.CreateClient(owner);
        await Submit(client, (VerificationFileBuilder.Pdf(), VerificationDocumentType.Licence));

        var response = await client.GetAsync("/api/organization/verification");

        await response.ShouldBe(HttpStatusCode.OK);
        var status = await response.Content.ReadAsync<VerificationStatusDto>();
        Assert.Equal(TenantVerificationStatus.Pending, status!.Status);
        Assert.Single(status.Documents);
        Assert.Equal(VerificationDocumentType.Licence, status.Documents[0].DocumentType);
    }

    #endregion

    #region SubmitDocuments

    [Fact]
    public async Task SubmitDocuments_FirstSubmission_PersistsPendingVerificationWithDocuments()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var client = fixture.CreateClient(owner);

        var response = await Submit(
            client,
            (VerificationFileBuilder.Pdf("licence.pdf"), VerificationDocumentType.Licence),
            (VerificationFileBuilder.Pdf("address.pdf"), VerificationDocumentType.ProofOfAddress));

        await response.ShouldBe(HttpStatusCode.OK);
        var verification = fixture.Verifications.Single(v => v.TenantId == tenantId);
        Assert.Equal(TenantVerificationStatus.Pending, verification.Status);
        Assert.Equal(2, verification.Documents.Count);
    }

    [Fact]
    public async Task SubmitDocuments_WhilePending_ReturnsConflict_WithoutUploadingAgain()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var client = fixture.CreateClient(owner);
        await Submit(client, (VerificationFileBuilder.Pdf(), VerificationDocumentType.Licence));

        var response = await Submit(client, (VerificationFileBuilder.Pdf(), VerificationDocumentType.Licence));

        await response.ShouldBe(HttpStatusCode.Conflict);
        Assert.Equal(1, fixture.Verifications.Count(v => v.TenantId == tenantId));
    }

    [Fact]
    public async Task SubmitDocuments_AfterRejection_ResubmitsAndClearsReason()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var rejected = await fixture.AddRejectedVerificationAsync(
            tenantId, VerificationDocumentType.Licence, "Illegible scan.", DateTime.UtcNow.AddDays(-1));
        var client = fixture.CreateClient(owner);

        var response = await Submit(client, (VerificationFileBuilder.Pdf(), VerificationDocumentType.Licence));

        await response.ShouldBe(HttpStatusCode.OK);
        var verification = fixture.Verifications.Single(v => v.Id == rejected.Id);
        Assert.Equal(TenantVerificationStatus.Pending, verification.Status);
        Assert.Null(verification.RejectionReason);
        Assert.Equal(2, verification.Documents.Count); // append-only: the rejected evidence stays alongside the new
    }

    [Fact]
    public async Task SubmitDocuments_DisallowedContentType_ReturnsBadRequest()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await Submit(client, (VerificationFileBuilder.TextFile(), VerificationDocumentType.Licence));

        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitDocuments_NoFiles_ReturnsBadRequest()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await SubmitAsync(client, []);

        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitDocuments_AsStaff_IsForbidden()
    {
        var owner = fixture.SeedState.VenueManager1;
        var tenantId = TenantOf(owner.Id);
        var staff = fixture.SeedState.VenueManagerNoVenue;
        await fixture.AddMembershipAsync(tenantId, staff.Id, TenantRole.Staff);
        var client = fixture.CreateClient(staff);
        client.DefaultRequestHeaders.Add(TenantHeaders.TenantId, tenantId.ToString());

        var response = await Submit(client, (VerificationFileBuilder.Pdf(), VerificationDocumentType.Licence));

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    #endregion
}
