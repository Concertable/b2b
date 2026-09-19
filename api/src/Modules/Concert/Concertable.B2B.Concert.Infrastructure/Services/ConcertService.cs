using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Concert.Application.Mappers;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Contracts.Commands;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.B2B.Concert.Infrastructure.Specifications;
using Concertable.B2B.Tenant.Contracts;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertService : IConcertService
{
    private readonly IConcertRepository concertRepository;
    private readonly IConcertPrivilegedRepository privilegedRepository;
    private readonly IPrivilegedOutboxUnitOfWorkBehavior privilegedOutboxUnitOfWorkBehavior;
    private readonly IConcertReadRepository readRepository;
    private readonly IConcertValidator concertValidator;
    private readonly IConcertWorkflow workflow;
    private readonly IArtistReadModelRepository artistReadModelRepository;
    private readonly IVenueReadModelRepository venueReadModelRepository;
    private readonly IBookingConfirmationEmailSender bookingConfirmationEmailSender;
    private readonly IBus bus;
    private readonly IUnitOfWork unitOfWork;
    private readonly TimeProvider timeProvider;
    private readonly IConcertCommandReceiptRepository receiptRepository;
    private readonly ITenantCommandFacts tenantCommandFacts;
    private readonly ITenantContext tenantContext;
    private readonly IMembershipContext membership;
    private readonly IMembershipAuthorityFence authorityFence;
    private readonly IPermissionCatalog permissionCatalog;
    private readonly ICommandExecutor commandExecutor;
    private readonly IResourceAccessContext resourceAccess;
    private readonly ILogger<ConcertService> logger;

    public ConcertService(
        IConcertRepository concertRepository,
        IConcertPrivilegedRepository privilegedRepository,
        IPrivilegedOutboxUnitOfWorkBehavior privilegedOutboxUnitOfWorkBehavior,
        IConcertReadRepository readRepository,
        IConcertValidator concertValidator,
        IConcertWorkflow workflow,
        IArtistReadModelRepository artistReadModelRepository,
        IVenueReadModelRepository venueReadModelRepository,
        IBookingConfirmationEmailSender bookingConfirmationEmailSender,
        IBus bus,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IConcertCommandReceiptRepository receiptRepository,
        ITenantCommandFacts tenantCommandFacts,
        ITenantContext tenantContext,
        IMembershipContext membership,
        IMembershipAuthorityFence authorityFence,
        IPermissionCatalog permissionCatalog,
        ICommandExecutor commandExecutor,
        IResourceAccessContext resourceAccess,
        ILogger<ConcertService> logger)
    {
        this.concertRepository = concertRepository;
        this.privilegedRepository = privilegedRepository;
        this.privilegedOutboxUnitOfWorkBehavior = privilegedOutboxUnitOfWorkBehavior;
        this.readRepository = readRepository;
        this.concertValidator = concertValidator;
        this.workflow = workflow;
        this.artistReadModelRepository = artistReadModelRepository;
        this.venueReadModelRepository = venueReadModelRepository;
        this.bookingConfirmationEmailSender = bookingConfirmationEmailSender;
        this.bus = bus;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
        this.receiptRepository = receiptRepository;
        this.tenantCommandFacts = tenantCommandFacts;
        this.tenantContext = tenantContext;
        this.membership = membership;
        this.authorityFence = authorityFence;
        this.permissionCatalog = permissionCatalog;
        this.commandExecutor = commandExecutor;
        this.resourceAccess = resourceAccess;
        this.logger = logger;
    }

    public Task CreateAsync(ConfirmedBookingSnapshot booking, CancellationToken ct = default) =>
        privilegedOutboxUnitOfWorkBehavior.ExecuteAsync(() => CreateCoreAsync(booking, ct), ct);

    private async Task CreateCoreAsync(ConfirmedBookingSnapshot booking, CancellationToken ct)
    {
        logger.CreatingConcertDraft(booking.BookingId);

        if (await privilegedRepository.GetByBookingIdAsync(booking.BookingId, ct) is not null)
            return;

        var artist = await artistReadModelRepository.GetByTenantIdAsync(booking.ArtistTenantId, ct)
            ?? throw new InvalidOperationException(
                $"Artist projection {booking.ArtistTenantId} was not found for booking {booking.BookingId}.");
        var venue = await venueReadModelRepository.GetByTenantIdAsync(booking.VenueTenantId, ct)
            ?? throw new InvalidOperationException(
                $"Venue projection {booking.VenueTenantId} was not found for booking {booking.BookingId}.");
        if (artist.Id != booking.ArtistId || venue.Id != booking.VenueId)
            throw new InvalidOperationException(
                $"Booking {booking.BookingId} does not match its artist or venue projection.");

        var artistGenres = artist.Genres.Select(genre => genre.Genre).ToList();
        var matchingGenres = booking.Genres.Count > 0
            ? artistGenres.Intersect(booking.Genres).ToList()
            : artistGenres;
        if (matchingGenres.Count == 0)
        {
            logger.ConcertDraftCreationFailed(booking.BookingId, artist.Id, booking.OpportunityId);
            throw new InvalidOperationException(
                $"Artist {artist.Id} does not match the genres for booking {booking.BookingId}.");
        }

        var concert = ConcertEntity.CreateDraft(
            booking,
            new ConcertDraft(
                $"{artist.Name} performing at {venue.Name}",
                venue.About,
                matchingGenres),
            timeProvider.GetUtcNow().UtcDateTime);
        await privilegedRepository.AddAsync(concert, ct);
        await privilegedRepository.SaveChangesAsync(ct);

        await bus.PublishAsync(new ConcertCreatedEvent(
            concert.Id,
            concert.ApplicationId,
            concert.OpportunityId,
            concert.ArtistId,
            concert.VenueId,
            concert.VenueTenantId,
            concert.ArtistTenantId,
            concert.Period.Start), ct);

        logger.ConcertDraftCreated(concert.Id, booking.BookingId, artist.Id, venue.Id);
        await bus.SendAsync(new NotifyConcertDraftCreatedCommand(
            concert.Id,
            artist.UserId,
            venue.UserId), ct);
        await bookingConfirmationEmailSender.SendAsync(booking, venue.Name, artist.Name, ct);
    }

    public Task<IReadOnlyList<PublishedConcert>> GetUpcomingByVenueIdAsync(
        int id,
        CancellationToken ct = default) =>
        readRepository.GetUpcomingByVenueIdAsync(id, ct);

    public Task<IReadOnlyList<PublishedConcert>> GetUpcomingByArtistIdAsync(
        int id,
        CancellationToken ct = default) =>
        readRepository.GetUpcomingByArtistIdAsync(id, ct);

    public async Task<Result<IReadOnlyList<ManagerConcertCard>, ConcertError>> GetUpcomingForCurrentVenueAsync()
    {
        if (tenantContext.TenantId is not { } tenantId)
            return new ConcertError.MissingVenue();

        return new Success<IReadOnlyList<ManagerConcertCard>>(
            await concertRepository.GetUpcomingCardsForVenueTenantIdAsync(tenantId));
    }

    public async Task<Result<IReadOnlyList<ConcertDraftReference>, ConcertError>> GetDraftsForCurrentVenueAsync(
        CancellationToken ct = default)
    {
        if (tenantContext.TenantId is not { } tenantId)
            return new ConcertError.MissingVenue();

        return new Success<IReadOnlyList<ConcertDraftReference>>(
            await concertRepository.GetDraftReferencesForVenueTenantIdAsync(tenantId, ct));
    }

    public async Task<Result<IReadOnlyList<ManagerConcertCard>, ConcertError>> GetUpcomingForCurrentArtistAsync()
    {
        if (tenantContext.TenantId is not { } tenantId)
            return new ConcertError.MissingArtist();

        return new Success<IReadOnlyList<ManagerConcertCard>>(
            await concertRepository.GetUpcomingCardsForArtistTenantIdAsync(tenantId));
    }

    public Task<IReadOnlyList<PublishedConcert>> GetHistoryByArtistIdAsync(
        int id,
        CancellationToken ct = default) =>
        readRepository.GetHistoryByArtistIdAsync(id, ct);

    public Task<IReadOnlyList<PublishedConcert>> GetHistoryByVenueIdAsync(
        int id,
        CancellationToken ct = default) =>
        readRepository.GetHistoryByVenueIdAsync(id, ct);

    public Task<Result<PublishedConcert, ConcertError>> GetPublishedAsync(
        int id,
        CancellationToken ct = default) =>
        readRepository.GetPublishedByIdAsync(id, ct)
            .ToOption()
            .OrFailure(() => (ConcertError)new ConcertError.NotFound(id));

    public Task<Result<ConcertSummary, ConcertError>> GetSummaryAsync(
        int id,
        CancellationToken ct = default) =>
        concertRepository.GetSummaryByIdAsync(id, ct)
            .ToOption()
            .OrFailure(() => (ConcertError)new ConcertError.NotFound(id));

    public async Task<Result<ConcertOperations, ConcertError>> GetOperationsAsync(
        int id,
        CancellationToken ct = default)
    {
        return await concertRepository.GetOperationsByIdAsync(id, ct)
            .ToOption()
            .OrFailure(() => (ConcertError)new ConcertError.NotFound(id))
            .MapAsync(operations => WithOperationsActionsAsync(operations, ct));
    }

    public async Task<Result<ConcertFinance, ConcertError>> GetFinanceAsync(
        int id,
        CancellationToken ct = default)
    {
        return await concertRepository.GetFinanceByIdAsync(id, ct)
            .ToOption()
            .OrFailure(() => (ConcertError)new ConcertError.NotFound(id))
            .MapAsync(finance => WithFinanceActionsAsync(finance, ct));
    }

    public async Task<Result<ConcertUpdateResponse, UpdateConcertError>> UpdateAsync(
        int id,
        UpdateConcertRequest request,
        CancellationToken ct = default)
    {
        if (membership.Membership is not { } actor)
            return new UpdateConcertError.NotPermitted();

        try
        {
            return await commandExecutor.ExecuteAsync<ConcertService, Result<ConcertUpdateResponse, UpdateConcertError>>(
                (service, token) => service.UpdateCommandAsync(id, request, actor, token),
                (service, _, token) => service.ValidateOperationsAuthorityAsync(
                    id, actor, TenantPermission.ConcertsOpsEdit, token),
                () => new UpdateConcertError.NotPermitted(),
                ct);
        }
        catch (DbUpdateException exception) when (exception.IsConcertConcurrencyConflict(id))
        {
            return new UpdateConcertError.Superseded(id);
        }
    }

    public async Task<UnitResult<PostConcertError>> PostAsync(
        int id,
        UpdateConcertRequest request,
        CancellationToken ct = default)
    {
        if (membership.Membership is not { } actor)
            return new PostConcertError.NotPermitted();

        try
        {
            return await commandExecutor.ExecuteAsync<ConcertService, UnitResult<PostConcertError>>(
                (service, token) => service.PostCommandAsync(id, request, actor, token),
                (service, _, token) => service.ValidateOperationsAuthorityAsync(
                    id, actor, TenantPermission.ConcertsOpsEdit, token),
                () => new PostConcertError.NotPermitted(),
                ct);
        }
        catch (DbUpdateException exception) when (exception.IsConcertConcurrencyConflict(id))
        {
            return await commandExecutor.ExecuteAsync<ConcertService, UnitResult<PostConcertError>>(
                (service, token) => service.ClassifyPostConflictAsync(id, token),
                ct);
        }
    }

    public async Task<UnitResult<DeclareDoorRevenueError>> DeclareDoorRevenueAsync(
        int id,
        decimal doorRevenue,
        CancellationToken ct = default)
    {
        if (membership.Membership is not { } actor)
            return new DeclareDoorRevenueError.VenueForbidden();

        try
        {
            return await commandExecutor.ExecuteAsync<ConcertService, UnitResult<DeclareDoorRevenueError>>(
                (service, token) => service.DeclareDoorRevenueCommandAsync(id, doorRevenue, actor, token),
                (service, _, token) => service.ValidateDoorRevenueAuthorityAsync(id, actor, token),
                () => new DeclareDoorRevenueError.VenueForbidden(),
                ct);
        }
        catch (DbUpdateException exception) when (exception.IsConcertConcurrencyConflict(id))
        {
            return new DeclareDoorRevenueError.Superseded(id);
        }
    }

    private Task<Result<ConcertUpdateResponse, UpdateConcertError>> UpdateCommandAsync(
        int id,
        UpdateConcertRequest request,
        MembershipSnapshot actor,
        CancellationToken ct) =>
        privilegedOutboxUnitOfWorkBehavior.ExecuteAsync(
            () => UpdateCoreAsync(id, request, actor, ct),
            ct);

    private async Task<Result<ConcertUpdateResponse, UpdateConcertError>> UpdateCoreAsync(
        int id,
        UpdateConcertRequest request,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        if (actor is null
            || !permissionCatalog.Grants(actor.Role, TenantPermission.ConcertsOpsEdit))
            return new UpdateConcertError.NotPermitted();

        var concert = await privilegedRepository.GetByIdForUpdateAsync(id, ct);
        if (concert is null)
            return new UpdateConcertError.ConcertNotFound(id);
        if (!await CanOperateAsync(id, actor, TenantPermission.ConcertsOpsEdit, ct))
            return new UpdateConcertError.NotPermitted();

        var validation = concertValidator.CanUpdate(concert, request.TotalTickets);
        if (validation.TryGetErrors(out var errors))
            return new UpdateConcertError.Invalid(new ValidationErrors(errors.ToDictionary()));

        concert.Update(request.Name, request.About, request.Price, request.TotalTickets);
        await privilegedRepository.SaveChangesAsync(ct);

        return new ConcertUpdateResponse
        {
            Id = concert.Id,
            Name = concert.Name,
            About = concert.About,
            Price = concert.Price,
            TotalTickets = concert.TotalTickets,
            AvailableTickets = 0,
        };
    }

    private Task<UnitResult<PostConcertError>> PostCommandAsync(
        int id,
        UpdateConcertRequest request,
        MembershipSnapshot actor,
        CancellationToken ct) =>
        privilegedOutboxUnitOfWorkBehavior.ExecuteAsync(
            () => PostCoreAsync(id, request, actor, ct),
            ct);

    private async Task<UnitResult<PostConcertError>> PostCoreAsync(
        int id,
        UpdateConcertRequest request,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        if (actor is null
            || !permissionCatalog.Grants(actor.Role, TenantPermission.ConcertsOpsEdit))
            return new PostConcertError.NotPermitted();

        var concert = await privilegedRepository.GetByIdForUpdateAsync(id, ct);
        if (concert is null)
            return new PostConcertError.ConcertNotFound(id);
        if (!await CanOperateAsync(id, actor, TenantPermission.ConcertsOpsEdit, ct))
            return new PostConcertError.NotPermitted();

        var validation = concertValidator.CanPost(concert);
        if (validation.TryGetErrors(out var errors))
            return new PostConcertError.Invalid(new ValidationErrors(errors.ToDictionary()));

        if (concert.Post(
                request.Name,
                request.About,
                request.Price,
                request.TotalTickets,
                timeProvider.GetUtcNow().UtcDateTime)
            .TryGetError(out var transitionError))
            return new PostConcertError.InvalidTransition(transitionError);

        await privilegedRepository.SaveChangesAsync(ct);
        return new Success();
    }

    private async Task<UnitResult<PostConcertError>> ClassifyPostConflictAsync(
        int id,
        CancellationToken ct) =>
        await privilegedOutboxUnitOfWorkBehavior.ExecuteAsync(async () =>
            await privilegedRepository.GetStateByIdAsync(id, ct) == ConcertState.Posted
                ? (UnitResult<PostConcertError>)new Success()
                : new PostConcertError.Superseded(id),
            ct);

    private Task<UnitResult<DeclareDoorRevenueError>> DeclareDoorRevenueCommandAsync(
        int id,
        decimal doorRevenue,
        MembershipSnapshot actor,
        CancellationToken ct) =>
        privilegedOutboxUnitOfWorkBehavior.ExecuteAsync(
            () => DeclareDoorRevenueCoreAsync(id, doorRevenue, actor, ct),
            ct);

    private async Task<UnitResult<DeclareDoorRevenueError>> DeclareDoorRevenueCoreAsync(
        int id,
        decimal doorRevenue,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        if (actor is null
            || !permissionCatalog.Grants(actor.Role, TenantPermission.ConcertsDeclareDoorRevenue))
            return new DeclareDoorRevenueError.VenueForbidden();

        var concert = await privilegedRepository.GetByIdForUpdateAsync(id, ct);
        if (concert is null)
            return new DeclareDoorRevenueError.ConcertNotFound(id);
        if (!await privilegedRepository.CanDeclareDoorRevenueAsync(
                id,
                actor,
                permissionCatalog.AudienceFor(actor.Role, TenantPermission.ConcertsDeclareDoorRevenue),
                timeProvider.GetUtcNow().UtcDateTime,
                ct))
            return new DeclareDoorRevenueError.VenueForbidden();

        if (concert is not DoorRevenueConcert doorRevenueConcert)
            return new DeclareDoorRevenueError.WrongDealType();
        if (timeProvider.GetUtcNow().UtcDateTime < concert.Period.End)
            return new DeclareDoorRevenueError.TooEarly();
        if (concert.State is not (ConcertState.Draft or ConcertState.Posted))
            return new DeclareDoorRevenueError.AlreadySettled();
        if (doorRevenueConcert.DeclareDoorRevenue(doorRevenue).TryGetError(out var revenueError))
            return revenueError.ToDeclareDoorRevenueError();

        await privilegedRepository.SaveChangesAsync(ct);
        return new Success();
    }

    private async Task<bool> ValidateOperationsAuthorityAsync(
        int id,
        MembershipSnapshot expectedActor,
        string permission,
        CancellationToken ct)
    {
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        return actor is not null
            && permissionCatalog.Grants(actor.Role, permission)
            && await CanOperateAsync(id, actor, permission, ct);
    }

    private async Task<bool> ValidateDoorRevenueAuthorityAsync(
        int id,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        return actor is not null
            && permissionCatalog.Grants(actor.Role, TenantPermission.ConcertsDeclareDoorRevenue)
            && await privilegedRepository.CanDeclareDoorRevenueAsync(
                id,
                actor,
                permissionCatalog.AudienceFor(actor.Role, TenantPermission.ConcertsDeclareDoorRevenue),
                timeProvider.GetUtcNow().UtcDateTime,
                ct);
    }

    private Task<bool> CanOperateAsync(
        int id,
        MembershipSnapshot actor,
        string permission,
        CancellationToken ct) =>
        privilegedRepository.CanOperateAsync(
            id,
            actor,
            permissionCatalog.AudienceFor(actor.Role, permission),
            timeProvider.GetUtcNow().UtcDateTime,
            ct);

    public async Task<Result<ConcertSummaryShare, ShareConcertSummaryError>> ShareSummaryAsync(
        int id,
        ShareConcertSummaryRequest request,
        CancellationToken ct = default)
    {
        if (membership.Membership is not { } actor)
            return new ShareConcertSummaryError.NotPermitted();

        if (request.RequestId == Guid.Empty || request.RecipientTenantId == Guid.Empty)
            return new ShareConcertSummaryError.InvalidRecipient();

        try
        {
            return await commandExecutor.ExecuteAsync<ConcertService, Result<ConcertSummaryShare, ShareConcertSummaryError>>(
                (service, token) => service.ShareSummaryCommandAsync(id, request, actor, token),
                (service, _, token) => service.ValidateShareAuthorityAsync(id, actor, token),
                () => new ShareConcertSummaryError.NotPermitted(),
                ct);
        }
        catch (DbUpdateException exception) when (exception.IsConcertConcurrencyConflict(id))
        {
            return new ShareConcertSummaryError.Superseded(id);
        }
    }

    private Task<Result<ConcertSummaryShare, ShareConcertSummaryError>> ShareSummaryCommandAsync(
        int id,
        ShareConcertSummaryRequest request,
        MembershipSnapshot actor,
        CancellationToken ct) =>
        privilegedOutboxUnitOfWorkBehavior.ExecuteAsync(
            () => ShareSummaryCoreAsync(id, request, actor, ct),
            ct);

    private async Task<Result<ConcertSummaryShare, ShareConcertSummaryError>> ShareSummaryCoreAsync(
        int id,
        ShareConcertSummaryRequest request,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var facts = await tenantCommandFacts.ResolveAsync(
            expectedActor,
            request.RecipientTenantId,
            request.RecipientMembershipId,
            ct);
        if (facts is null
            || !permissionCatalog.Grants(facts.Actor.Role, TenantPermission.ResourcesShare))
            return new ShareConcertSummaryError.NotPermitted();
        if (!facts.TargetTenantExists
            || request.RecipientMembershipId is not null && facts.TargetMembership is null)
            return new ShareConcertSummaryError.InvalidRecipient();

        if (await privilegedRepository.GetIdentityByIdForUpdateAsync(id, ct) is null)
            return new ShareConcertSummaryError.ConcertNotFound(id);
        if (!await CanShareAsync(id, facts.Actor, ct))
            return new ShareConcertSummaryError.NotPermitted();

        var concert = await privilegedRepository.GetWithGrantsByIdForUpdateAsync(id, ct)
            ?? throw new InvalidOperationException($"Concert {id} disappeared while locked.");
        var payloadHash = ResourceCommandReceipt.HashPayload(
            id, request.RecipientTenantId, request.RecipientMembershipId, request.ValidUntil);
        var receipt = await receiptRepository.GetByTenantIdAndOperationAndRequestIdForUpdateAsync(
            facts.Actor.TenantId,
            ConcertCommandReceipt.ShareSummaryOperation,
            request.RequestId,
            ct);
        if (receipt is not null)
            return ReplaySummaryShare(concert, receipt, payloadHash);
        if (concert.AccessVersion != request.ExpectedAccessVersion)
            return new ShareConcertSummaryError.Superseded(id);

        var decidedAt = resourceAccess.UtcNow;
        concert.RevokeExpiredSummaryShares(
            facts.Actor.TenantId,
            request.RecipientTenantId,
            request.RecipientMembershipId,
            decidedAt);
        await privilegedRepository.SaveChangesAsync(ct);

        var share = concert.ShareSummary(
            facts.Actor.TenantId,
            facts.Actor.UserId,
            request.RecipientTenantId,
            request.RecipientMembershipId,
            decidedAt,
            request.ValidUntil)
            .MapError(static error => error.ToShareConcertSummaryError());
        if (share.TryGetError(out var shareError))
            return shareError;
        if (!share.TryGetValue(out var grant))
            return new ShareConcertSummaryError.NotPermitted();

        privilegedRepository.AddAccessGrants([grant]);
        receiptRepository.Add(
            ConcertCommandReceipt.Record(
                facts.Actor.TenantId,
                ConcertCommandReceipt.ShareSummaryOperation,
                request.RequestId,
                payloadHash,
                grant.Id.ToString(),
                decidedAt));
        await privilegedRepository.SaveChangesAsync(ct);
        return grant.ToSummaryShare(concert.AccessVersion);
    }

    private static Result<ConcertSummaryShare, ShareConcertSummaryError> ReplaySummaryShare(
        ConcertEntity concert,
        ConcertCommandReceipt receipt,
        string payloadHash) =>
        receipt.Matches(payloadHash)
            && Guid.TryParse(receipt.Outcome, out var grantId)
            && concert.AccessGrants.SingleOrDefault(grant => grant.Id == grantId) is { } issued
            ? issued.ToSummaryShare(concert.AccessVersion)
            : new ShareConcertSummaryError.RequestConflict();

    public async Task<UnitResult<RevokeConcertSummaryShareError>> RevokeSummaryShareAsync(
        int id,
        Guid grantId,
        long expectedAccessVersion,
        CancellationToken ct = default)
    {
        if (membership.Membership is not { } actor)
            return new RevokeConcertSummaryShareError.NotPermitted();

        try
        {
            return await commandExecutor.ExecuteAsync<ConcertService, UnitResult<RevokeConcertSummaryShareError>>(
                (service, token) => service.RevokeSummaryShareCommandAsync(
                    id, grantId, expectedAccessVersion, actor, token),
                (service, _, token) => service.ValidateShareAuthorityAsync(id, actor, token),
                () => new RevokeConcertSummaryShareError.NotPermitted(),
                ct);
        }
        catch (DbUpdateException exception) when (exception.IsConcertConcurrencyConflict(id))
        {
            return new RevokeConcertSummaryShareError.Superseded(id);
        }
    }

    private Task<UnitResult<RevokeConcertSummaryShareError>> RevokeSummaryShareCommandAsync(
        int id,
        Guid grantId,
        long expectedAccessVersion,
        MembershipSnapshot actor,
        CancellationToken ct) =>
        privilegedOutboxUnitOfWorkBehavior.ExecuteAsync(
            () => RevokeSummaryShareCoreAsync(id, grantId, expectedAccessVersion, actor, ct),
            ct);

    private async Task<UnitResult<RevokeConcertSummaryShareError>> RevokeSummaryShareCoreAsync(
        int id,
        Guid grantId,
        long expectedAccessVersion,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var facts = await tenantCommandFacts.ResolveAsync(expectedActor, expectedActor.TenantId, ct: ct);
        if (facts is null
            || !permissionCatalog.Grants(facts.Actor.Role, TenantPermission.ResourcesShare))
            return new RevokeConcertSummaryShareError.NotPermitted();

        var concert = await privilegedRepository.GetWithGrantsByIdForUpdateAsync(id, ct);
        if (concert is null)
            return new RevokeConcertSummaryShareError.ConcertNotFound(id);
        if (!await CanShareAsync(id, facts.Actor, ct))
            return new RevokeConcertSummaryShareError.NotPermitted();
        if (concert.AccessVersion != expectedAccessVersion)
            return new RevokeConcertSummaryShareError.Superseded(id);
        if (concert.RevokeSummaryShare(grantId, facts.Actor.TenantId, resourceAccess.UtcNow)
            .TryGetError(out var revocationError))
            return revocationError.ToRevokeConcertSummaryShareError();

        await privilegedRepository.SaveChangesAsync(ct);
        return new Success();
    }

    public async Task<UnitResult<AssignConcertMemberError>> AssignMemberAsync(
        int id,
        AssignConcertMemberRequest request,
        CancellationToken ct = default)
    {
        if (membership.Membership is not { } actor)
            return new AssignConcertMemberError.NotPermitted();

        try
        {
            return await commandExecutor.ExecuteAsync<ConcertService, UnitResult<AssignConcertMemberError>>(
                (service, token) => service.AssignMemberCommandAsync(id, request, actor, token),
                (service, _, token) => service.ValidateShareAuthorityAsync(id, actor, token),
                () => new AssignConcertMemberError.NotPermitted(),
                ct);
        }
        catch (DbUpdateException exception) when (exception.IsConcertConcurrencyConflict(id))
        {
            return new AssignConcertMemberError.Superseded(id);
        }
    }

    private Task<UnitResult<AssignConcertMemberError>> AssignMemberCommandAsync(
        int id,
        AssignConcertMemberRequest request,
        MembershipSnapshot actor,
        CancellationToken ct) =>
        privilegedOutboxUnitOfWorkBehavior.ExecuteAsync(
            () => AssignMemberCoreAsync(id, request, actor, ct),
            ct);

    private async Task<UnitResult<AssignConcertMemberError>> AssignMemberCoreAsync(
        int id,
        AssignConcertMemberRequest request,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var facts = await tenantCommandFacts.ResolveAsync(
            expectedActor, expectedActor.TenantId, request.MembershipId, ct);
        if (facts is null
            || !permissionCatalog.Grants(facts.Actor.Role, TenantPermission.ResourcesShare))
            return new AssignConcertMemberError.NotPermitted();
        if (facts.TargetMembership is null)
            return new AssignConcertMemberError.InvalidMembership();

        var concert = await privilegedRepository.GetWithGrantsByIdForUpdateAsync(id, ct);
        if (concert is null)
            return new AssignConcertMemberError.ConcertNotFound(id);
        if (!await CanShareAsync(id, facts.Actor, ct))
            return new AssignConcertMemberError.NotPermitted();
        if (concert.AccessVersion != request.ExpectedAccessVersion)
            return new AssignConcertMemberError.Superseded(id);
        var assignment = concert.AssignMember(
                facts.Actor.TenantId,
                facts.TargetMembership.MembershipId,
                facts.Actor.TenantId,
                resourceAccess.UtcNow);
        if (assignment.TryGetError(out var assignmentError))
            return assignmentError.ToAssignConcertMemberError();
        if (!assignment.TryGetValue(out var grants))
            return new AssignConcertMemberError.NotPermitted();

        privilegedRepository.AddAccessGrants(grants);
        await privilegedRepository.SaveChangesAsync(ct);
        return new Success();
    }

    public async Task<UnitResult<AssignConcertMemberError>> RemoveMemberAssignmentAsync(
        int id,
        Guid membershipId,
        CancellationToken ct = default)
    {
        if (membership.Membership is not { } actor)
            return new AssignConcertMemberError.NotPermitted();

        try
        {
            return await commandExecutor.ExecuteAsync<ConcertService, UnitResult<AssignConcertMemberError>>(
                (service, token) => service.RemoveMemberAssignmentCommandAsync(id, membershipId, actor, token),
                (service, _, token) => service.ValidateShareAuthorityAsync(id, actor, token),
                () => new AssignConcertMemberError.NotPermitted(),
                ct);
        }
        catch (DbUpdateException exception) when (exception.IsConcertConcurrencyConflict(id))
        {
            return new AssignConcertMemberError.Superseded(id);
        }
    }

    private Task<UnitResult<AssignConcertMemberError>> RemoveMemberAssignmentCommandAsync(
        int id,
        Guid membershipId,
        MembershipSnapshot actor,
        CancellationToken ct) =>
        privilegedOutboxUnitOfWorkBehavior.ExecuteAsync(
            () => RemoveMemberAssignmentCoreAsync(id, membershipId, actor, ct),
            ct);

    private async Task<UnitResult<AssignConcertMemberError>> RemoveMemberAssignmentCoreAsync(
        int id,
        Guid membershipId,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var facts = await tenantCommandFacts.ResolveAsync(
            expectedActor, expectedActor.TenantId, membershipId, ct);
        if (facts is null
            || !permissionCatalog.Grants(facts.Actor.Role, TenantPermission.ResourcesShare))
            return new AssignConcertMemberError.NotPermitted();

        var concert = await privilegedRepository.GetWithGrantsByIdForUpdateAsync(id, ct);
        if (concert is null)
            return new AssignConcertMemberError.ConcertNotFound(id);
        if (!await CanShareAsync(id, facts.Actor, ct))
            return new AssignConcertMemberError.NotPermitted();
        if (concert.RemoveMemberAssignment(facts.Actor.TenantId, membershipId, resourceAccess.UtcNow)
            .TryGetError(out var assignmentError))
            return assignmentError.ToAssignConcertMemberError();

        await privilegedRepository.SaveChangesAsync(ct);
        return new Success();
    }

    private async Task<bool> ValidateShareAuthorityAsync(
        int id,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        return actor is not null
            && permissionCatalog.Grants(actor.Role, TenantPermission.ResourcesShare)
            && await CanShareAsync(id, actor, ct);
    }

    private Task<bool> CanShareAsync(
        int id,
        MembershipSnapshot actor,
        CancellationToken ct) =>
        privilegedRepository.CanShareAsync(
            id,
            actor,
            permissionCatalog.AudienceFor(actor.Role, TenantPermission.ResourcesShare),
            timeProvider.GetUtcNow().UtcDateTime,
            ct);

    public Task<IReadOnlyList<ConcertSummary>> GetUnpostedByArtistIdAsync(
        int id,
        CancellationToken ct = default) =>
        concertRepository.GetUnpostedByArtistIdAsync(id, ct);

    public Task<IReadOnlyList<ConcertSummary>> GetUnpostedByVenueIdAsync(
        int id,
        CancellationToken ct = default) =>
        concertRepository.GetUnpostedByVenueIdAsync(id, ct);

    public Task<UnitResult<CancelConcertError>> CancelAsync(
        int concertId,
        CancellationToken ct = default) =>
        workflow.CancelAsync(concertId, ct);

    private async Task<ConcertOperations> WithOperationsActionsAsync(
        ConcertOperations operations,
        CancellationToken ct)
    {
        if (membership.Membership is not { } actor
            || await concertRepository.GetWithGrantsByIdAsync(operations.Id, ct) is not { } concert)
            return operations with { CanCancel = false };

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var canCancel = permissionCatalog.Grants(actor.Role, TenantPermission.ConcertsManage)
            && (concert.VenueTenantId == actor.TenantId || concert.ArtistTenantId == actor.TenantId)
            && ResourceGrantPolicy.Allows(
                concert.AccessGrants,
                Concertable.B2B.Concert.Contracts.Enums.ConcertAccessScope.Operations,
                actor,
                permissionCatalog.AudienceFor(actor.Role, TenantPermission.ConcertsManage),
                now);
        return operations with
        {
            CanCancel = canCancel
                && operations.State is ConcertState.Draft or ConcertState.Posted or ConcertState.CancellationFailed,
        };
    }

    private async Task<ConcertFinance> WithFinanceActionsAsync(
        ConcertFinance finance,
        CancellationToken ct)
    {
        if (membership.Membership is not { } actor
            || await concertRepository.GetWithGrantsByIdAsync(finance.Id, ct) is not { } concert)
            return finance with { CanDeclareDoorRevenue = false };

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var canDeclareDoorRevenue = permissionCatalog.Grants(
                actor.Role,
                TenantPermission.ConcertsDeclareDoorRevenue)
            && concert.VenueTenantId == actor.TenantId
            && ResourceGrantPolicy.Allows(
                concert.AccessGrants,
                Concertable.B2B.Concert.Contracts.Enums.ConcertAccessScope.Operations,
                actor,
                permissionCatalog.AudienceFor(actor.Role, TenantPermission.ConcertsDeclareDoorRevenue),
                now)
            && ResourceGrantPolicy.Allows(
                concert.AccessGrants,
                Concertable.B2B.Concert.Contracts.Enums.ConcertAccessScope.Finance,
                actor,
                permissionCatalog.AudienceFor(actor.Role, TenantPermission.ConcertsDeclareDoorRevenue),
                now);

        return finance with
        {
            CanDeclareDoorRevenue = canDeclareDoorRevenue
                && concert.State is ConcertState.Draft or ConcertState.Posted
                && concert is DoorRevenueConcert { DoorRevenue: null }
                && concert.Period.End < now,
        };
    }
}
