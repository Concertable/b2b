using Microsoft.EntityFrameworkCore;
using Concertable.DataAccess.Application;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Application.Errors;
using Concertable.B2B.Booking.Application.Mappers;
using Concertable.B2B.Booking.Application.Models;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class BookingService : IBookingService
{
    private readonly IBookingRepository bookingRepository;
    private readonly IBookingWorkflow workflow;
    private readonly TimeProvider timeProvider;
    private readonly IUnitOfWork unitOfWork;
    private readonly ITenantContext tenantContext;
    private readonly IAccessContext accessContext;

    public BookingService(
        IBookingRepository bookingRepository,
        IBookingWorkflow workflow,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        IAccessContext accessContext)
    {
        this.bookingRepository = bookingRepository;
        this.workflow = workflow;
        this.timeProvider = timeProvider;
        this.unitOfWork = unitOfWork;
        this.tenantContext = tenantContext;
        this.accessContext = accessContext;
    }

    public async Task<BookingDto?> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        (await bookingRepository.GetByApplicationIdAsync(applicationId, ct))?.ToDto();

    public Task<int?> GetIdByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        bookingRepository.GetIdByApplicationIdAsync(applicationId, ct);

    public async Task<BookingSummaryDto?> GetSummaryByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        var booking = await bookingRepository.GetByApplicationIdAsync(applicationId, ct);
        return booking is null
            ? null
            : new BookingSummaryDto(
                booking.Id,
                booking.ApplicationId,
                booking.State,
                booking.OperationId,
                booking.FinancialFailure?.Code,
                booking.FinancialFailure?.Message);
    }

    public async Task<IReadOnlyList<BookingSummaryDto>> GetSummariesByApplicationIdsAsync(
        IReadOnlyCollection<int> applicationIds,
        CancellationToken ct = default) =>
        (await bookingRepository.GetByApplicationIdsAsync(applicationIds, ct))
            .Select(booking => new BookingSummaryDto(
                booking.Id,
                booking.ApplicationId,
                booking.State,
                booking.OperationId,
                booking.FinancialFailure?.Code,
                booking.FinancialFailure?.Message))
            .ToList();

    public Task<int> GetArtistAwaitingCheckoutCountAsync(
        Guid artistTenantId,
        CancellationToken ct = default) =>
        bookingRepository.GetAwaitingCheckoutCountByArtistTenantIdAsync(
            artistTenantId,
            timeProvider.GetUtcNow().UtcDateTime,
            ct);

    public Task<UnitResult<CancelBookingError>> CancelAsync(
        int bookingId,
        CancellationToken ct = default) =>
        workflow.CancelAsync(bookingId, ct);

    public Task RecordSucceededAsync(
        int bookingId,
        FinancialOperationSucceeded operation,
        CancellationToken ct = default) =>
        workflow.RecordSucceededAsync(bookingId, operation, ct);

    public Task RecordFailedAsync(
        int bookingId,
        FinancialOperationFailed operation,
        CancellationToken ct = default) =>
        workflow.RecordFailedAsync(bookingId, operation, ct);

    public async Task<Result<BookingShareResponse, ShareBookingError>> ShareAsync(
        int bookingId,
        ShareBookingRequest request,
        CancellationToken ct = default)
    {
        if (tenantContext.TenantId is not { } actingTenantId)
            return new ShareBookingError.NoActiveTenant();

        var booking = await bookingRepository.GetWithGrantsByIdAsync(bookingId, ct);
        if (booking is null)
            return new ShareBookingError.BookingNotFound(bookingId);

        var share = booking.Share(
            request.ToTenantId,
            request.ToMemberUserId,
            request.Scope,
            actingTenantId,
            accessContext.UserId,
            timeProvider.GetUtcNow().UtcDateTime,
            request.ValidUntil)
            .MapError(static error => error.ToShareBookingError());

        if (share.TryGetError(out var shareError))
            return shareError;

        if (!await unitOfWork.TrySaveChangesAsync(
                static exception => exception is DbUpdateConcurrencyException))
            return new ShareBookingError.Superseded(bookingId);

        return share.Map(static grant => grant.ToShareResponse());
    }

    public async Task<UnitResult<RevokeBookingShareError>> RevokeShareAsync(
        int bookingId,
        Guid grantId,
        CancellationToken ct = default)
    {
        if (tenantContext.TenantId is not { } actingTenantId)
            return new RevokeBookingShareError.NoActiveTenant();

        var booking = await bookingRepository.GetWithGrantsByIdAsync(bookingId, ct);
        if (booking is null)
            return new RevokeBookingShareError.BookingNotFound(bookingId);

        if (booking.RevokeShare(grantId, actingTenantId, timeProvider.GetUtcNow().UtcDateTime)
            .TryGetError(out var revocationError))
            return revocationError.ToRevokeBookingShareError();

        return await unitOfWork.TrySaveChangesAsync(
                static exception => exception is DbUpdateConcurrencyException)
            ? new Success()
            : new RevokeBookingShareError.Superseded(bookingId);
    }
}
