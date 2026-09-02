using System.Net;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.Kernel.ValueObjects;
using Concertable.Kernel.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Concertable.Kernel.Specifications;

namespace Concertable.B2B.Concert.UnitTests.Services;

public sealed class ContractIssuerTests
{
    private readonly Mock<IDealAccessor> dealAccessor = new();
    private readonly Mock<IApplicationRepository> applicationRepository = new();
    private readonly Mock<IOpportunityRepository> opportunityRepository = new();
    private readonly Mock<IContractRepository> contractRepository = new();
    private readonly Mock<IDealTermsRenderer> termsRenderer = new();
    private readonly Mock<ICurrentUser> currentUser = new();
    private readonly Mock<IClientContext> clientContext = new();
    private readonly ContractIssuer issuer;

    private readonly ESignature artistESignature = new(
        Guid.NewGuid(), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        IPAddress.Parse("203.0.113.7"), "artist-agent", "Artie Artist", null);

    private static readonly DateRange OpportunityPeriod = new(
        new DateTime(2026, 6, 1, 20, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 1, 23, 0, 0, DateTimeKind.Utc));

    public ContractIssuerTests()
    {
        dealAccessor.SetupGet(c => c.Deal).Returns(new FlatFeeDealDto { PaymentMethod = PaymentMethod.Transfer, Fee = 500m });
        applicationRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<ISpecification<ApplicationEntity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationWithArtistAndVenue());
        opportunityRepository
            .Setup(r => r.GetPeriodByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(OpportunityPeriod);
        termsRenderer.Setup(r => r.Render(It.IsAny<DealDto>())).Returns("terms");
        currentUser.SetupGet(u => u.Id).Returns(Guid.NewGuid());
        clientContext.SetupGet(c => c.IpAddress).Returns(IPAddress.Loopback);
        clientContext.SetupGet(c => c.UserAgent).Returns("venue-agent");

        issuer = new ContractIssuer(
            dealAccessor.Object,
            applicationRepository.Object,
            opportunityRepository.Object,
            contractRepository.Object,
            termsRenderer.Object,
            currentUser.Object,
            clientContext.Object,
            Options.Create(new LegalSettings { PlatformTermsVersion = "2026-07" }),
            new FakeTimeProvider());
    }

    // The contract snapshots the artist's apply-time signature (a complex-type value copied by value
    // on save) and builds the venue's fresh from the accepting user + request context.
    [Fact]
    public async Task IssueAsync_SnapshotsArtistSignatureFromApplication_AndBuildsVenueSignatureFromRequest()
    {
        ContractEntity? built = null;
        contractRepository
            .Setup(r => r.AddAsync(It.IsAny<ContractEntity>(), It.IsAny<CancellationToken>()))
            .Callback<ContractEntity, CancellationToken>((a, _) => built = a)
            .ReturnsAsync((ContractEntity a, CancellationToken _) => a);

        var application = StandardApplication.Create(
            artistId: 1,
            opportunityId: 10,
            DealType.FlatFee,
            Guid.NewGuid(),
            Guid.NewGuid());
        application.Opportunity = OpportunityEntity.Create(
            venueId: 2,
            new DateRange(new DateTime(2026, 6, 1, 20, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 1, 23, 0, 0, DateTimeKind.Utc)),
            dealId: 3);
        application.RecordArtistESignature(artistESignature, "fingerprint");

        var booking = StandardBooking.Create(application);

        await issuer.IssueAsync(application, booking, new ESignatureRequest { SignatoryName = "Vera Venue" });

        Assert.NotNull(built);
        Assert.Equal(application.ArtistESignature, built.ArtistESignature);
        Assert.Equal("Vera Venue", built.VenueESignature.SignatoryName);
        Assert.Equal(application.VenueTenantId, built.VenueTenantId);
        Assert.Equal(application.ArtistTenantId, built.ArtistTenantId);
    }

    private static StandardApplication ApplicationWithArtistAndVenue()
    {
        var opportunity = OpportunityEntity.Create(2, OpportunityPeriod, 1);
        opportunity.Venue = new VenueReadModel { Id = 2, Name = "Vera Venue" };

        var application = StandardApplication.Create(1, 2, DealType.FlatFee, Guid.NewGuid(), Guid.NewGuid());
        application.Artist = new ArtistReadModel { Id = 1, Name = "Artie Artist" };
        application.Opportunity = opportunity;

        return application;
    }
}
