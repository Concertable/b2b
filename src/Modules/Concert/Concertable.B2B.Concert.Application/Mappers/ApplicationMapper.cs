using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Mappers;

internal sealed class ApplicationMapper : IApplicationMapper
{
    private readonly IOpportunityMapper opportunityMapper;
    private readonly IOpportunityRepository opportunities;
    private readonly IContractRepository contracts;

    public ApplicationMapper(
        IOpportunityMapper opportunityMapper,
        IOpportunityRepository opportunities,
        IContractRepository contracts)
    {
        this.opportunityMapper = opportunityMapper;
        this.opportunities = opportunities;
        this.contracts = contracts;
    }

    public async Task<ApplicationDto> ToDtoAsync(ApplicationEntity application)
    {
        var opportunity = await opportunities.GetByIdAsync(application.OpportunityId)
            ?? throw new InvalidOperationException($"Opportunity {application.OpportunityId} not found for application {application.Id}.");
        return new(application.Id,
            application.ToArtistSummary(),
            await opportunityMapper.ToDtoAsync(opportunity),
            application.State.ToStatus(),
            application.State,
            await contracts.GetIdByApplicationIdAsync(application.Id));
    }

    public async Task<IReadOnlyList<ApplicationDto>> ToDtosAsync(IEnumerable<ApplicationEntity> applications)
    {
        var applicationList = applications.ToList();
        var opportunityList = await opportunities.GetByIdsAsync(applicationList.Select(a => a.OpportunityId).Distinct().ToList());
        var opportunitiesById = opportunityList.ToDictionary(o => o.Id);
        var opportunityDtos = await opportunityMapper.ToDtosAsync(
            applicationList.Select(a => opportunitiesById[a.OpportunityId]));
        var dealIds = await contracts.GetContractIdsByApplicationIdsAsync(applicationList.Select(a => a.Id).ToList());

        return applicationList.Zip(opportunityDtos, (a, opp) =>
            new ApplicationDto(a.Id, a.ToArtistSummary(), opp, a.State.ToStatus(), a.State,
                dealIds.TryGetValue(a.Id, out var id) ? id : null)).ToList();
    }
}
