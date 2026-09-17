using System.Net;
using System.Net.Mime;
using System.Text.Json;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Privacy.Application.Interfaces;
using Concertable.B2B.Privacy.Domain.Lifecycle;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.User.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Concertable.B2B.Privacy.IntegrationTests;

[Collection("Integration")]
public sealed class SubjectRightsApiTests : IAsyncLifetime
{
    private readonly PrivacyApiFixture fixture;

    public SubjectRightsApiTests(PrivacyApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    #region RequestErasure

    [Fact]
    public async Task RequestErasure_CleanSubject_AnonymisesAndCompletes()
    {
        // ArtistManagerNoArtist registered but never set up an organisation: a tenant it solely owns, no
        // concerts, so no live obligation — the check clears and erasure runs to completion.
        var subject = fixture.SeedState.ArtistManagerNoArtist;

        var result = await fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<ISubjectErasureService>().RequestErasureAsync(subject.Id));

        Assert.Equal(ErasureState.Completed, result.State);

        var memberships = await fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<ITenantModule>().GetMembershipsAsync(subject.Id));
        Assert.Empty(memberships);

        var user = await fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<IUserModule>().GetUserExportAsync(subject.Id));
        Assert.True(user.TryGetValue(out var fragment));
        Assert.Contains("erased", fragment.Email);
    }

    [Fact]
    public async Task RequestErasure_SubjectWithLiveObligation_DefersAndTouchesNothing()
    {
        // VenueManager1's tenant is the venue party to the seeded Accepted (payment-pending) booking — a live
        // financial obligation — so erasure must fail closed to Deferred and leave every row intact.
        var subject = fixture.SeedState.VenueManager1;

        var result = await fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<ISubjectErasureService>().RequestErasureAsync(subject.Id));

        Assert.Equal(ErasureState.Deferred, result.State);
        Assert.NotNull(result.DeferralReason);

        var memberships = await fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<ITenantModule>().GetMembershipsAsync(subject.Id));
        Assert.NotEmpty(memberships);

        var user = await fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<IUserModule>().GetUserExportAsync(subject.Id));
        Assert.True(user.TryGetValue(out var fragment));
        Assert.DoesNotContain("erased", fragment.Email);
    }

    [Fact]
    public async Task RequestErasure_Route_IsReachableForAdminAndForbiddenOtherwise()
    {
        var subjectId = Guid.NewGuid();
        var admin = fixture.CreateClient(fixture.SeedState.Admin);
        var nonAdmin = fixture.CreateClient(fixture.SeedState.VenueManager2);

        var allowed = await admin.PostAsync($"/api/subject-erasure/{subjectId}", null);
        var forbidden = await nonAdmin.PostAsync($"/api/subject-erasure/{subjectId}", null);

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    #endregion

    #region Export

    [Fact]
    public async Task Export_SubjectWithData_ReturnsExactlyTheirBundle()
    {
        var subject = fixture.SeedState.VenueManager1;

        var download = await fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<ISubjectExporter>().ExportAsync(subject.Id));

        Assert.Equal(MediaTypeNames.Application.Json, download.ContentType);
        Assert.Contains(subject.Id.ToString("N"), download.FileName);

        using var doc = JsonDocument.Parse(download.Content);
        var root = doc.RootElement;
        Assert.Equal(subject.Id, root.GetProperty("subjectId").GetGuid());
        Assert.Equal(JsonValueKind.Object, root.GetProperty("user").ValueKind);
        Assert.NotEqual(0, root.GetProperty("memberships").GetArrayLength());
    }

    #endregion
}
