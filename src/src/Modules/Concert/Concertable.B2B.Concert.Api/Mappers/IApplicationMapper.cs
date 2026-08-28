using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Application.DTOs;

namespace Concertable.B2B.Concert.Api.Mappers;

internal interface IApplicationMapper
{
    ApplicationResponse<VenueApplicationActions> ToVenueResponse(ApplicationDto dto);
    IEnumerable<ApplicationResponse<VenueApplicationActions>> ToVenueResponses(IEnumerable<ApplicationDto> dtos);
    ApplicationResponse<ArtistApplicationActions> ToArtistResponse(ApplicationDto dto);
    IEnumerable<ApplicationResponse<ArtistApplicationActions>> ToArtistResponses(IEnumerable<ApplicationDto> dtos);
}
