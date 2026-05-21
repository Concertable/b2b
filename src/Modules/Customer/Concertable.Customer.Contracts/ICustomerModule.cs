using Concertable.Shared;
using Concertable.User.Contracts;

namespace Concertable.Customer.Contracts;

public interface ICustomerModule
{
    Task<CustomerDto?> GetCustomerAsync(Guid userId);

    Task<IReadOnlyCollection<Guid>> GetUserIdsByLocationAndGenresAsync(
        double latitude,
        double longitude,
        IEnumerable<Genre> genres);
}
