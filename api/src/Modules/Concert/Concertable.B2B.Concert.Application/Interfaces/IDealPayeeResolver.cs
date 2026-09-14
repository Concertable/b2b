using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IDealPayeeResolver : IDealStrategy
{
    Guid ResolveTicketUserId(ConcertEntity concert);
    Guid ResolveTicketTenantId(ConcertEntity concert);
    Guid ResolveSettlementTenantId(ConcertEntity concert);
}
