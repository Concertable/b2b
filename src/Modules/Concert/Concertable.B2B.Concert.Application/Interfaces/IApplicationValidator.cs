using Concertable.B2B.Concert.Domain.Entities;
using FluentResults;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IApplicationValidator
{
    Task<Result> CanApplyAsync(OpportunityEntity opportunity, int artistId);
    Task<Result> CanApplyAsync(int opportunityId);
    Task<Result> CanAcceptAsync(OpportunityEntity opportunity, ApplicationEntity application);
    Task<Result> CanAcceptAsync(int applicationId);
}
