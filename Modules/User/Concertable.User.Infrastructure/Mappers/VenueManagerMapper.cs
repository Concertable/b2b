using Concertable.User.Infrastructure.Data;

namespace Concertable.User.Infrastructure.Mappers;

internal sealed class VenueManagerMapper : IRoleMapper
{
    private readonly UserDbContext context;

    public VenueManagerMapper(UserDbContext context)
    {
        this.context = context;
    }

    public Role Role => Role.VenueManager;

    public async Task<IUser> ToDtoAsync(UserEntity user)
    {
        var profile = await context.VenueManagerProfiles.FindAsync(user.Id);
        return new VenueManagerDto
        {
            Id = user.Id,
            Email = user.Email,
            Latitude = user.Location.ToLatitude(),
            Longitude = user.Location.ToLongitude(),
            County = user.Address?.County,
            Town = user.Address?.Town,
            VenueId = profile?.VenueId,
            IsEmailVerified = true,
        };
    }
}
