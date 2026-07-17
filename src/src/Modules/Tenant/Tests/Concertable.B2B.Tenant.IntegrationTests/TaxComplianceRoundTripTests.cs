using System.Net;
using System.Net.Http.Json;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Concertable.B2B.Tenant.IntegrationTests;

/// <summary>
/// The round-trip gate: nested owned value objects (TaxCompliance owning RegisteredAddress) are the
/// main EF risk, so every assertion here reads back through a fresh context — never the change tracker
/// that wrote the row. Completeness is presence: a tenant with tax data on file is complete.
/// </summary>
[Collection("Integration")]
public sealed class TaxComplianceRoundTripTests : IAsyncLifetime
{
    private readonly TenantApiFixture fixture;

    public TaxComplianceRoundTripTests(TenantApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    private static UpdateTenantRequest BuildRequest() => new()
    {
        LegalName = "The Grand Venue Ltd",
        TaxCompliance = new TaxComplianceDto
        {
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
    public async Task Get_BeforeSetup_ReturnsOrganizationWithoutTaxCompliance()
    {
        // A registered operator who hasn't completed organization setup — the only seeded tenant left without tax details.
        var manager = fixture.SeedState.VenueManagerNoVenue;
        var expectedTenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == manager.Id).Id;

        var client = fixture.CreateClient(manager);
        var response = await client.GetAsync("/api/organizations");

        await response.ShouldBe(HttpStatusCode.OK);
        var organization = await response.Content.ReadAsync<TenantDetails>();
        Assert.NotNull(organization);
        Assert.Equal(expectedTenantId, organization!.Id);
        // No tax data yet = not complete (the nag's source of truth).
        Assert.Null(organization.TaxCompliance);
    }

    [Fact]
    public async Task Update_RoundTripsNestedTaxComplianceThroughAFreshContext()
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
        // Same DTO shape for read and write, so it round-trips by value; presence == complete.
        Assert.Equal(request.TaxCompliance, read.TaxCompliance);

        var tenant = await fixture.Tenants.SingleOrDefaultAsync(t => t.Id == tenantId);

        var expected = new TaxCompliance(
            vatNumber: "GB123456789",
            sellerIdentifier: "12345678",
            registeredAddress: new RegisteredAddress("1 High Street", "Floor 2", "Manchester", "M1 1AA", "United Kingdom"),
            bankReference: "GB29NWBK60161331926819");
        Assert.NotNull(tenant);
        Assert.Equal(expected, tenant!.TaxCompliance);
    }

    [Fact]
    public async Task Update_ReplacesExistingTaxCompliance()
    {
        var manager = fixture.SeedState.VenueManager1;
        var client = fixture.CreateClient(manager);

        await (await client.PutAsJsonAsync("/api/organizations", BuildRequest())).ShouldBe(HttpStatusCode.OK);

        var replacement = new UpdateTenantRequest
        {
            LegalName = "Grand Venue Holdings Ltd",
            TaxCompliance = new TaxComplianceDto
            {
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
        Assert.Equal(replacement.TaxCompliance, read.TaxCompliance);
    }

    [Fact]
    public async Task Update_InvalidVatNumberFormat_ReturnsBadRequest()
    {
        var manager = fixture.SeedState.VenueManager1;
        var request = BuildRequest() with
        {
            TaxCompliance = BuildRequest().TaxCompliance with { VatNumber = "NOTAVATNUMBER" },
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
