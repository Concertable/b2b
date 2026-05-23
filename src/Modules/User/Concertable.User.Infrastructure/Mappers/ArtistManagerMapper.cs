using Concertable.Kernel;
using Concertable.User.Infrastructure.Data;

namespace Concertable.User.Infrastructure.Mappers;

internal sealed class ArtistManagerMapper : IRoleMapper
{
    private readonly UserDbContext context;

    public ArtistManagerMapper(UserDbContext context)
    {
        this.context = context;
    }

    public Role Role => Role.ArtistManager;

    public async Task<IUser> ToDtoAsync(UserEntity user)
    {
        var profile = await context.ArtistManagerProfiles.FindAsync(user.Id);
        return new ArtistManagerDto
        {
            Id = user.Id,
            Email = user.Email,
            Latitude = user.Location.ToLatitude(),
            Longitude = user.Location.ToLongitude(),
            County = user.Address?.County,
            Town = user.Address?.Town,
            ArtistId = profile?.ArtistId,
            IsEmailVerified = true,
        };
    }
}
