using System.Net;
using System.Net.Mime;
using System.Text.Json;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Privacy.Application.Interfaces;
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


    [Fact]
    public async Task Export_Route_RequiresPersistedAdminAuthority()
    {
        var subjectId = this.fixture.SeedState.VenueManager1.Id;
        var admin = this.fixture.CreateClient(this.fixture.SeedState.Admin);
        var member = this.fixture.CreateClient(this.fixture.SeedState.VenueManager2);
        var allowed = await admin.GetAsync($"/api/subject-export/{subjectId}");
        var forbidden = await member.GetAsync($"/api/subject-export/{subjectId}");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(MediaTypeNames.Application.Json, allowed.Content.Headers.ContentType?.MediaType);
    }

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
        var contents = root.GetProperty("messages").EnumerateArray()
            .Select(message => message.GetProperty("content").GetString()).ToArray();
        Assert.Contains("Test inbox message — venue to artist.", contents);
        Assert.DoesNotContain("Test inbox message — artist to venue.", contents);
        foreach (var contract in root.GetProperty("contracts").EnumerateArray())
        {
            Assert.False(contract.TryGetProperty("venueName", out _));
            Assert.False(contract.TryGetProperty("artistName", out _));
        }
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
        Assert.Empty(root.GetProperty("messages").EnumerateArray());
        Assert.Empty(root.GetProperty("concertRecords").GetProperty("invoices").EnumerateArray());
    }

    [Fact]
    public async Task Export_EmptySubject_DoesNotReturnSystemAuthoredMessages()
    {
        var download = await this.fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<ISubjectExporter>().ExportAsync(Guid.Empty));
        using var document = JsonDocument.Parse(download.Content);
        Assert.Empty(document.RootElement.GetProperty("messages").EnumerateArray());
    }

}
