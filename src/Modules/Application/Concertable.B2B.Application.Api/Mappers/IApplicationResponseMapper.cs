using Concertable.B2B.Application.Api.Responses;
using Concertable.B2B.Application.Application.DTOs;

namespace Concertable.B2B.Application.Api.Mappers;

internal interface IApplicationResponseMapper
{
    Task<ApplicationResponse> ToResponseAsync(ApplicationDto dto);
    Task<IReadOnlyList<ApplicationResponse>> ToResponsesAsync(IEnumerable<ApplicationDto> dtos);
}
