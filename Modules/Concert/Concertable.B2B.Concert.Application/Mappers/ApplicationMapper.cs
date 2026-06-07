using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Application.Mappers;

internal sealed class ApplicationMapper : IApplicationMapper
{
    private readonly IOpportunityMapper opportunityMapper;

    public ApplicationMapper(IOpportunityMapper opportunityMapper)
    {
        this.opportunityMapper = opportunityMapper;
    }

    public async Task<ApplicationDto> ToDtoAsync(ApplicationEntity application) =>
        new(application.Id,
            BuildArtistSummary(application),
            await opportunityMapper.ToDtoAsync(application.Opportunity),
            ToStatus(application.State));

    public async Task<IEnumerable<ApplicationDto>> ToDtosAsync(IEnumerable<ApplicationEntity> applications)
    {
        var applicationList = applications.ToList();
        var opportunityDtos = await opportunityMapper.ToDtosAsync(applicationList.Select(a => a.Opportunity));

        return applicationList.Zip(opportunityDtos, (a, opp) =>
            new ApplicationDto(a.Id, BuildArtistSummary(a), opp, ToStatus(a.State)));
    }

    private static ApplicationStatus ToStatus(LifecycleState state) => state switch
    {
        LifecycleState.Applied => ApplicationStatus.Pending,
        LifecycleState.Rejected => ApplicationStatus.Rejected,
        LifecycleState.Withdrawn => ApplicationStatus.Withdrawn,
        _ => ApplicationStatus.Accepted
    };

    private static ArtistSummary BuildArtistSummary(ApplicationEntity application) => new()
    {
        Id = application.Artist.Id,
        Name = application.Artist.Name,
        Avatar = application.Artist.Avatar,
        Genres = application.Artist.Genres.Select(g => g.Genre)
    };
}
