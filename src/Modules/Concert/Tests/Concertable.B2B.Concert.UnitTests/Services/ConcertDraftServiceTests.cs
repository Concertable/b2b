using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Events;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.Contracts.Enums;
using Concertable.Kernel.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Services;

public sealed class ConcertDraftServiceTests
{
    [Fact]
    public async Task CreateAsync_AddsConcertExplicitlyAndPersistsBookingConfirmation()
    {
        var venueTenantId = Guid.NewGuid();
        var artistTenantId = Guid.NewGuid();
        var period = new DateRange(
            new DateTime(2026, 9, 1, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 23, 0, 0, DateTimeKind.Utc));
        var application = StandardApplication.Create(
            artistId: 11,
            opportunityId: 12,
            DealType.FlatFee,
            venueTenantId,
            artistTenantId);
        var booking = StandardBooking.Create(application.ToAccepted());
        var artist = new ArtistReadModel
        {
            Id = 11,
            UserId = Guid.NewGuid(),
            Name = "Artist",
            Genres = [new ArtistReadModelGenre { Genre = Genre.Rock }]
        };
        var opportunity = OpportunityEntity.Create(venueId: 13, period, dealId: 14, [Genre.Rock]);
        var venue = new VenueReadModel
        {
            Id = 13,
            UserId = Guid.NewGuid(),
            Name = "Venue",
            About = "About"
        };
        var bookingRepository = new Mock<IBookingRepository>();
        var concertRepository = new Mock<IConcertRepository>();
        var notifier = new Mock<IConcertNotifier>();
        ConcertEntity? addedConcert = null;

        bookingRepository
            .Setup(value => value.GetDraftContextByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookingDraftContext(booking, artist, opportunity, venue));
        concertRepository
            .Setup(value => value.AddAsync(It.IsAny<ConcertEntity>(), It.IsAny<CancellationToken>()))
            .Callback<ConcertEntity, CancellationToken>((concert, _) => addedConcert = concert)
            .ReturnsAsync((ConcertEntity concert, CancellationToken _) => concert);
        notifier
            .Setup(value => value.ConcertDraftCreatedAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);
        var service = new ConcertDraftService(
            bookingRepository.Object,
            concertRepository.Object,
            notifier.Object,
            Mock.Of<ILogger<ConcertDraftService>>());

        var result = await service.CreateAsync(7);

        Assert.True(result.IsSuccess);
        Assert.NotNull(addedConcert);
        Assert.Equal(booking.ApplicationId, addedConcert.ApplicationId);
        Assert.Equal(venue.Id, addedConcert.VenueId);
        Assert.False(addedConcert.RequiresDoorRevenue);
        Assert.Contains(booking.DomainEvents, value => value is BookingConfirmedDomainEvent);
        concertRepository.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        bookingRepository.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
