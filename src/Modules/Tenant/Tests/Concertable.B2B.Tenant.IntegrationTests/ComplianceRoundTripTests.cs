using System.Net;
using System.Net.Http.Json;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Domain;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Concertable.B2B.Tenant.IntegrationTests;

/// <summary>
/// The Phase 5 round-trip gate: nested owned value objects (Compliance owning RegisteredAddress) are the
/// main EF risk, so every assertion here reads back through a fresh context — never the change tracker
/// that wrote the row.
/// </summary>
[Collection("Integration")]
public sealed class ComplianceRoundTripTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public ComplianceRoundTripTests(ApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    private static UpdateTenantRequest BuildRequest() => new()
    {
        LegalName = "The Grand Venue Ltd",
        Compliance = new ComplianceDto
        {
            VatRegistered = true,
            VatNumber = "GB123456789",
            SellerIdentifier = "12345678",
            RegisteredAddress = new RegisteredAddressDto
            {
                Line1 = "1 High Street",
                Line2 = "Floor 2",
                City = "Manchester",
                Postcode = "M1 1AA",
                Country = "United Kingdom",
            },
            BankReference = "GB29NWBK60161331926819",
        },
    };

    [Fact]
    public async Task Get_BeforeSetup_ReturnsOrganizationWithoutCompliance()
    {
        var manager = fixture.SeedState.VenueManager1;
        var expectedTenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == manager.Id).Id;

        var client = fixture.CreateClient(manager);
        var response = await client.GetAsync("/api/organizations");

        await response.ShouldBe(HttpStatusCode.OK);
        var organization = await response.Content.ReadAsync<TenantDetails>();
        Assert.NotNull(organization);
        Assert.Equal(expectedTenantId, organization!.Id);
        Assert.Null(organization.Compliance);
    }

    [Fact]
    public async Task Update_RoundTripsNestedComplianceThroughAFreshContext()
    {
        var manager = fixture.SeedState.VenueManager1;
        var tenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == manager.Id).Id;
        var request = BuildRequest();

        var client = fixture.CreateClient(manager);
        var response = await client.PutAsJsonAsync("/api/organizations", request);
        await response.ShouldBe(HttpStatusCode.OK);

        var read = await client.GetFromJsonAsync<TenantDetails>("/api/organizations");
        Assert.NotNull(read);
        Assert.Equal(request.LegalName, read!.LegalName);
        Assert.Equal(request.Compliance, read.Compliance);

        using var scope = fixture.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        var tenant = await repository.GetByIdAsync(tenantId);

        var expected = new Compliance(
            vatRegistered: true,
            vatNumber: "GB123456789",
            sellerIdentifier: "12345678",
            registeredAddress: new RegisteredAddress("1 High Street", "Floor 2", "Manchester", "M1 1AA", "United Kingdom"),
            bankReference: "GB29NWBK60161331926819");
        Assert.NotNull(tenant);
        Assert.Equal(expected, tenant!.Compliance);
    }

    [Fact]
    public async Task Update_ReplacesExistingCompliance()
    {
        var manager = fixture.SeedState.VenueManager1;
        var client = fixture.CreateClient(manager);

        await (await client.PutAsJsonAsync("/api/organizations", BuildRequest())).ShouldBe(HttpStatusCode.OK);

        var replacement = new UpdateTenantRequest
        {
            LegalName = "Grand Venue Holdings Ltd",
            Compliance = new ComplianceDto
            {
                VatRegistered = false,
                VatNumber = null,
                SellerIdentifier = "87654321",
                RegisteredAddress = new RegisteredAddressDto
                {
                    Line1 = "99 New Road",
                    Line2 = null,
                    City = "Leeds",
                    Postcode = "LS1 4AB",
                    Country = "United Kingdom",
                },
                BankReference = "GB94BARC10201530093459",
            },
        };
        await (await client.PutAsJsonAsync("/api/organizations", replacement)).ShouldBe(HttpStatusCode.OK);

        var read = await client.GetFromJsonAsync<TenantDetails>("/api/organizations");
        Assert.NotNull(read);
        Assert.Equal(replacement.LegalName, read!.LegalName);
        Assert.Equal(replacement.Compliance, read.Compliance);
    }

    [Fact]
    public async Task Update_VatRegisteredWithoutNumber_ReturnsBadRequest()
    {
        var manager = fixture.SeedState.VenueManager1;
        var request = BuildRequest() with
        {
            Compliance = BuildRequest().Compliance with { VatNumber = null },
        };

        var client = fixture.CreateClient(manager);
        var response = await client.PutAsJsonAsync("/api/organizations", request);

        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_WithoutTenant_ReturnsNoContent()
    {
        var client = fixture.CreateClient(fixture.SeedState.Admin);

        var response = await client.GetAsync("/api/organizations");

        await response.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_WithoutTenant_ReturnsForbidden()
    {
        var client = fixture.CreateClient(fixture.SeedState.Admin);

        var response = await client.PutAsJsonAsync("/api/organizations", BuildRequest());

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }
}
