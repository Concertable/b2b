using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IApplyDispatcher
{
    Task<ApplicationEntity> ApplyAsync(int opportunityId, int artistId);
    Task<ApplicationEntity> ApplyAsync(int opportunityId, int artistId, string paymentMethodId);
}
