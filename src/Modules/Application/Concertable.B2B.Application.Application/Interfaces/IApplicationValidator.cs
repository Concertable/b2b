using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Opportunity.Contracts;
using Reunion.Validation;

namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IApplicationValidator
{
    Task<ValidationResult> CanApplyAsync(OpportunityDto opportunity, int artistId);
    Task<ValidationResult> CanAcceptAsync(OpportunityDto opportunity, ApplicationEntity application);
}
