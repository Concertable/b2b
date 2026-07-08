using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;

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
            application.ToArtistSummary(),
            await opportunityMapper.ToDtoAsync(application.Opportunity),
            application.State.ToStatus(),
            application.State);

    public async Task<IEnumerable<ApplicationDto>> ToDtosAsync(IEnumerable<ApplicationEntity> applications)
    {
        var applicationList = applications.ToList();
        var opportunityDtos = await opportunityMapper.ToDtosAsync(applicationList.Select(a => a.Opportunity));

        return applicationList.Zip(opportunityDtos, (a, opp) =>
            new ApplicationDto(a.Id, a.ToArtistSummary(), opp, a.State.ToStatus(), a.State));
    }
}
