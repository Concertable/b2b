using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.B2B.Opportunity.Application.DTOs;
using Concertable.B2B.Opportunity.Application.Errors;
using Concertable.B2B.Opportunity.Application.Interfaces;
using Concertable.B2B.Opportunity.Domain.Entities;
using Concertable.B2B.Opportunity.Infrastructure.Services;
using Concertable.B2B.Venue.Contracts;
using Concertable.Contracts;
using Concertable.Contracts.Enums;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Reunion;

namespace Concertable.B2B.Opportunity.UnitTests.Services;

public sealed class OpportunityDashboardServiceTests
{
    private readonly Mock<IOpportunityRepository> repository = new();
    private readonly Mock<IOpportunityReadRepository> readRepository = new();
    private readonly Mock<IApplicationDashboardMetricsProvider> applicationMetrics = new();
    private readonly Mock<IArtistModule> artists = new();
    private readonly Mock<IVenueModule> venues = new();
    private readonly Mock<IOpportunityMapper> mapper = new();
    private readonly Mock<ITenantContext> tenantContext = new();
    private readonly FakeTimeProvider timeProvider = new(
        new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero));
    private readonly Guid tenantId = Guid.NewGuid();

    [Fact]
    public async Task GetApplicationMetricsForCurrentVenueAsync_MapsCountsAndDeadline()
    {
        var entity = OpportunityEntity.Create(
            42,
            new DateRange(new DateTime(2026, 8, 26, 20, 0, 0), new DateTime(2026, 8, 26, 23, 0, 0)),
            7,
            [Genre.Rock]);
        var dto = CreateDto(1, 42, [Genre.Rock]);
        tenantContext.SetupGet(context => context.TenantId).Returns(tenantId);
        repository.Setup(value => value.GetOpenByVenueTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([entity]);
        applicationMetrics.Setup(value => value.GetCountsByOpportunityIdsAsync(
                It.IsAny<IReadOnlyCollection<int>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int> { [1] = 4 });
        mapper.Setup(value => value.ToDtosAsync(It.IsAny<IEnumerable<OpportunityEntity>>()))
            .ReturnsAsync([dto]);

        var result = await CreateService().GetApplicationMetricsForCurrentVenueAsync();

        Assert.True(result.TryGetValue(out var metrics));
        var item = Assert.Single(metrics);
        Assert.Equal(4, item.ApplicationCount);
        Assert.Equal(3, item.DaysUntilDeadline);
        Assert.Same(dto, item.Opportunity);
    }

    [Fact]
    public async Task GetMatchesForCurrentArtistAsync_MapsFitAndVenueLocation()
    {
        var artist = new ArtistProfile(
            9,
            tenantId,
            Guid.NewGuid(),
            "The Artist",
            "About",
            "artist@example.com",
            new HashSet<Genre> { Genre.Rock, Genre.Jazz });
        var entity = OpportunityEntity.Create(
            43,
            new DateRange(new DateTime(2026, 9, 1, 20, 0, 0), new DateTime(2026, 9, 1, 23, 0, 0)),
            8,
            [Genre.Rock, Genre.Pop]);
        var dto = CreateDto(2, 43, [Genre.Rock, Genre.Pop]);
        artists.Setup(value => value.GetCurrentProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option.Some(artist));
        applicationMetrics.Setup(value => value.GetOpportunityIdsForArtistTenantAsync(
                tenantId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());
        readRepository.Setup(value => value.GetMatchCandidatesAsync(
                It.IsAny<IReadOnlyCollection<int>>(),
                artist.Genres,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([entity]);
        mapper.Setup(value => value.ToDtosAsync(It.IsAny<IEnumerable<OpportunityEntity>>()))
            .ReturnsAsync([dto]);
        venues.Setup(value => value.GetProfileAsync(43, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option.Some(new VenueProfile(
                43,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "The Venue",
                "About",
                "venue@example.com",
                "Lancashire",
                "Preston")));

        var result = await CreateService().GetMatchesForCurrentArtistAsync();

        Assert.True(result.TryGetValue(out var matches));
        var match = Assert.Single(matches);
        Assert.Equal(50, match.FitScore);
        Assert.Equal("Lancashire", match.County);
        Assert.Equal("Preston", match.Town);
        Assert.Same(dto, match.Opportunity);
    }

    [Fact]
    public async Task GetMatchesForCurrentArtistAsync_MissingArtist_ReturnsTypedError()
    {
        artists.Setup(value => value.GetCurrentProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option.None<ArtistProfile>());

        var result = await CreateService().GetMatchesForCurrentArtistAsync();

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<OpportunityError.MissingArtist>(error);
        readRepository.Verify(value => value.GetMatchCandidatesAsync(
                It.IsAny<IReadOnlyCollection<int>>(),
                It.IsAny<IReadOnlySet<Genre>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private OpportunityDashboardService CreateService() => new(
        repository.Object,
        readRepository.Object,
        applicationMetrics.Object,
        artists.Object,
        venues.Object,
        mapper.Object,
        tenantContext.Object,
        timeProvider);

    private static OpportunityDto CreateDto(int id, int venueId, IReadOnlyList<Genre> genres) => new()
    {
        Id = id,
        VenueId = venueId,
        VenueName = "The Venue",
        DealId = 7,
        Deal = new FlatFeeDealDto { Id = 7, PaymentMethod = PaymentMethod.Cash, Fee = 500 },
        StartDate = new DateTime(2026, 8, 26, 20, 0, 0),
        EndDate = new DateTime(2026, 8, 26, 23, 0, 0),
        Genres = genres
    };
}
