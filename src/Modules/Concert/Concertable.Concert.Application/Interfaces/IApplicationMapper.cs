using Concertable.Concert.Application.DTOs;

namespace Concertable.Concert.Application.Interfaces;

internal interface IApplicationMapper
{
    Task<ApplicationDto> ToDtoAsync(ApplicationEntity application);
    Task<IEnumerable<ApplicationDto>> ToDtosAsync(IEnumerable<ApplicationEntity> applications);
}
