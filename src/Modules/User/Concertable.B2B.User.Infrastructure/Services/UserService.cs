using Concertable.Kernel.Geometry;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Services.Geometry;
using Concertable.Shared.Geocoding.Application;
using Concertable.B2B.User.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.User.Infrastructure.Services;

internal sealed class UserService : IUserService
{
    private readonly IUserRepository userRepsitory;
    private readonly ICurrentUser currentUser;
    private readonly IGeocodingClient geocodingClient;
    private readonly IGeometryProvider geometryProvider;
    private readonly IUserModule userModule;

    public UserService(
        IUserRepository userRepsitory,
        ICurrentUser currentUser,
        IGeocodingClient geocodingClient,
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider,
        IUserModule userModule)
    {
        this.userRepsitory = userRepsitory;
        this.currentUser = currentUser;
        this.geocodingClient = geocodingClient;
        this.geometryProvider = geometryProvider;
        this.userModule = userModule;
    }

    public async Task<Result<UserBase, SaveLocationError>> SaveLocationAsync(double latitude, double longitude)
    {
        var user = await userRepsitory.GetByIdAsync(currentUser.GetId());
        if (user is null)
            return Result.Failure<UserBase, SaveLocationError>(new SaveLocationError());

        var address = await geocodingClient.GetLocationAsync(latitude, longitude);
        user.UpdateLocation(
            geometryProvider.CreatePoint(latitude, longitude),
            address);

        userRepsitory.Update(user);
        await userRepsitory.SaveChangesAsync();

        var updated = await userModule.GetByIdAsync(user.Id);
        return updated.Match(
            value => Result.Success<UserBase, SaveLocationError>(value),
            () => throw new InvalidOperationException($"User {user.Id} not found after location update."));
    }
}
