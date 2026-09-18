using Concertable.B2B.Application.Api.Responses;
using Concertable.B2B.Application.Application.DTOs;

namespace Concertable.B2B.Application.Api.Mappers;

internal interface IApplicationMapper
{
    Task<ApplicationResponse> ToResponseAsync(ApplicationDto dto);
    Task<IReadOnlyList<ApplicationResponse>> ToResponsesAsync(IReadOnlyList<ApplicationDto> dtos);
}
