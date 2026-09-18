using Concertable.B2B.Application.Api.Responses;
using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Booking.Contracts;

namespace Concertable.B2B.Application.Api.Mappers;

internal sealed class ApplicationMapper : IApplicationMapper
{
    private readonly IBookingModule bookingModule;
    private readonly IMembershipContext membership;
    private readonly IPermissionCatalog permissionCatalog;

    public ApplicationMapper(
        IBookingModule bookingModule,
        IMembershipContext membership,
        IPermissionCatalog permissionCatalog)
    {
        this.bookingModule = bookingModule;
        this.membership = membership;
        this.permissionCatalog = permissionCatalog;
    }

    public async Task<ApplicationResponse> ToResponseAsync(ApplicationDto dto)
    {
        var bookingOption = await bookingModule.GetByApplicationIdAsync(dto.Id);
        bookingOption.TryGetValue(out var booking);
        return dto.ToResponse(booking, membership.Membership, permissionCatalog);
    }

    public async Task<IReadOnlyList<ApplicationResponse>> ToResponsesAsync(IReadOnlyList<ApplicationDto> dtos)
    {
        var bookingsByApplicationId = (await bookingModule.GetByApplicationIdsAsync(
                dtos.Select(dto => dto.Id).ToArray()))
            .ToDictionary(booking => booking.ApplicationId);
        return dtos
            .Select(dto => dto.ToResponse(
                bookingsByApplicationId.GetValueOrDefault(dto.Id),
                membership.Membership,
                permissionCatalog))
            .ToList();
    }
}
