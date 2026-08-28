using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Domain.Entities;

namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IApplicationMapper
{
    Task<ApplicationDto> ToDtoAsync(ApplicationEntity application);
    Task<IReadOnlyList<ApplicationDto>> ToDtosAsync(IEnumerable<ApplicationEntity> applications);
}
