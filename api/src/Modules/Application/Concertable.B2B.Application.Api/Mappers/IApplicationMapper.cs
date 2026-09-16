using Concertable.B2B.Application.Api.Responses;
using Concertable.B2B.Application.Application.DTOs;

namespace Concertable.B2B.Application.Api.Mappers;

internal interface IApplicationMapper
{
    Task<IReadOnlyList<ApplicationResponse<VenueApplicationActions>>> ToVenueResponsesAsync(IReadOnlyList<ApplicationDto> dtos);
    Task<ApplicationResponse<ArtistApplicationActions>> ToArtistResponseAsync(ApplicationDto dto);
    Task<IReadOnlyList<ApplicationResponse<ArtistApplicationActions>>> ToArtistResponsesAsync(IReadOnlyList<ApplicationDto> dtos);

    /// <summary>Maps an application to the response shape owed to the side the caller reads it from.</summary>
    Task<ApplicationResponse> ToResponseAsync(ApplicationDetailsDto details);
}
