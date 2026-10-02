using ITenantResolver = Concertable.B2B.Tenant.Contracts.ITenantResolver;
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
    private readonly ITenantResolver tenantResolver;
    private readonly ITenantCapabilityAuthorization tenantCapabilities;
    private readonly IResourceAuthorization resources;
    private readonly IAuthorizationContext authorizationContext;
    private readonly IMembershipContext membership;
    private readonly IMembershipResolver membershipResolver;
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
        ITenantResolver tenantResolver,
        ITenantCapabilityAuthorization tenantCapabilities,
        IResourceAuthorization resources,
        IAuthorizationContext authorizationContext,
        IMembershipContext membership,
        IMembershipResolver membershipResolver,
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
        this.tenantResolver = tenantResolver;
        this.tenantCapabilities = tenantCapabilities;
        this.resources = resources;
        this.authorizationContext = authorizationContext;
        this.membership = membership;
        this.membershipResolver = membershipResolver;
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
        {
            if (authorizationContext.IsActive)
            {
                authorizationContext.RegisterFailure<Result<ConversationDto, CreateConversationError>>(
                    () => new CreateConversationError.NotPermitted());
                authorizationContext.MarkAuthorityFailed();
            }
            return Task.FromResult<Result<ConversationDto, CreateConversationError>>(
                new CreateConversationError.NotPermitted());
        }

        if (unitOfWorkAccessor.Current is not null)
            return unitOfWork.ExecuteAsync(() => CreateCoreAsync(request, actor, ct), ct);

        return transactionRunner.ExecuteAsync<ConversationService, Result<ConversationDto, CreateConversationError>>(
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
        authorizationContext.RegisterFailure<Result<ConversationDto, CreateConversationError>>(
            () => new CreateConversationError.NotPermitted());

        var participants = request.ParticipantTenantIds.Distinct().Order().ToArray();
        if (request.RequestId == Guid.Empty
            || participants.Length < 2
            || participants.Contains(Guid.Empty)
            || !participants.Contains(expectedActor.TenantId))
            return new CreateConversationError.InvalidParticipants();

        var resolutionOption = await tenantResolver.ResolveManyAsync(expectedActor, participants, ct);
        if (!resolutionOption.TryGetValue(out var resolution))
            return new CreateConversationError.NotPermitted();
        if (!resolution.ExistingTenantIds.SetEquals(participants))
            return new CreateConversationError.InvalidParticipants();
        if (await tenantCapabilities.RequireAsync(TenantPermission.MessagesSend, ct)
            != AuthorizationDecision.Allowed)
            return new CreateConversationError.NotPermitted();

        var payloadHash = CommandPayloadHash.Create(string.Join(",", participants));
        var receipt = await privilegedRepository.GetCreationReceiptForUpdateAsync(
            resolution.Actor.TenantId,
            resolution.Actor.MembershipId,
            request.RequestId,
            ct);
        if (receipt is not null)
        {
            if (!receipt.Matches(payloadHash))
                return new CreateConversationError.RequestConflict();
            var replay = await privilegedRepository.GetWithGrantsByIdAsync(receipt.ConversationId, ct)
                ?? throw new InvalidOperationException("A conversation creation receipt referenced a missing conversation.");
            return await ToDtoAsync(replay, ct);
        }

        var at = timeProvider.GetUtcNow().UtcDateTime;
        var conversation = ConversationEntity.Create(participants, resolution.Actor.TenantId, resolution.Actor.UserId, at);
        privilegedRepository.Add(conversation);
        await privilegedRepository.SaveChangesAsync(ct);
        privilegedRepository.Add(ConversationCreationReceipt.Record(
            conversation.Id,
            resolution.Actor.TenantId,
            resolution.Actor.MembershipId,
            request.RequestId,
            payloadHash,
            at));
        await privilegedRepository.SaveChangesAsync(ct);
        return await ToDtoAsync(conversation, ct);
    }

    public async Task<Result<ConversationDto, ConversationAccessError>> GetAsync(
        int conversationId,
        CancellationToken ct = default)
    {
        if (conversationId <= 0
            || await resources.CheckAsync(ReadRequest(conversationId), ct) != AuthorizationDecision.Allowed)
            return new ConversationAccessError.NotFound(conversationId);
        var conversation = await privilegedRepository.GetWithGrantsByIdAsync(conversationId, ct)
            ?? throw new InvalidOperationException($"Conversation {conversationId} disappeared after authorization.");
        return await ToDtoAsync(conversation, ct);
    }

    public async Task<Result<IReadOnlyList<MessageDto>, ConversationAccessError>> GetMessagesAsync(
        int conversationId,
        CancellationToken ct = default)
    {
        if (conversationId <= 0
            || await resources.CheckAsync(ReadRequest(conversationId), ct) != AuthorizationDecision.Allowed)
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
        {
            if (authorizationContext.IsActive)
            {
                authorizationContext.RegisterFailure<Result<MessageDto, SendMessageError>>(
                    () => new SendMessageError.NotPermitted());
                authorizationContext.MarkAuthorityFailed();
            }
            return Task.FromResult<Result<MessageDto, SendMessageError>>(new SendMessageError.NotPermitted());
        }
        if (unitOfWorkAccessor.Current is not null)
            return unitOfWork.ExecuteAsync(
                () => SendCoreAsync(conversationId, request, action, actor, ct), ct);
        return transactionRunner.ExecuteAsync<ConversationService, Result<MessageDto, SendMessageError>>(
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
        authorizationContext.RegisterFailure<Result<MessageDto, SendMessageError>>(
            () => new SendMessageError.NotPermitted());
        if (request.RequestId == Guid.Empty || string.IsNullOrWhiteSpace(request.Content))
            return new SendMessageError.InvalidMessage();
        var actorOption = await membershipResolver.ResolveSnapshotAsync(expectedActor, ct);
        if (!actorOption.TryGetValue(out var actor)
            || !actor.HasPermission(TenantPermission.MessagesSend))
        {
            authorizationContext.MarkAuthorityFailed();
            return new SendMessageError.NotPermitted();
        }
        if (conversationId <= 0)
            return new SendMessageError.NotFound(conversationId);
        if (await resources.RequireAsync(SendRequest(conversationId), ct) != AuthorizationDecision.Allowed)
            return new SendMessageError.NotPermitted();
        var conversation = await privilegedRepository.GetWithGrantsByIdForUpdateAsync(conversationId, ct);
        if (conversation is null)
            return new SendMessageError.NotFound(conversationId);
        var payloadHash = CommandPayloadHash.Create(request.Content, action);
        var replay = await privilegedRepository.GetMessageReceiptForUpdateAsync(
            conversationId, actor.MembershipId, request.RequestId, ct);
        if (replay is not null)
            return replay.PayloadHash == payloadHash ? replay.ToMessageDto() : new SendMessageError.RequestConflict();

        var message = MessageEntity.Create(
            conversationId,
            conversation.AllocateMessageSequence(),
            request.RequestId,
            payloadHash,
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
        {
            if (authorizationContext.IsActive)
            {
                authorizationContext.RegisterFailure<UnitResult<ConversationAccessError>>(
                    () => new ConversationAccessError.NotPermitted());
                authorizationContext.MarkAuthorityFailed();
            }
            return Task.FromResult<UnitResult<ConversationAccessError>>(
                new ConversationAccessError.NotPermitted());
        }
        return transactionRunner.ExecuteAsync<ConversationService, UnitResult<ConversationAccessError>>(
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
        authorizationContext.RegisterFailure<UnitResult<ConversationAccessError>>(
            () => new ConversationAccessError.NotPermitted());
        var actorOption = await membershipResolver.ResolveSnapshotAsync(expectedActor, ct);
        if (!actorOption.TryGetValue(out var actor) || !actor.HasPermission(TenantPermission.MessagesRead))
        {
            authorizationContext.MarkAuthorityFailed();
            return new ConversationAccessError.NotPermitted();
        }
        if (conversationId <= 0)
            return new ConversationAccessError.NotFound(conversationId);
        if (await resources.RequireAsync(ReadRequest(conversationId), ct) != AuthorizationDecision.Allowed)
            return new ConversationAccessError.NotPermitted();
        var conversation = await privilegedRepository.GetWithGrantsByIdForUpdateAsync(conversationId, ct);
        if (conversation is null)
            return new ConversationAccessError.NotFound(conversationId);
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
        ChangeAssignmentAsync(conversationId, request.MembershipId, request.ExpectedAccessVersion, true, ct);

    public Task<UnitResult<AssignConversationMemberError>> RemoveMemberAssignmentAsync(
        int conversationId,
        Guid membershipId,
        long expectedAccessVersion,
        CancellationToken ct = default) =>
        ChangeAssignmentAsync(conversationId, membershipId, expectedAccessVersion, false, ct);

    private Task<UnitResult<AssignConversationMemberError>> ChangeAssignmentAsync(
        int conversationId,
        Guid membershipId,
        long expectedAccessVersion,
        bool assign,
        CancellationToken ct)
    {
        if (membership.Membership is not { } actor)
        {
            if (authorizationContext.IsActive)
            {
                authorizationContext.RegisterFailure<UnitResult<AssignConversationMemberError>>(
                    () => new AssignConversationMemberError.NotPermitted());
                authorizationContext.MarkAuthorityFailed();
            }
            return Task.FromResult<UnitResult<AssignConversationMemberError>>(
                new AssignConversationMemberError.NotPermitted());
        }
        return transactionRunner.ExecuteAsync<ConversationService, UnitResult<AssignConversationMemberError>>(
            (service, token) => service.ChangeAssignmentCommandAsync(
                conversationId, membershipId, expectedAccessVersion, assign, actor, token),
            ct);
    }

    private Task<UnitResult<AssignConversationMemberError>> ChangeAssignmentCommandAsync(
        int conversationId,
        Guid membershipId,
        long expectedAccessVersion,
        bool assign,
        MembershipSnapshot expectedActor,
        CancellationToken ct) =>
        unitOfWork.ExecuteAsync(
            () => ChangeAssignmentCoreAsync(
                conversationId, membershipId, expectedAccessVersion, assign, expectedActor, ct),
            ct);

    private async Task<UnitResult<AssignConversationMemberError>> ChangeAssignmentCoreAsync(
        int conversationId,
        Guid membershipId,
        long expectedAccessVersion,
        bool assign,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        authorizationContext.RegisterFailure<UnitResult<AssignConversationMemberError>>(
            () => new AssignConversationMemberError.NotPermitted());
        var resolutionOption = await tenantResolver.ResolveAsync(
            expectedActor, expectedActor.TenantId, membershipId, ct);
        if (!resolutionOption.TryGetValue(out var resolution)
            || !resolution.Actor.HasPermission(TenantPermission.ResourcesShare))
        {
            authorizationContext.MarkAuthorityFailed();
            return new AssignConversationMemberError.NotPermitted();
        }
        if (assign && resolution.TargetMembership is null)
            return new AssignConversationMemberError.InvalidMembership();
        if (conversationId <= 0)
            return new AssignConversationMemberError.NotFound(conversationId);
        if (await resources.RequireAsync(ShareRequest(conversationId), ct) != AuthorizationDecision.Allowed)
            return new AssignConversationMemberError.NotPermitted();
        var conversation = await privilegedRepository.GetWithGrantsByIdForUpdateAsync(conversationId, ct);
        if (conversation is null)
            return new AssignConversationMemberError.NotFound(conversationId);
        if (conversation.AccessVersion != expectedAccessVersion)
            return new AssignConversationMemberError.Superseded(conversationId);

        var grants = assign
            ? conversation.AssignMember(
                resolution.Actor.TenantId,
                membershipId,
                resolution.TargetMembership!.TenantId,
                timeProvider.GetUtcNow().UtcDateTime)
            : [];
        var changed = assign
            ? grants.Count > 0
            : conversation.RemoveMemberAssignment(
                resolution.Actor.TenantId,
                membershipId,
                timeProvider.GetUtcNow().UtcDateTime);
        if (!changed)
            return assign
                ? new AssignConversationMemberError.AlreadyAssigned()
                : new AssignConversationMemberError.InvalidMembership();
        if (assign)
            privilegedRepository.AddAccessGrants(grants);
        await privilegedRepository.SaveChangesAsync(ct);
        await bus.PublishAsync(new ConversationChanged(conversationId), ct);
        return new Success();
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken ct = default)
    {
        var actor = await ReadActorAsync(ct);
        return actor is null ? 0 : await messageRepository.GetUnreadCountAsync(actor, ct);
    }

    public async Task<IReadOnlyList<MessagePreviewDto>> GetRecentPreviewsAsync(
        int pageNumber,
        CancellationToken ct = default)
    {
        var actor = await ReadActorAsync(ct);
        if (actor is null)
            return [];
        var previews = await messageRepository.GetRecentPreviewsAsync(actor, pageNumber, ct);
        var results = new List<MessagePreviewDto>(previews.Count);
        foreach (var preview in previews)
        {
            var conversation = await privilegedRepository.GetWithGrantsByIdAsync(preview.ConversationId, ct);
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

    private async Task<MembershipSnapshot?> ReadActorAsync(CancellationToken ct)
    {
        if (membership.Membership is not { } expected || !expected.HasPermission(TenantPermission.MessagesRead))
            return null;
        var resolution = await membershipResolver.ResolveSnapshotAsync(expected, ct);
        return resolution.TryGetValue(out var actor) && actor.HasPermission(TenantPermission.MessagesRead)
            ? actor
            : null;
    }

    private async Task<ConversationDto> ToDtoAsync(ConversationEntity conversation, CancellationToken ct) =>
        new(conversation.Id, conversation.AccessVersion, await ParticipantsAsync(conversation, ct));

    private async Task<IReadOnlyList<ConversationParticipant>> ParticipantsAsync(
        ConversationEntity conversation,
        CancellationToken ct)
    {
        var tenantIds = conversation.AccessGrants
            .Where(grant => grant.Kind == ResourceGrantKind.Principal
                            && grant.Scope == ConversationAccessScope.Read)
            .Select(grant => grant.TenantId)
            .Distinct()
            .ToHashSet();
        var displays = await privilegedRepository.GetTenantDisplaysAsync(tenantIds, ct);
        return tenantIds
            .Order()
            .Select(id => new ConversationParticipant(
                id,
                displays.TryGetValue(id, out var display) ? display.DisplayName : UnknownTenant))
            .ToList();
    }

    private static AuthorizationRequest ReadRequest(int conversationId) =>
        new(TenantPermission.MessagesRead,
            ResourceAddress.Create(ResourceKind.Conversation, conversationId),
            ResourceFacet.Read);

    private static AuthorizationRequest SendRequest(int conversationId) =>
        new(TenantPermission.MessagesSend,
            ResourceAddress.Create(ResourceKind.Conversation, conversationId),
            ResourceFacet.SendMessages);

    private static AuthorizationRequest ShareRequest(int conversationId) =>
        new(TenantPermission.ResourcesShare,
            ResourceAddress.Create(ResourceKind.Conversation, conversationId));
}
