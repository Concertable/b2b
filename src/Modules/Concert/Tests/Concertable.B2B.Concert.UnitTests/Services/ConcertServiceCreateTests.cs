using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.Contracts.Enums;
using Concertable.Kernel.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class ConcertServiceCreateTests
{
    private readonly ConfirmedBooking booking;
    private readonly Mock<IConcertRepository> repository;
    private readonly ConcertService service;
    private ConcertEntity? addedConcert;

    public ConcertServiceCreateTests()
    {
        var venueTenantId = Guid.NewGuid();
        var artistTenantId = Guid.NewGuid();
        this.booking = new ConfirmedBooking(
            Guid.NewGuid(),
            7,
            8,
            9,
            11,
            13,
            venueTenantId,
            artistTenantId,
            DealType.FlatFee,
            false,
            new DateTime(2026, 9, 1, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 23, 0, 0, DateTimeKind.Utc),
            [Genre.Rock],
            new FlatFeeBookingTerms(500m));
        var artist = new ArtistReadModel
        {
            Id = this.booking.ArtistId,
            TenantId = artistTenantId,
            UserId = Guid.NewGuid(),
            Name = "Artist",
            Genres = [new ArtistReadModelGenre { Genre = Genre.Rock }]
        };
        var venue = new VenueReadModel
        {
            Id = this.booking.VenueId,
            TenantId = venueTenantId,
            UserId = Guid.NewGuid(),
            Name = "Venue",
            About = "About"
        };
        this.repository = new Mock<IConcertRepository>();
        var artists = new Mock<IArtistReadModelRepository>();
        var venues = new Mock<IVenueReadModelRepository>();
        this.repository
            .Setup(value => value.GetByBookingIdAsync(this.booking.BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConcertEntity?)null);
        this.repository
            .Setup(value => value.AddAsync(It.IsAny<ConcertEntity>(), It.IsAny<CancellationToken>()))
            .Callback<ConcertEntity, CancellationToken>((concert, _) => this.addedConcert = concert)
            .ReturnsAsync((ConcertEntity concert, CancellationToken _) => concert);
        artists
            .Setup(value => value.GetByTenantIdAsync(artistTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artist);
        venues
            .Setup(value => value.GetByTenantIdAsync(venueTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(venue);
        this.service = new ConcertService(
            this.repository.Object,
            Mock.Of<IConcertReadRepository>(),
            Mock.Of<IInvoiceRepository>(),
            Mock.Of<IConcertValidator>(),
            artists.Object,
            venues.Object,
            Mock.Of<IConcertNotifier>(),
            Mock.Of<IBookingConfirmationEmailSender>(),
            TimeProvider.System,
            Mock.Of<ITenantContext>(),
            Mock.Of<ILogger<ConcertService>>());
    }

    [Fact]
    public async Task CreateAsync_ConfirmedBooking_AddsConcertAndPersists()
    {
        await this.service.CreateAsync(this.booking);

        Assert.NotNull(this.addedConcert);
        Assert.Equal(this.booking.ApplicationId, this.addedConcert.ApplicationId);
        Assert.Equal(this.booking.BookingId, this.addedConcert.BookingId);
        Assert.Equal(this.booking.VenueId, this.addedConcert.VenueId);
        Assert.False(this.addedConcert.RequiresDoorRevenue);
        this.repository.Verify(
            value => value.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
