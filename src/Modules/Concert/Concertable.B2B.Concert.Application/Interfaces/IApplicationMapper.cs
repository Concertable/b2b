using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IApplicationMapper
{
    Task<ApplicationDto> ToDtoAsync(ApplicationEntity application);
    Task<IEnumerable<ApplicationDto>> ToDtosAsync(IEnumerable<ApplicationEntity> applications);
}
