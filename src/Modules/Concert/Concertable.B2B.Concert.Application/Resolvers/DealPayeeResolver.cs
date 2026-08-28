using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Resolvers;

internal sealed class DealPayeeResolver : IDealPayeeResolver
{
    private readonly IDealTypeStrategyFactory<IDealPayeeResolver> payeeResolverFactory;

    public DealPayeeResolver(IDealTypeStrategyFactory<IDealPayeeResolver> payeeResolverFactory)
    {
        this.payeeResolverFactory = payeeResolverFactory;
    }

    public Guid ResolveTicketUserId(ConcertEntity concert) =>
        Resolve(concert).ResolveTicketUserId(concert);

    public Guid ResolveTicketTenantId(ConcertEntity concert) =>
        Resolve(concert).ResolveTicketTenantId(concert);

    public Guid ResolveSettlementTenantId(ConcertEntity concert) =>
        Resolve(concert).ResolveSettlementTenantId(concert);

    private IDealPayeeResolver Resolve(ConcertEntity concert) =>
        payeeResolverFactory.Create(concert.DealType);
}
