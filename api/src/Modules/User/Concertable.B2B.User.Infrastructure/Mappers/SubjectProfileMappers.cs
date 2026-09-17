using Concertable.B2B.User.Contracts;
using Concertable.B2B.User.Domain.Entities;

namespace Concertable.B2B.User.Infrastructure.Mappers;

internal static class SubjectProfileMappers
{
    extension(UserEntity user)
    {
        public SubjectProfileDto ToSubjectProfileDto() => new()
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
