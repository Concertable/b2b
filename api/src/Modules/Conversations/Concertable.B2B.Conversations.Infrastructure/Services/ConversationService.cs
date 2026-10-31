using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Conversations.Application.Errors;
using Concertable.B2B.Conversations.Application.Requests;
using Concertable.B2B.Conversations.Contracts.Events;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Tenant.Contracts;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Conversations.Infrastructure.Services;

internal sealed class ConversationService : IConversationService
{
    private const string InboxHref = "/?inbox=open";
    private const string UnknownTenant = "Unknown business";
    private readonly IConversationPrivilegedRepository privilegedRepository;
    private readonly IConversationReadPositionRepository readPositionRepository;
    private readonly IMessageRepository messageRepository;
    private readonly IPrivilegedOutboxUnitOfWorkBehavior unitOfWork;
    private readonly ITargetTenantResolver targetTenantResolver;
    private readonly IAuthorizationContext membership;
    private readonly ITransactionRunner transactionRunner;
    private readonly UnitOfWorkAccessor unitOfWorkAccessor;
    private readonly ITenantContext tenantContext;
    private readonly IBus bus;
    private readonly TimeProvider timeProvider;

    public ConversationService(
        IConversationPrivilegedRepository privilegedRepository,
        IConversationReadPositionRepository readPositionRepository,
        IMessageRepository messageRepository,
        IPrivilegedOutboxUnitOfWorkBehavior unitOfWork,
        ITargetTenantResolver targetTenantResolver,
        IAuthorizationContext membership,
        ITransactionRunner transactionRunner,
        UnitOfWorkAccessor unitOfWorkAccessor,
        ITenantContext tenantContext,
        IBus bus,
        TimeProvider timeProvider)
    {
        this.privilegedRepository = privilegedRepository;
        this.readPositionRepository = readPositionRepository;
        this.messageRepository = messageRepository;
        this.unitOfWork = unitOfWork;
        this.targetTenantResolver = targetTenantResolver;
        this.membership = membership;
        this.transactionRunner = transactionRunner;
        this.unitOfWorkAccessor = unitOfWorkAccessor;
        this.tenantContext = tenantContext;
        this.bus = bus;
        this.timeProvider = timeProvider;
    }

    public Task<Result<ConversationDto, CreateConversationError>> CreateAsync(
        CreateConversationRequest request,
        CancellationToken ct = default)
    {
        if (membership.Membership is not { } actor)
            return Task.FromResult<Result<ConversationDto, CreateConversationError>>(
                new CreateConversationError.NotPermitted());

        if (unitOfWorkAccessor.Current is not null)
            return unitOfWork.ExecuteAsync(() => CreateCoreAsync(request, actor, ct), ct);

        return transactionRunner.RunAsync<ConversationService, Result<ConversationDto, CreateConversationError>>(
            (service, token) => service.CreateCommandAsync(request, actor, token),
            ct);
    }

    private Task<Result<ConversationDto, CreateConversationError>> CreateCommandAsync(
        CreateConversationRequest request,
        MembershipSnapshot expectedActor,
        CancellationToken ct) =>
        unitOfWork.ExecuteAsync(() => CreateCoreAsync(request, expectedActor, ct), ct);

    private async Task<Result<ConversationDto, CreateConversationError>> CreateCoreAsync(
        CreateConversationRequest request,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {

        var participants = request.ParticipantTenantIds.Distinct().Order().ToArray();
        if (request.RequestId == Guid.Empty
            || participants.Length < 2
            || participants.Contains(Guid.Empty)
            || !participants.Contains(expectedActor.TenantId))
            return new CreateConversationError.InvalidParticipants();

        var actor = expectedActor;
        var existingParticipants = await targetTenantResolver.ResolveManyAsync(actor, participants, ct);
        if (!existingParticipants.SetEquals(participants))
            return new CreateConversationError.InvalidParticipants();
        if (!actor.Permissions[TenantPermission.MessagesSend].Includes(ResourceAccess.All))
            return new CreateConversationError.NotPermitted();

        var idempotencyHash = IdempotencyHash.Create(string.Join(",", participants));
        var receipt = await privilegedRepository.GetCreationReceiptForUpdateAsync(
            actor.TenantId,
            actor.MembershipId,
            request.RequestId,
            ct);
        if (receipt is not null)
        {
            if (!receipt.Matches(idempotencyHash))
                return new CreateConversationError.RequestConflict();
            var replay = await privilegedRepository.GetByIdAsync(receipt.ConversationId, ct)
                ?? throw new InvalidOperationException("A conversation creation receipt referenced a missing conversation.");
            return await ToDtoAsync(replay, ct);
        }

        var at = timeProvider.GetUtcNow().UtcDateTime;
        var conversation = ConversationEntity.Create(participants, actor.TenantId, at);
        privilegedRepository.Add(conversation);
        await privilegedRepository.SaveChangesAsync(ct);
        privilegedRepository.Add(ConversationCreationReceipt.Record(
            conversation.Id,
            actor.TenantId,
            actor.MembershipId,
            request.RequestId,
            idempotencyHash,
            at));
        await privilegedRepository.SaveChangesAsync(ct);
        return await ToDtoAsync(conversation, ct);
    }

    public async Task<Result<ConversationDto, ConversationAccessError>> GetAsync(
        int conversationId,
        CancellationToken ct = default)
    {
        if (await FindReadableAsync(conversationId, ct) is not { } conversation)
            return new ConversationAccessError.NotFound(conversationId);
        return await ToDtoAsync(conversation, ct);
    }

    public async Task<Result<IReadOnlyList<MessageDto>, ConversationAccessError>> GetMessagesAsync(
        int conversationId,
        CancellationToken ct = default)
    {
        if (await FindReadableAsync(conversationId, ct) is null)
            return new ConversationAccessError.NotFound(conversationId);
        var messages = await messageRepository.GetByConversationIdAsync(conversationId, ct);
        var activeTenantId = tenantContext.GetTenantId();
        return messages.Select(message => message.ToMessageDto(message.SenderTenantId != activeTenantId)).ToList();
    }

    public Task<Result<MessageDto, SendMessageError>> SendAsync(
        int conversationId,
        SendMessageRequest request,
        MessageAction? action = null,
        CancellationToken ct = default)
    {
        if (membership.Membership is not { } actor)
            return Task.FromResult<Result<MessageDto, SendMessageError>>(new SendMessageError.NotPermitted());
        if (unitOfWorkAccessor.Current is not null)
            return unitOfWork.ExecuteAsync(
                () => SendCoreAsync(conversationId, request, action, actor, ct), ct);
        return transactionRunner.RunAsync<ConversationService, Result<MessageDto, SendMessageError>>(
            (service, token) => service.SendCommandAsync(conversationId, request, action, actor, token),
            ct);
    }

    private Task<Result<MessageDto, SendMessageError>> SendCommandAsync(
        int conversationId,
        SendMessageRequest request,
        MessageAction? action,
        MembershipSnapshot expectedActor,
        CancellationToken ct) =>
        unitOfWork.ExecuteAsync(() => SendCoreAsync(conversationId, request, action, expectedActor, ct), ct);

    private async Task<Result<MessageDto, SendMessageError>> SendCoreAsync(
        int conversationId,
        SendMessageRequest request,
        MessageAction? action,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        if (request.RequestId == Guid.Empty || string.IsNullOrWhiteSpace(request.Content))
            return new SendMessageError.InvalidMessage();
        var actor = expectedActor;
        if (!actor.HasPermission(TenantPermission.MessagesSend))
            return new SendMessageError.NotPermitted();
        if (conversationId <= 0)
            return new SendMessageError.NotFound(conversationId);
        var conversation = await privilegedRepository.GetByIdForUpdateAsync(conversationId, ct);
        if (conversation is null || !conversation.AcceptsMessagesFrom(actor))
            return new SendMessageError.NotPermitted();
        var idempotencyHash = IdempotencyHash.Create(request.Content, action);
        var replay = await privilegedRepository.GetMessageReceiptForUpdateAsync(
            conversationId, actor.MembershipId, request.RequestId, ct);
        if (replay is not null)
            return replay.IdempotencyHash == idempotencyHash ? replay.ToMessageDto() : new SendMessageError.RequestConflict();

        var message = MessageEntity.Create(
            conversationId,
            conversation.AllocateMessageSequence(),
            request.RequestId,
            idempotencyHash,
            actor.TenantId,
            actor.MembershipId,
            actor.UserId,
            request.Content.Trim(),
            timeProvider.GetUtcNow().UtcDateTime,
            action);
        privilegedRepository.Add(message);
        await bus.PublishAsync(new ConversationChanged(conversationId), ct);
        await privilegedRepository.SaveChangesAsync(ct);
        return message.ToMessageDto();
    }

    public Task<UnitResult<ConversationAccessError>> AdvanceReadPositionAsync(
        int conversationId,
        AdvanceConversationReadPositionRequest request,
        CancellationToken ct = default)
    {
        if (membership.Membership is not { } actor)
            return Task.FromResult<UnitResult<ConversationAccessError>>(
                new ConversationAccessError.NotPermitted());
        return transactionRunner.RunAsync<ConversationService, UnitResult<ConversationAccessError>>(
            (service, token) => service.AdvanceReadPositionCommandAsync(conversationId, request, actor, token),
            ct);
    }

    private Task<UnitResult<ConversationAccessError>> AdvanceReadPositionCommandAsync(
        int conversationId,
        AdvanceConversationReadPositionRequest request,
        MembershipSnapshot expectedActor,
        CancellationToken ct) =>
        unitOfWork.ExecuteAsync(() => AdvanceReadPositionCoreAsync(conversationId, request, expectedActor, ct), ct);

    private async Task<UnitResult<ConversationAccessError>> AdvanceReadPositionCoreAsync(
        int conversationId,
        AdvanceConversationReadPositionRequest request,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var actor = expectedActor;
        if (!actor.HasPermission(TenantPermission.MessagesRead))
            return new ConversationAccessError.NotPermitted();
        if (conversationId <= 0)
            return new ConversationAccessError.NotFound(conversationId);
        var conversation = await privilegedRepository.GetByIdForUpdateAsync(conversationId, ct);
        if (conversation is null || !conversation.IsReadableBy(actor))
            return new ConversationAccessError.NotPermitted();
        if (request.ThroughSequence <= 0
            || !await messageRepository.ContainsSequenceAsync(conversationId, request.ThroughSequence, ct))
            return new ConversationAccessError.InvalidSequence();
        await readPositionRepository.AdvanceAsync(
            conversationId, actor.TenantId, actor.MembershipId, request.ThroughSequence, ct);
        return new Success();
    }

    public Task<UnitResult<AssignConversationMemberError>> AssignMemberAsync(
        int conversationId,
        AssignConversationMemberRequest request,
        CancellationToken ct = default) =>
        RunAssignmentCommandAsync(
            (actor, token) => AssignMemberCoreAsync(
                conversationId, request.MembershipId, request.ExpectedAccessVersion, actor, token),
            ct);

    public Task<UnitResult<AssignConversationMemberError>> UnassignMemberAsync(
        int conversationId,
        Guid membershipId,
        long expectedAccessVersion,
        CancellationToken ct = default) =>
        RunAssignmentCommandAsync(
            (actor, token) => UnassignMemberCoreAsync(conversationId, membershipId, expectedAccessVersion, actor, token),
            ct);

    private Task<UnitResult<AssignConversationMemberError>> RunAssignmentCommandAsync(
        Func<MembershipSnapshot, CancellationToken, Task<UnitResult<AssignConversationMemberError>>> command,
        CancellationToken ct)
    {
        if (membership.Membership is not { } actor)
            return Task.FromResult<UnitResult<AssignConversationMemberError>>(
                new AssignConversationMemberError.NotPermitted());
        return transactionRunner.RunAsync<ConversationService, UnitResult<AssignConversationMemberError>>(
            (service, token) => service.unitOfWork.ExecuteAsync(() => command(actor, token), token),
            ct);
    }

    private async Task<UnitResult<AssignConversationMemberError>> AssignMemberCoreAsync(
        int conversationId,
        Guid membershipId,
        long expectedAccessVersion,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var loaded = await LoadForAssignmentChangeAsync(conversationId, membershipId, expectedAccessVersion, expectedActor, ct);
        if (!loaded.TryGetValue(out var change))
        {
            loaded.TryGetError(out var error);
            return error!;
        }
        if (change.Resolution.TargetMembership is not { } target)
            return new AssignConversationMemberError.InvalidMembership();
        if (!change.Conversation.AssignMember(
                change.Resolution.Actor.TenantId,
                change.Resolution.Actor.UserId,
                membershipId,
                target.TenantId,
                timeProvider.GetUtcNow().UtcDateTime))
            return new AssignConversationMemberError.AlreadyAssigned();
        return await SaveAssignmentChangeAsync(conversationId, ct);
    }

    private async Task<UnitResult<AssignConversationMemberError>> UnassignMemberCoreAsync(
        int conversationId,
        Guid membershipId,
        long expectedAccessVersion,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var loaded = await LoadForAssignmentChangeAsync(conversationId, membershipId, expectedAccessVersion, expectedActor, ct);
        if (!loaded.TryGetValue(out var change))
        {
            loaded.TryGetError(out var error);
            return error!;
        }
        if (change.Resolution.TargetMembership is null
            || !change.Conversation.UnassignMember(change.Resolution.Actor.TenantId, membershipId))
            return new AssignConversationMemberError.InvalidMembership();
        return await SaveAssignmentChangeAsync(conversationId, ct);
    }

    private async Task<Result<AssignmentChange, AssignConversationMemberError>> LoadForAssignmentChangeAsync(
        int conversationId,
        Guid membershipId,
        long expectedAccessVersion,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var resolutionOption = await targetTenantResolver.ResolveAsync(
            expectedActor, expectedActor.TenantId, membershipId, ct);
        if (!resolutionOption.TryGetValue(out var resolution)
            || !resolution.Actor.HasPermission(TenantPermission.ResourcesShare))
            return new AssignConversationMemberError.NotPermitted();
        if (conversationId <= 0)
            return new AssignConversationMemberError.NotFound(conversationId);
        var conversation = await privilegedRepository.GetByIdForUpdateAsync(conversationId, ct);
        if (conversation is null || !conversation.IsShareableBy(resolution.Actor))
            return new AssignConversationMemberError.NotPermitted();
        if (conversation.AccessVersion != expectedAccessVersion)
            return new AssignConversationMemberError.Superseded(conversationId);
        return new AssignmentChange(conversation, resolution);
    }

    private async Task<UnitResult<AssignConversationMemberError>> SaveAssignmentChangeAsync(
        int conversationId, CancellationToken ct)
    {
        await privilegedRepository.SaveChangesAsync(ct);
        await bus.PublishAsync(new ConversationChanged(conversationId), ct);
        return new Success();
    }

    private sealed record AssignmentChange(ConversationEntity Conversation, TenantResolution Resolution);

    public async Task<int> GetUnreadCountAsync(CancellationToken ct = default)
    {
        var actor = ReadActor();
        return actor is null ? 0 : await messageRepository.GetUnreadCountAsync(actor, ct);
    }

    public async Task<IReadOnlyList<MessagePreviewDto>> GetRecentPreviewsAsync(
        int pageNumber,
        CancellationToken ct = default)
    {
        var actor = ReadActor();
        if (actor is null)
            return [];
        var previews = await messageRepository.GetRecentPreviewsAsync(actor, pageNumber, ct);
        var results = new List<MessagePreviewDto>(previews.Count);
        foreach (var preview in previews)
        {
            var conversation = await privilegedRepository.GetByIdAsync(preview.ConversationId, ct);
            if (conversation is null)
                continue;
            results.Add(new MessagePreviewDto(
                preview.Id,
                preview.ConversationId,
                await ParticipantsAsync(conversation, ct),
                preview.Preview,
                preview.At,
                preview.Unread,
                InboxHref));
        }
        return results;
    }

    private MembershipSnapshot? ReadActor() =>
        membership.Membership is { } actor && actor.HasPermission(TenantPermission.MessagesRead)
            ? actor
            : null;

    private async Task<ConversationEntity?> FindReadableAsync(int conversationId, CancellationToken ct) =>
        conversationId > 0
        && membership.Membership is { } actor
        && await privilegedRepository.GetByIdAsync(conversationId, ct) is { } conversation
        && conversation.IsReadableBy(actor)
            ? conversation
            : null;

    private async Task<ConversationDto> ToDtoAsync(ConversationEntity conversation, CancellationToken ct) =>
        new(conversation.Id, conversation.AccessVersion, await ParticipantsAsync(conversation, ct));

    private async Task<IReadOnlyList<ConversationParticipantDto>> ParticipantsAsync(
        ConversationEntity conversation,
        CancellationToken ct)
    {
        var tenantIds = conversation.Participants.Select(participant => participant.TenantId).ToHashSet();
        var displays = await privilegedRepository.GetTenantDisplaysAsync(tenantIds, ct);
        return tenantIds
            .Order()
            .Select(id => new ConversationParticipantDto(
                id,
                displays.TryGetValue(id, out var display) ? display.DisplayName : UnknownTenant))
            .ToList();
    }
}
