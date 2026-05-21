using Concertable.Customer.Application.Interfaces;
using Concertable.Customer.Contracts;
using Concertable.Shared;
using Concertable.User.Contracts;

namespace Concertable.Customer.Infrastructure;

internal class CustomerModule : ICustomerModule
{
    private readonly IPreferenceService preferenceService;
    private readonly IUserModule userModule;

    public CustomerModule(IPreferenceService preferenceService, IUserModule userModule)
    {
        this.preferenceService = preferenceService;
        this.userModule = userModule;
    }

    public async Task<CustomerDto?> GetCustomerAsync(Guid userId)
    {
        var user = await userModule.GetByIdAsync(userId);
        return user as CustomerDto;
    }

    public Task<IReadOnlyCollection<Guid>> GetUserIdsByLocationAndGenresAsync(
        double latitude,
        double longitude,
        IEnumerable<Genre> genres) =>
        preferenceService.GetUserIdsByLocationAndGenresAsync(latitude, longitude, genres);
}
