using Concertable.Shared;

namespace Concertable.Customer.Contracts;

public interface ICustomerModule
{
    Task<IReadOnlyCollection<Guid>> GetUserIdsByLocationAndGenresAsync(
        double latitude,
        double longitude,
        IEnumerable<Genre> genres);
}
