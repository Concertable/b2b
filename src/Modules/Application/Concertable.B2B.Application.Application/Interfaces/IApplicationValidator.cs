using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Opportunity.Contracts;
using Reunion.Validation;

namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IApplicationValidator
{
    Task<ValidationResult> CanApplyAsync(OpportunityDetails opportunity, int artistId);
    Task<ValidationResult> CanAcceptAsync(OpportunityDetails opportunity, ApplicationEntity application);
}
