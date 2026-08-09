using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Domain.Entities;
using Reunion.Errors;
using Reunion;

namespace Concertable.B2B.Deal.Application.Interfaces;

internal interface IDealUpdater
{
    UnitResult<ValidationErrors> Apply(DealEntity existing, IDeal source);
}
