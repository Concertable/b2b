using Concertable.Customer.Application.Interfaces;
using Concertable.Customer.Contracts;
using Concertable.Shared;

namespace Concertable.Customer.Infrastructure;

internal class CustomerModule : ICustomerModule
{
    private readonly IPreferenceService preferenceService;

    public CustomerModule(IPreferenceService preferenceService)
    {
        this.preferenceService = preferenceService;
    }

    public Task<IReadOnlyCollection<Guid>> GetUserIdsByLocationAndGenresAsync(
        double latitude,
        double longitude,
        IEnumerable<Genre> genres) =>
        preferenceService.GetUserIdsByLocationAndGenresAsync(latitude, longitude, genres);
}
