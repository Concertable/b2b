using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Application.DTOs;

namespace Concertable.B2B.Concert.Api.Mappers;

internal interface IApplicationResponseMapper
{
    ApplicationResponse ToResponse(ApplicationDto dto);
    IEnumerable<ApplicationResponse> ToResponses(IEnumerable<ApplicationDto> dtos);
}
