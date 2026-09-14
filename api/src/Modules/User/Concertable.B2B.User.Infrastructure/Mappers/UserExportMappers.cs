using Concertable.B2B.User.Contracts;
using Concertable.B2B.User.Domain.Entities;

namespace Concertable.B2B.User.Infrastructure.Mappers;

internal static class UserExportMappers
{
    extension(UserEntity user)
    {
        public UserExport ToUserExport() => new()
        {
            Email = user.Email,
            Latitude = user.Location?.Y,
            Longitude = user.Location?.X,
            County = user.Address?.County,
            Town = user.Address?.Town,
            Avatar = user.Avatar,
        };
    }
}
