namespace Concertable.Concert.Application.Interfaces;

internal interface IApplyDispatcher
{
    Task<ApplicationEntity> ApplyAsync(int opportunityId, int artistId);
    Task<ApplicationEntity> ApplyAsync(int opportunityId, int artistId, string paymentMethodId);
}
