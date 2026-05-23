using Concertable.Kernel.Geometry;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Services.Geometry;
using Concertable.Shared.Geocoding.Application;
using Concertable.B2B.User.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.User.Infrastructure.Services;

internal class UserService : IUserService
{
    private readonly IUserRepository userRepsitory;
    private readonly ICurrentUser currentUser;
    private readonly IGeocodingService geocodingService;
    private readonly IGeometryProvider geometryProvider;
    private readonly IUserModule userModule;

    public UserService(
        IUserRepository userRepsitory,
        ICurrentUser currentUser,
        IGeocodingService geocodingService,
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider,
        IUserModule userModule)
    {
        this.userRepsitory = userRepsitory;
        this.currentUser = currentUser;
        this.geocodingService = geocodingService;
        this.geometryProvider = geometryProvider;
        this.userModule = userModule;
    }

    public async Task<IUser> SaveLocationAsync(double latitude, double longitude)
    {
        var user = await userRepsitory.GetByIdAsync(currentUser.GetId())
            ?? throw new UnauthorizedAccessException("User not found.");

        var locationDto = await geocodingService.GetLocationAsync(latitude, longitude);
        user.UpdateLocation(
            geometryProvider.CreatePoint(latitude, longitude),
            new Address(locationDto.County, locationDto.Town));

        userRepsitory.Update(user);
        await userRepsitory.SaveChangesAsync();

        return await userModule.GetByIdAsync(user.Id)
            ?? throw new UnauthorizedAccessException("User not found.");
    }
}
