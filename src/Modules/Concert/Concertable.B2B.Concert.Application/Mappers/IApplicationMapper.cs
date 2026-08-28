using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Mappers;

internal interface IApplicationMapper
{
    Task<ApplicationDto> ToDtoAsync(ApplicationEntity application);
    Task<IReadOnlyList<ApplicationDto>> ToDtosAsync(IEnumerable<ApplicationEntity> applications);
}
