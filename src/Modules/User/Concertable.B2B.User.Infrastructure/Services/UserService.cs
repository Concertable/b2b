using Concertable.Kernel.Geometry;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Services.Geometry;
using Concertable.Shared.Geocoding.Application;
using Concertable.B2B.User.Contracts;
using Concertable.B2B.User.Infrastructure.Mappers;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.User.Infrastructure.Services;

internal sealed class UserService : IUserService
{
    private readonly IUserRepository userRepository;
    private readonly IUserMapper userMapper;
    private readonly ICurrentUser currentUser;
    private readonly IGeocodingClient geocodingClient;
    private readonly IGeometryProvider geometryProvider;

    public UserService(
        IUserRepository userRepository,
        IUserMapper userMapper,
        ICurrentUser currentUser,
        IGeocodingClient geocodingClient,
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider)
    {
        this.userRepository = userRepository;
        this.userMapper = userMapper;
        this.currentUser = currentUser;
        this.geocodingClient = geocodingClient;
        this.geometryProvider = geometryProvider;
    }

    public async Task<Result<UserDto, SaveLocationError>> SaveLocationAsync(double latitude, double longitude)
    {
        var user = await userRepository.GetByIdAsync(currentUser.GetId());
        if (user is null)
            return new SaveLocationError.UserNotFound();

        var address = await geocodingClient.GetLocationAsync(latitude, longitude);
        user.UpdateLocation(
            geometryProvider.CreatePoint(latitude, longitude),
            address);

        userRepository.Update(user);
        await userRepository.SaveChangesAsync();

        var updated = await userMapper.ToDtoAsync(user)
            ?? throw new InvalidOperationException($"User {user.Id} could not be mapped after location update.");

        return updated;
    }

    public async Task<Option<UserDto>> GetByIdAsync(Guid id)
    {
        var user = await userRepository.GetByIdAsync(id);
        if (user is null)
            return null;

        return await userMapper.ToDtoAsync(user);
    }

    public async Task<IReadOnlyList<UserDto>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var users = await userRepository.GetByIdsAsync(ids);
        var result = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            var dto = await userMapper.ToDtoAsync(user);
            if (dto is not null)
                result.Add(dto);
        }

        return result;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetEmailsByIdsAsync(IEnumerable<Guid> ids)
    {
        var users = await userRepository.GetByIdsAsync(ids);
        return users.ToDictionary(user => user.Id, user => user.Email);
    }

    public async Task<Option<ManagerDto>> GetManagerByIdAsync(Guid userId)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            return null;

        return new ManagerDto { Id = user.Id, Email = user.Email, Avatar = user.Avatar };
    }

    public async Task<Option<Guid>> GetIdByEmailAsync(string email) =>
        (await userRepository.GetIdByEmailAsync(email)).ToOption();
}
