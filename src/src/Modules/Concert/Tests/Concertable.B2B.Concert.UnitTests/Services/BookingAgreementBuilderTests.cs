using System.Net;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.Kernel;
using Concertable.Kernel.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Services;

public sealed class BookingAgreementBuilderTests
{
    private readonly Mock<IContractAccessor> contractAccessor = new();
    private readonly Mock<IApplicationRepository> applicationRepository = new();
    private readonly Mock<IBookingAgreementRepository> agreementRepository = new();
    private readonly Mock<IAgreementTermsRenderer> termsRenderer = new();
    private readonly Mock<ICurrentUser> currentUser = new();
    private readonly Mock<IClientContext> clientContext = new();
    private readonly BookingAgreementBuilder builder;

    private readonly ESignature artistESignature = new(
        Guid.NewGuid(), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        IPAddress.Parse("203.0.113.7"), "artist-agent", "Artie Artist", null);

    public BookingAgreementBuilderTests()
    {
        contractAccessor.SetupGet(c => c.Contract).Returns(new FlatFeeContract { PaymentMethod = PaymentMethod.Transfer, Fee = 500m });
        applicationRepository
            .Setup(r => r.GetArtistAndVenueByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(((ArtistReadModel, VenueReadModel)?)(
                new ArtistReadModel { Id = 1, Name = "Artie Artist" },
                new VenueReadModel { Id = 2, Name = "Vera Venue" }));
        termsRenderer.Setup(r => r.Render(It.IsAny<IContract>())).Returns("terms");
        currentUser.SetupGet(u => u.Id).Returns(Guid.NewGuid());
        clientContext.SetupGet(c => c.IpAddress).Returns(IPAddress.Loopback);
        clientContext.SetupGet(c => c.UserAgent).Returns("venue-agent");

        builder = new BookingAgreementBuilder(
            contractAccessor.Object,
            applicationRepository.Object,
            agreementRepository.Object,
            termsRenderer.Object,
            currentUser.Object,
            clientContext.Object,
            Options.Create(new LegalSettings { PlatformTermsVersion = "2026-07" }),
            new FakeTimeProvider());
    }

    // The agreement snapshots the artist's apply-time signature (a complex-type value copied by value
    // on save) and builds the venue's fresh from the accepting user + request context.
    [Fact]
    public async Task BuildAsync_SnapshotsArtistSignatureFromApplication_AndBuildsVenueSignatureFromRequest()
    {
        BookingAgreementEntity? built = null;
        agreementRepository
            .Setup(r => r.AddAsync(It.IsAny<BookingAgreementEntity>(), It.IsAny<CancellationToken>()))
            .Callback<BookingAgreementEntity, CancellationToken>((a, _) => built = a)
            .ReturnsAsync((BookingAgreementEntity a, CancellationToken _) => a);

        var application = StandardApplication.Create(artistId: 1, opportunityId: 10, ContractType.FlatFee);
        application.Opportunity = OpportunityEntity.Create(
            venueId: 2,
            new DateRange(new DateTime(2026, 6, 1, 20, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 1, 23, 0, 0, DateTimeKind.Utc)),
            contractId: 3);
        application.RecordArtistESignature(artistESignature, "fingerprint");

        await builder.BuildAsync(application, bookingId: 42, new ESignatureRequest { SignatoryName = "Vera Venue" });

        Assert.NotNull(built);
        Assert.Equal(application.ArtistESignature, built.ArtistESignature);
        Assert.Equal("Vera Venue", built.VenueESignature.SignatoryName);
    }
}
