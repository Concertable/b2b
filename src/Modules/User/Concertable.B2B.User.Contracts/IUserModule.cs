namespace Concertable.B2B.User.Contracts;

public interface IUserModule
{
    Task<UserBase?> GetByIdAsync(Guid id);
    Task<IReadOnlyCollection<UserBase>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task<ManagerDto?> GetManagerByIdAsync(Guid userId);
}
