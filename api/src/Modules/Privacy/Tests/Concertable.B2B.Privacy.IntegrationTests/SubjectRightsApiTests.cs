using System.Net;
using System.Net.Mime;
using System.Text.Json;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Privacy.Application.Interfaces;
using Concertable.B2B.Privacy.Domain.Lifecycle;
using Concertable.B2B.Privacy.Domain.Entities;
using Concertable.B2B.Privacy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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
        this.fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => this.fixture.ResetAsync();
    public Task DisposeAsync() { this.fixture.DetachOutput(); return Task.CompletedTask; }

    #region RequestErasure

    [Fact]
    public async Task RequestErasure_CleanSubject_AnonymisesAndCompletes()
    {
        var subject = this.fixture.SeedState.ArtistManagerNoArtist;

        var outcome = await this.fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<ISubjectErasureService>().RequestErasureAsync(subject.Id));
        Assert.True(outcome.TryGetValue(out var result));

        Assert.Equal(ErasureState.Completed, result.State);

        var memberships = await this.fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<ITenantModule>().GetMembershipsAsync(subject.Id));
        Assert.Empty(memberships);

        var user = await this.fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<IUserModule>().GetSubjectProfileAsync(subject.Id));
        Assert.True(user.TryGetValue(out var fragment));
        Assert.Contains("erased", fragment.Email);
    }

    [Fact]
    public async Task RequestErasure_SubjectWithLiveObligation_DefersAndTouchesNothing()
    {
        var subject = this.fixture.SeedState.VenueManager1;

        var outcome = await this.fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<ISubjectErasureService>().RequestErasureAsync(subject.Id));
        Assert.True(outcome.TryGetValue(out var result));

        Assert.Equal(ErasureState.Deferred, result.State);
        Assert.NotNull(result.DeferralReason);

        var memberships = await this.fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<ITenantModule>().GetMembershipsAsync(subject.Id));
        Assert.NotEmpty(memberships);

        var user = await this.fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<IUserModule>().GetSubjectProfileAsync(subject.Id));
        Assert.True(user.TryGetValue(out var fragment));
        Assert.DoesNotContain("erased", fragment.Email);
    }

    [Fact]
    public async Task RequestErasure_Route_IsReachableForAdminAndForbiddenOtherwise()
    {
        var subjectId = Guid.NewGuid();
        var admin = this.fixture.CreateClient(this.fixture.SeedState.Admin);
        var nonAdmin = this.fixture.CreateClient(this.fixture.SeedState.VenueManager2);

        var allowed = await admin.PostAsync($"/api/subject-erasure/{subjectId}", null);
        var forbidden = await nonAdmin.PostAsync($"/api/subject-erasure/{subjectId}", null);

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    #endregion

    [Fact]
    public async Task CompletedJournal_RejectsStaleCaptureFromAnotherContext()
    {
        var subjectId = Guid.NewGuid();
        await using var firstScope = this.fixture.Services.CreateAsyncScope();
        var firstContext = firstScope.ServiceProvider.GetRequiredService<PrivacyDbContext>();
        var request = SubjectErasureRequestEntity.Create(subjectId, this.fixture.SeedNow);
        request.Fire(ErasureTrigger.Begin);
        firstContext.SubjectErasureRequests.Add(request);
        await firstContext.SaveChangesAsync();

        await using var staleScope = this.fixture.Services.CreateAsyncScope();
        var staleContext = staleScope.ServiceProvider.GetRequiredService<PrivacyDbContext>();
        var staleRequest = await staleContext.SubjectErasureRequests.SingleAsync(value => value.SubjectId == subjectId);

        request.CaptureFanOutState("original@example.test", new HashSet<Guid> { Guid.NewGuid() });
        await firstContext.SaveChangesAsync();
        request.Fire(ErasureTrigger.Complete);
        request.RecordCompletion(this.fixture.SeedNow);
        await firstContext.SaveChangesAsync();

        staleRequest.CaptureFanOutState("original@example.test", new HashSet<Guid> { Guid.NewGuid() });
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleContext.SaveChangesAsync());

        await using var verificationScope = this.fixture.Services.CreateAsyncScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<PrivacyDbContext>();
        var completed = await verificationContext.SubjectErasureRequests.SingleAsync(value => value.SubjectId == subjectId);
        Assert.Equal(ErasureState.Completed, completed.State);
        Assert.Null(completed.SubjectEmail);
        Assert.Null(completed.TenantIds);
    }

    [Fact]
    public async Task ResumableBatch_RotatesPastPersistentlyDeferredRequests()
    {
        var start = this.fixture.SeedNow.AddDays(-1);
        var requests = Enumerable.Range(0, 101)
            .Select(index => SubjectErasureRequestEntity.Create(Guid.NewGuid(), start.AddSeconds(index)))
            .ToArray();
        foreach (var request in requests)
            request.Fire(ErasureTrigger.Defer);
        await using var scope = this.fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PrivacyDbContext>();
        context.SubjectErasureRequests.AddRange(requests);
        await context.SaveChangesAsync();
        var repository = scope.ServiceProvider.GetRequiredService<ISubjectErasureRepository>();
        var first = await repository.ListResumableSubjectIdsAsync(100);
        Assert.DoesNotContain(requests[100].SubjectId, first);

        foreach (var request in requests.Take(100))
            request.RecordAttempt(this.fixture.SeedNow);
        await context.SaveChangesAsync();
        var next = await repository.ListResumableSubjectIdsAsync(100);
        Assert.Equal(requests[100].SubjectId, next[0]);
    }

    #region Export

    [Fact]
    public async Task Export_SubjectWithData_ReturnsExactlyTheirBundle()
    {
        var subject = this.fixture.SeedState.VenueManager1;

        var download = await this.fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<ISubjectExporter>().ExportAsync(subject.Id));

        Assert.Equal(MediaTypeNames.Application.Json, download.ContentType);
        Assert.Contains(subject.Id.ToString("N"), download.FileName);

        using var doc = JsonDocument.Parse(download.Content);
        var root = doc.RootElement;
        Assert.Equal(subject.Id, root.GetProperty("subjectId").GetGuid());
        Assert.Equal(JsonValueKind.Object, root.GetProperty("user").ValueKind);
        Assert.NotEqual(0, root.GetProperty("memberships").GetArrayLength());
    }

    [Fact]
    public async Task ExportAsync_UnknownSubject_EmitsANullUserFragment()
    {
        var unknownSubjectId = Guid.NewGuid();

        var download = await this.fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<ISubjectExporter>().ExportAsync(unknownSubjectId));

        using var document = JsonDocument.Parse(download.Content);
        var root = document.RootElement;
        Assert.Equal(JsonValueKind.Null, root.GetProperty("user").ValueKind);
        Assert.Empty(root.GetProperty("memberships").EnumerateArray());
        Assert.Empty(root.GetProperty("contracts").EnumerateArray());
    }

    #endregion
}
