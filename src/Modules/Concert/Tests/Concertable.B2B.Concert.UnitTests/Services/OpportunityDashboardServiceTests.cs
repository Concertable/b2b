using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Projections;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.Contracts.Enums;
using Concertable.Kernel.Identity;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Reunion;

namespace Concertable.B2B.Concert.UnitTests.Services;

public sealed class OpportunityDashboardServiceTests
{
    private readonly Mock<IOpportunityRepository> repository;
    private readonly Mock<IOpportunityReadRepository> readRepository;
    private readonly Mock<IArtistReadModelRepository> artistRepository;
    private readonly Mock<ITenantContext> tenantContext;
    private readonly Mock<IDealModule> dealModule;
    private readonly FakeTimeProvider timeProvider;
    private readonly OpportunityDashboardService service;
    private readonly Guid tenantId = Guid.NewGuid();

    public OpportunityDashboardServiceTests()
    {
        this.repository = new Mock<IOpportunityRepository>();
        this.readRepository = new Mock<IOpportunityReadRepository>();
        this.artistRepository = new Mock<IArtistReadModelRepository>();
        this.tenantContext = new Mock<ITenantContext>();
        this.dealModule = new Mock<IDealModule>();
        this.timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero));
        this.tenantContext.SetupGet(value => value.TenantId).Returns(this.tenantId);
        this.service = new OpportunityDashboardService(
            this.repository.Object,
            this.readRepository.Object,
            this.artistRepository.Object,
            this.tenantContext.Object,
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
        this.repository
            .Setup(value => value.GetOpenWithApplicationCountsByVenueTenantIdAsync(this.tenantId))
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
        this.artistRepository
            .Setup(value => value.GetByTenantIdAsync(this.tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtistReadModel
            {
                Id = 9,
                TenantId = this.tenantId,
                Genres = artistGenres.Select(genre => new ArtistReadModelGenre { Genre = genre }).ToList(),
            });
        this.readRepository
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
        this.artistRepository
            .Setup(value => value.GetByTenantIdAsync(this.tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArtistReadModel?)null);

        var result = await this.service.GetMatchesForCurrentArtistAsync();

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<OpportunityError.MissingArtist>(error);
        this.readRepository.Verify(
            value => value.GetMatchCandidatesAsync(It.IsAny<int>(), It.IsAny<IReadOnlySet<Genre>>()),
            Times.Never);
    }

    #endregion
}
