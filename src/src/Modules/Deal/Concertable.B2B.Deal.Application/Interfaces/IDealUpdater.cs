using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Domain.Entities;

namespace Concertable.B2B.Deal.Application.Interfaces;

internal interface IDealUpdater
{
    void Apply(DealEntity existing, IDeal source);
}
