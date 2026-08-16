using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Projections;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.B2B.Venue.Contracts;
using Concertable.Contracts.Enums;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Reunion;

namespace Concertable.B2B.Concert.UnitTests.Services;

public sealed class OpportunityDashboardServiceTests
{
    private readonly Mock<IOpportunityRepository> repository;
    private readonly Mock<IPublicOpportunityRepository> publicRepository;
    private readonly Mock<IVenueModule> venueModule;
    private readonly Mock<IArtistModule> artistModule;
    private readonly Mock<IDealModule> dealModule;
    private readonly FakeTimeProvider timeProvider;
    private readonly OpportunityDashboardService service;

    public OpportunityDashboardServiceTests()
    {
        this.repository = new Mock<IOpportunityRepository>();
        this.publicRepository = new Mock<IPublicOpportunityRepository>();
        this.venueModule = new Mock<IVenueModule>();
        this.artistModule = new Mock<IArtistModule>();
        this.dealModule = new Mock<IDealModule>();
        this.timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero));
        this.service = new OpportunityDashboardService(
            this.repository.Object,
            this.publicRepository.Object,
            this.venueModule.Object,
            this.artistModule.Object,
            this.dealModule.Object,
            this.timeProvider);
    }

    #region GetApplicationMetricsForCurrentVenueAsync

    [Fact]
    public async Task GetApplicationMetricsForCurrentVenueAsync_CurrentVenue_MapsProjectionAndDeadline()
    {
        var projection = new OpportunityApplicationProjection
        {
            Id = 1,
            VenueId = 42,
            VenueName = "The Venue",
            StartDate = new DateTime(2026, 8, 26, 20, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 8, 26, 23, 0, 0, DateTimeKind.Utc),
            Genres = [Genre.Rock],
            DealId = 7,
            ApplicationCount = 4
        };
        var deal = new FlatFeeDeal { Id = 7, PaymentMethod = PaymentMethod.Cash, Fee = 500 };
        this.venueModule
            .Setup(module => module.GetVenueIdForCurrentTenantAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option.Some(42));
        this.repository
            .Setup(value => value.GetOpenWithApplicationCountsByVenueIdAsync(42))
            .ReturnsAsync([projection]);
        this.dealModule
            .Setup(module => module.GetByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([deal]);

        var result = await this.service.GetApplicationMetricsForCurrentVenueAsync();

        Assert.True(result.TryGetValue(out var metrics));
        var item = Assert.Single(metrics);
        Assert.Equal(4, item.ApplicationCount);
        Assert.Equal(3, item.DaysUntilDeadline);
        Assert.Equal(1, item.Opportunity.Id);
        Assert.Same(deal, item.Opportunity.Deal);
    }

    [Fact]
    public async Task GetApplicationMetricsForCurrentVenueAsync_MissingVenue_ReturnsTypedError()
    {
        this.venueModule
            .Setup(module => module.GetVenueIdForCurrentTenantAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option.None<int>());

        var result = await this.service.GetApplicationMetricsForCurrentVenueAsync();

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<OpportunityError.MissingVenue>(error);
        this.repository.Verify(
            value => value.GetOpenWithApplicationCountsByVenueIdAsync(It.IsAny<int>()),
            Times.Never);
    }

    #endregion

    #region GetMatchesForCurrentArtistAsync

    [Fact]
    public async Task GetMatchesForCurrentArtistAsync_MatchingGenres_MapsFitScore()
    {
        IReadOnlySet<Genre> artistGenres = new HashSet<Genre> { Genre.Rock, Genre.Jazz };
        var projection = new OpportunityMatchProjection
        {
            Id = 2,
            VenueId = 43,
            VenueName = "Another Venue",
            County = "Lancashire",
            Town = "Preston",
            StartDate = new DateTime(2026, 9, 1, 20, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 9, 1, 23, 0, 0, DateTimeKind.Utc),
            Genres = [Genre.Rock, Genre.Pop],
            DealId = 8
        };
        var deal = new FlatFeeDeal { Id = 8, PaymentMethod = PaymentMethod.Cash, Fee = 600 };
        this.artistModule
            .Setup(module => module.GetIdForCurrentTenantAsync())
            .ReturnsAsync(Option.Some(9));
        this.artistModule
            .Setup(module => module.GetGenresAsync(9))
            .ReturnsAsync(artistGenres);
        this.publicRepository
            .Setup(value => value.GetMatchCandidatesAsync(9, artistGenres))
            .ReturnsAsync([projection]);
        this.dealModule
            .Setup(module => module.GetByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([deal]);

        var result = await this.service.GetMatchesForCurrentArtistAsync();

        Assert.True(result.TryGetValue(out var matches));
        var match = Assert.Single(matches);
        Assert.Equal(50, match.FitScore);
        Assert.Equal("Lancashire", match.County);
        Assert.Equal("Preston", match.Town);
        Assert.Same(deal, match.Opportunity.Deal);
    }

    [Fact]
    public async Task GetMatchesForCurrentArtistAsync_MissingArtist_ReturnsTypedError()
    {
        this.artistModule
            .Setup(module => module.GetIdForCurrentTenantAsync())
            .ReturnsAsync(Option.None<int>());

        var result = await this.service.GetMatchesForCurrentArtistAsync();

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<OpportunityError.MissingArtist>(error);
        this.publicRepository.Verify(
            value => value.GetMatchCandidatesAsync(It.IsAny<int>(), It.IsAny<IReadOnlySet<Genre>>()),
            Times.Never);
    }

    #endregion
}
