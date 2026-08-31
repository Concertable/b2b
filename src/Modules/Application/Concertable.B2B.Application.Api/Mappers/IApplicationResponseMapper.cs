using Concertable.B2B.Application.Api.Responses;
using Concertable.B2B.Application.Application.DTOs;

namespace Concertable.B2B.Application.Api.Mappers;

internal interface IApplicationMapper
{
    Task<ApplicationResponse<VenueApplicationActions>> ToVenueResponseAsync(ApplicationDto dto);
    Task<IReadOnlyList<ApplicationResponse<VenueApplicationActions>>> ToVenueResponsesAsync(IReadOnlyList<ApplicationDto> dtos);
    Task<ApplicationResponse<ArtistApplicationActions>> ToArtistResponseAsync(ApplicationDto dto);
    Task<IReadOnlyList<ApplicationResponse<ArtistApplicationActions>>> ToArtistResponsesAsync(IReadOnlyList<ApplicationDto> dtos);
}
