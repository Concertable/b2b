using Concertable.B2B.Concert.Domain.Entities;
using Reunion.Validation;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IApplicationValidator
{
    Task<ValidationResult> CanApplyAsync(OpportunityEntity opportunity, int artistId);
    Task<ValidationResult> CanAcceptAsync(OpportunityEntity opportunity, ApplicationEntity application);
}
