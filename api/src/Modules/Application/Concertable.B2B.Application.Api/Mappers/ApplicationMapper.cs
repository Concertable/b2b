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

    public async Task<ApplicationSummaryResponse> ToSummaryResponseAsync(
        ApplicationSummaryDto dto,
        CancellationToken ct = default)
    {
        var bookingOption = await bookingModule.GetByApplicationIdAsync(dto.Id, ct);
        bookingOption.TryGetValue(out var booking);
        return dto.ToResponse(booking);
    }

    public async Task<IReadOnlyList<ApplicationSummaryResponse>> ToSummaryResponsesAsync(
        IReadOnlyList<ApplicationSummaryDto> dtos,
        CancellationToken ct = default)
    {
        var bookings = await GetBookingsByApplicationIdAsync(dtos.Select(dto => dto.Id), ct);
        return dtos
            .Select(dto => dto.ToResponse(bookings.GetValueOrDefault(dto.Id)))
            .ToList();
    }

    public async Task<ApplicationProposalResponse> ToProposalResponseAsync(
        ApplicationProposalDto dto,
        CancellationToken ct = default)
    {
        var bookingOption = await bookingModule.GetByApplicationIdAsync(dto.Id, ct);
        bookingOption.TryGetValue(out var booking);
        return dto.ToResponse(booking, membership.Membership, permissionCatalog);
    }

    public async Task<IReadOnlyList<ApplicationProposalResponse>> ToProposalResponsesAsync(
        IReadOnlyList<ApplicationProposalDto> dtos,
        CancellationToken ct = default)
    {
        var bookings = await GetBookingsByApplicationIdAsync(dtos.Select(dto => dto.Id), ct);
        return dtos
            .Select(dto => dto.ToResponse(
                bookings.GetValueOrDefault(dto.Id),
                membership.Membership,
                permissionCatalog))
            .ToList();
    }

    private async Task<IReadOnlyDictionary<int, BookingSummary>> GetBookingsByApplicationIdAsync(
        IEnumerable<int> applicationIds,
        CancellationToken ct) =>
        (await bookingModule.GetByApplicationIdsAsync(applicationIds.ToArray(), ct))
            .ToDictionary(booking => booking.ApplicationId);
}
