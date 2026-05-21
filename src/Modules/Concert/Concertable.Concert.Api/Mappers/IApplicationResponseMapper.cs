using Concertable.Concert.Api.Responses;
using Concertable.Concert.Application.DTOs;

namespace Concertable.Concert.Api.Mappers;

internal interface IApplicationResponseMapper
{
    ApplicationResponse ToResponse(ApplicationDto dto);
    IEnumerable<ApplicationResponse> ToResponses(IEnumerable<ApplicationDto> dtos);
}
