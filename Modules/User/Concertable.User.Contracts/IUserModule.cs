namespace Concertable.User.Contracts;

public interface IUserModule
{
    Task<IUser?> GetByIdAsync(Guid id);
    Task<IReadOnlyCollection<IUser>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task<ManagerDto?> GetManagerByIdAsync(Guid userId);
}
