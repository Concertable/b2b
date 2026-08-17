using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Resolvers;

internal sealed class DealPayeeResolver : IDealPayeeResolver
{
    private readonly IConcertDealStrategyFactory<IDealPayeeResolver> resolvers;

    public DealPayeeResolver(IConcertDealStrategyFactory<IDealPayeeResolver> resolvers)
    {
        this.resolvers = resolvers;
    }

    public Guid ResolveTicketUserId(ConcertEntity concert) =>
        Resolve(concert).ResolveTicketUserId(concert);

    public Guid ResolveTicketTenantId(ConcertEntity concert) =>
        Resolve(concert).ResolveTicketTenantId(concert);

    public Guid ResolveSettlementTenantId(ConcertEntity concert) =>
        Resolve(concert).ResolveSettlementTenantId(concert);

    private IDealPayeeResolver Resolve(ConcertEntity concert) =>
        resolvers.Create(concert.DealType);
}
