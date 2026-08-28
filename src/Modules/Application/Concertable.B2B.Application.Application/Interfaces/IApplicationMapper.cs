using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Domain.Entities;

<<<<<<<< HEAD:api/Concertable.B2B/src/Modules/Application/Concertable.B2B.Application.Application/Interfaces/IApplicationMapper.cs
namespace Concertable.B2B.Application.Application.Interfaces;
========
namespace Concertable.B2B.Concert.Application.Mappers;
>>>>>>>> origin/main:api/Concertable.B2B/src/Modules/Concert/Concertable.B2B.Concert.Application/Mappers/IApplicationMapper.cs

internal interface IApplicationMapper
{
    Task<ApplicationDto> ToDtoAsync(ApplicationEntity application);
    Task<IReadOnlyList<ApplicationDto>> ToDtosAsync(IEnumerable<ApplicationEntity> applications);
}
