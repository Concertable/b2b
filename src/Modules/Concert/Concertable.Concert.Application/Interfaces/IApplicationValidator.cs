using FluentResults;

namespace Concertable.Concert.Application.Interfaces;

internal interface IApplicationValidator
{
    Task<Result> CanApplyAsync(OpportunityEntity opportunity, int artistId);
    Task<Result> CanApplyAsync(int opportunityId, int artistId);
    Task<Result> CanAcceptAsync(OpportunityEntity opportunity, ApplicationEntity application);
    Task<Result> CanAcceptAsync(int applicationId);
}
