using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.Contracts.Enums;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Contracts;
using Microsoft.Extensions.Logging;
using Moq;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class ConcertServiceCreateTests
{
    private readonly ConfirmedBookingSnapshot booking;
    private readonly Mock<IConcertRepository> repository;
    private readonly ConcertService service;
    private ConcertEntity? addedConcert;

    public ConcertServiceCreateTests()
    {
        var venueTenantId = Guid.NewGuid();
        var artistTenantId = Guid.NewGuid();
        booking = new ConfirmedBookingSnapshot(
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
            new FlatFeeTerms(500m));
        var artist = new ArtistReadModel
        {
            Id = booking.ArtistId,
            TenantId = artistTenantId,
            UserId = Guid.NewGuid(),
            Name = "Artist",
            Genres = [new ArtistReadModelGenre { Genre = Genre.Rock }]
        };
        var venue = new VenueReadModel
        {
            Id = booking.VenueId,
            TenantId = venueTenantId,
            UserId = Guid.NewGuid(),
            Name = "Venue",
            About = "About"
        };
        repository = new Mock<IConcertRepository>();
        var artists = new Mock<IArtistReadModelRepository>();
        var venues = new Mock<IVenueReadModelRepository>();
        repository
            .Setup(value => value.GetByBookingIdAsync(booking.BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConcertEntity?)null);
        repository
            .Setup(value => value.AddAsync(It.IsAny<ConcertEntity>(), It.IsAny<CancellationToken>()))
            .Callback<ConcertEntity, CancellationToken>((concert, _) => addedConcert = concert)
            .ReturnsAsync((ConcertEntity concert, CancellationToken _) => concert);
        artists
            .Setup(value => value.GetByTenantIdAsync(artistTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artist);
        venues
            .Setup(value => value.GetByTenantIdAsync(venueTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(venue);
        service = new ConcertService(
            repository.Object,
            Mock.Of<IConcertReadRepository>(),
            Mock.Of<IInvoiceRepository>(),
            Mock.Of<IConcertValidator>(),
            Mock.Of<IConcertWorkflow>(),
            artists.Object,
            venues.Object,
            Mock.Of<IBookingConfirmationEmailSender>(),
            Mock.Of<IBus>(),
            Mock.Of<IBookingModule>(),
            Mock.Of<IUnitOfWork>(),
            TimeProvider.System,
            Mock.Of<ITenantContext>(),
            Mock.Of<ILogger<ConcertService>>());
    }

    [Fact]
    public async Task CreateAsync_ConfirmedBooking_AddsConcertAndPersists()
    {
        await service.CreateAsync(booking);

        Assert.NotNull(addedConcert);
        Assert.Equal(booking.ApplicationId, addedConcert.ApplicationId);
        Assert.Equal(booking.BookingId, addedConcert.BookingId);
        Assert.Equal(booking.VenueId, addedConcert.VenueId);
        Assert.False(addedConcert.RequiresDoorRevenue);
        repository.Verify(
            value => value.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ExistingConcertForBooking_DoesNotAddOrSave()
    {
        repository
            .Setup(value => value.GetByBookingIdAsync(booking.BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ConcertEntity.CreateDraft(booking, "Existing", "About", [Genre.Rock]));

        await service.CreateAsync(booking);

        Assert.Null(addedConcert);
        repository.Verify(
            value => value.AddAsync(It.IsAny<ConcertEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repository.Verify(
            value => value.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
