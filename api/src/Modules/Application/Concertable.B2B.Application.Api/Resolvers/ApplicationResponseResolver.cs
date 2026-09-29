using Concertable.B2B.Application.Api.Mappers;
using Concertable.B2B.Application.Api.Responses;
using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Booking.Contracts;

namespace Concertable.B2B.Application.Api.Resolvers;

internal sealed class ApplicationResponseResolver : IApplicationResponseResolver
{
    private readonly IBookingModule bookingModule;
    private readonly IMembershipContext membership;
    private readonly IPermissionCatalog permissionCatalog;

    public ApplicationResponseResolver(
        IBookingModule bookingModule,
        IMembershipContext membership,
        IPermissionCatalog permissionCatalog)
    {
        this.bookingModule = bookingModule;
        this.membership = membership;
        this.permissionCatalog = permissionCatalog;
    }

    public async Task<ApplicationSummaryResponse> ResolveSummaryAsync(
        ApplicationSummary dto,
        CancellationToken ct = default)
    {
        var bookingOption = await bookingModule.GetByApplicationIdAsync(dto.Id, ct);
        bookingOption.TryGetValue(out var booking);
        return dto.ToResponse(booking);
    }

    public async Task<IReadOnlyList<ApplicationSummaryResponse>> ResolveSummariesAsync(
        IReadOnlyList<ApplicationSummary> dtos,
        CancellationToken ct = default)
    {
        var bookings = await GetBookingsByApplicationIdAsync(dtos.Select(dto => dto.Id), ct);
        return dtos
            .Select(dto => dto.ToResponse(bookings.GetValueOrDefault(dto.Id)))
            .ToList();
    }

    public async Task<ApplicationProposalResponse> ResolveProposalAsync(
        ApplicationProposal dto,
        CancellationToken ct = default)
    {
        var bookingOption = await bookingModule.GetByApplicationIdAsync(dto.Id, ct);
        bookingOption.TryGetValue(out var booking);
        return dto.ToResponse(booking, membership.Membership, permissionCatalog);
    }

    public async Task<IReadOnlyList<ApplicationProposalResponse>> ResolveProposalsAsync(
        IReadOnlyList<ApplicationProposal> dtos,
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
