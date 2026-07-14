using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// Resolves who receives a concert's ticket revenue. The deal→payee rule lives behind this
/// interface (keyed strategy, see <c>PayeeResolver</c>); consumers never branch on deal type.
/// </summary>
internal interface IPayeeResolver
{
    Guid ResolveUserId(ConcertEntity concert);
    Guid ResolveTenantId(ConcertEntity concert);
}
