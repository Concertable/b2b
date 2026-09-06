using Concertable.B2B.Application.Api.Responses;
using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Booking.Contracts;

namespace Concertable.B2B.Application.Api.Mappers;

internal sealed class ApplicationMapper : IApplicationMapper
{
    private readonly IBookingModule bookingModule;

    public ApplicationMapper(IBookingModule bookingModule)
    {
        this.bookingModule = bookingModule;
    }

    public async Task<ApplicationResponse<VenueApplicationActions>> ToVenueResponseAsync(ApplicationDto dto)
    {
        var bookingOption = await bookingModule.GetByApplicationIdAsync(dto.Id);
        bookingOption.TryGetValue(out var booking);
        return dto.ToVenueResponse(booking);
    }

    public async Task<IReadOnlyList<ApplicationResponse<VenueApplicationActions>>> ToVenueResponsesAsync(
        IReadOnlyList<ApplicationDto> dtos)
    {
        var bookingsByApplicationId = (await bookingModule.GetByApplicationIdsAsync(
                dtos.Select(dto => dto.Id).ToArray()))
            .ToDictionary(booking => booking.ApplicationId);
        return dtos
            .Select(dto => dto.ToVenueResponse(bookingsByApplicationId.GetValueOrDefault(dto.Id)))
            .ToList();
    }

    public async Task<ApplicationResponse<ArtistApplicationActions>> ToArtistResponseAsync(ApplicationDto dto)
    {
        var bookingOption = await bookingModule.GetByApplicationIdAsync(dto.Id);
        bookingOption.TryGetValue(out var booking);
        return dto.ToArtistResponse(booking);
    }

    public async Task<IReadOnlyList<ApplicationResponse<ArtistApplicationActions>>> ToArtistResponsesAsync(
        IReadOnlyList<ApplicationDto> dtos)
    {
        var bookingsByApplicationId = (await bookingModule.GetByApplicationIdsAsync(
                dtos.Select(dto => dto.Id).ToArray()))
            .ToDictionary(booking => booking.ApplicationId);
        return dtos
            .Select(dto => dto.ToArtistResponse(bookingsByApplicationId.GetValueOrDefault(dto.Id)))
            .ToList();
    }
}
