using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Conversations.Application.Errors;
using Concertable.B2B.Conversations.Application.Requests;
using Concertable.B2B.Conversations.Contracts.Events;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.Tenant.Contracts;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Conversations.Infrastructure.Services;

internal sealed class ConversationService : IConversationService
{
    private const string InboxHref = "/?inbox=open";
    private const string UnknownTenant = "Unknown business";
    private readonly IConversationRepository repository;
    private readonly IConversationPrivilegedRepository privilegedRepository;
    private readonly IConversationReadPositionRepository readPositionRepository;
    private readonly IMessageRepository messageRepository;
    private readonly IPrivilegedOutboxUnitOfWorkBehavior unitOfWork;
    private readonly ITenantCommandFacts tenantCommandFacts;
    private readonly IMembershipContext membership;
    private readonly IMembershipAuthorityFence authorityFence;
    private readonly IPermissionCatalog permissionCatalog;
    private readonly ICommandExecutor commandExecutor;
    private readonly ITenantContext tenantContext;
    private readonly IBus bus;
    private readonly TimeProvider timeProvider;

    public ConversationService(
        IConversationRepository repository,
        IConversationPrivilegedRepository privilegedRepository,
        IConversationReadPositionRepository readPositionRepository,
        IMessageRepository messageRepository,
        IPrivilegedOutboxUnitOfWorkBehavior unitOfWork,
        ITenantCommandFacts tenantCommandFacts,
        IMembershipContext membership,
        IMembershipAuthorityFence authorityFence,
        IPermissionCatalog permissionCatalog,
        ICommandExecutor commandExecutor,
        ITenantContext tenantContext,
        IBus bus,
        TimeProvider timeProvider)
    {
        this.repository = repository;
        this.privilegedRepository = privilegedRepository;
        this.readPositionRepository = readPositionRepository;
        this.messageRepository = messageRepository;
        this.unitOfWork = unitOfWork;
        this.tenantCommandFacts = tenantCommandFacts;
        this.membership = membership;
        this.authorityFence = authorityFence;
        this.permissionCatalog = permissionCatalog;
        this.commandExecutor = commandExecutor;
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

        return commandExecutor.ExecuteAsync<ConversationService, Result<ConversationDto, CreateConversationError>>(
            (service, token) => service.CreateCommandAsync(request, actor, token),
            (service, _, token) => service.ValidateCreateAuthorityAsync(actor, token),
            () => new CreateConversationError.NotPermitted(),
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

        var facts = await tenantCommandFacts.ResolveAudienceAsync(expectedActor, participants, ct);
        if (facts is null
            || !permissionCatalog.Grants(facts.Actor.Role, TenantPermission.MessagesSend)
            || permissionCatalog.AudienceFor(facts.Actor.Role, TenantPermission.MessagesSend)
                != ResourceAudience.TenantResources)
            return new CreateConversationError.NotPermitted();
        if (!facts.ExistingTenantIds.SetEquals(participants))
            return new CreateConversationError.InvalidParticipants();

        var payloadHash = ResourceCommandReceipt.HashPayload(string.Join(",", participants));
        var receipt = await privilegedRepository.GetCreationReceiptForUpdateAsync(
            facts.Actor.TenantId,
            facts.Actor.MembershipId,
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
        var conversation = ConversationEntity.Create(participants, facts.Actor.TenantId, facts.Actor.UserId, at);
        privilegedRepository.Add(conversation);
        await privilegedRepository.SaveChangesAsync(ct);
        privilegedRepository.Add(ConversationCreationReceipt.Record(
            conversation.Id,
            facts.Actor.TenantId,
            facts.Actor.MembershipId,
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
        if (await repository.GetByIdAsync(conversationId, ct) is null)
            return new ConversationAccessError.NotFound(conversationId);
        var conversation = await privilegedRepository.GetWithGrantsByIdAsync(conversationId, ct)
            ?? throw new InvalidOperationException($"Conversation {conversationId} disappeared after authorization.");
        return await ToDtoAsync(conversation, ct);
    }

    public async Task<Result<IReadOnlyList<MessageDto>, ConversationAccessError>> GetMessagesAsync(
        int conversationId,
        CancellationToken ct = default)
    {
        if (await repository.GetByIdAsync(conversationId, ct) is null)
            return new ConversationAccessError.NotFound(conversationId);
        var messages = await messageRepository.GetByConversationIdAsync(conversationId, ct);
        var activeTenantId = tenantContext.GetTenantId();
        return messages.Select(message => ToDto(message, message.SenderTenantId != activeTenantId)).ToList();
    }

    public Task<Result<MessageDto, SendMessageError>> SendAsync(
        int conversationId,
        SendMessageRequest request,
        MessageAction? action = null,
        CancellationToken ct = default)
    {
        if (membership.Membership is not { } actor)
            return Task.FromResult<Result<MessageDto, SendMessageError>>(new SendMessageError.NotPermitted());
        return commandExecutor.ExecuteAsync<ConversationService, Result<MessageDto, SendMessageError>>(
            (service, token) => service.SendCommandAsync(conversationId, request, action, actor, token),
            (service, _, token) => service.ValidateAccessAsync(
                conversationId, actor, TenantPermission.MessagesSend, ConversationAccessScope.SendMessages, token),
            () => new SendMessageError.NotPermitted(),
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
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        if (actor is null
            || !permissionCatalog.Grants(actor.Role, TenantPermission.MessagesSend))
            return new SendMessageError.NotPermitted();
        var conversation = await privilegedRepository.GetWithGrantsByIdForUpdateAsync(conversationId, ct);
        if (conversation is null)
            return new SendMessageError.NotFound(conversationId);
        if (!Allows(conversation, actor, TenantPermission.MessagesSend, ConversationAccessScope.SendMessages))
            return new SendMessageError.NotPermitted();

        var payloadHash = ResourceCommandReceipt.HashPayload(request.Content, action);
        var replay = await privilegedRepository.GetMessageReceiptForUpdateAsync(
            conversationId, actor.MembershipId, request.RequestId, ct);
        if (replay is not null)
            return replay.PayloadHash == payloadHash ? ToDto(replay) : new SendMessageError.RequestConflict();

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
        return ToDto(message);
    }

    public Task<UnitResult<ConversationAccessError>> AdvanceReadPositionAsync(
        int conversationId,
        AdvanceConversationReadPositionRequest request,
        CancellationToken ct = default)
    {
        if (membership.Membership is not { } actor)
            return Task.FromResult<UnitResult<ConversationAccessError>>(new ConversationAccessError.NotPermitted());
        return commandExecutor.ExecuteAsync<ConversationService, UnitResult<ConversationAccessError>>(
            (service, token) => service.AdvanceReadPositionCommandAsync(conversationId, request, actor, token),
            (service, _, token) => service.ValidateAccessAsync(
                conversationId, actor, TenantPermission.MessagesRead, ConversationAccessScope.Read, token),
            () => new ConversationAccessError.NotPermitted(),
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
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        if (actor is null || !permissionCatalog.Grants(actor.Role, TenantPermission.MessagesRead))
            return new ConversationAccessError.NotPermitted();
        var conversation = await privilegedRepository.GetWithGrantsByIdForUpdateAsync(conversationId, ct);
        if (conversation is null)
            return new ConversationAccessError.NotFound(conversationId);
        if (!Allows(conversation, actor, TenantPermission.MessagesRead, ConversationAccessScope.Read))
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
        ChangeAssignmentAsync(conversationId, request.MembershipId, request.ExpectedAccessVersion, true, ct);

    public Task<UnitResult<AssignConversationMemberError>> RemoveMemberAssignmentAsync(
        int conversationId,
        Guid membershipId,
        CancellationToken ct = default) =>
        ChangeAssignmentAsync(conversationId, membershipId, null, false, ct);

    private Task<UnitResult<AssignConversationMemberError>> ChangeAssignmentAsync(
        int conversationId,
        Guid membershipId,
        long? expectedAccessVersion,
        bool assign,
        CancellationToken ct)
    {
        if (membership.Membership is not { } actor)
            return Task.FromResult<UnitResult<AssignConversationMemberError>>(
                new AssignConversationMemberError.NotPermitted());
        return commandExecutor.ExecuteAsync<ConversationService, UnitResult<AssignConversationMemberError>>(
            (service, token) => service.ChangeAssignmentCommandAsync(
                conversationId, membershipId, expectedAccessVersion, assign, actor, token),
            (service, _, token) => service.ValidatePrincipalAsync(conversationId, actor, token),
            () => new AssignConversationMemberError.NotPermitted(),
            ct);
    }

    private Task<UnitResult<AssignConversationMemberError>> ChangeAssignmentCommandAsync(
        int conversationId,
        Guid membershipId,
        long? expectedAccessVersion,
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
        long? expectedAccessVersion,
        bool assign,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var facts = await tenantCommandFacts.ResolveAsync(
            expectedActor, expectedActor.TenantId, membershipId, ct);
        if (facts is null
            || !permissionCatalog.Grants(facts.Actor.Role, TenantPermission.ResourcesShare))
            return new AssignConversationMemberError.NotPermitted();
        if (assign && facts.TargetMembership is null)
            return new AssignConversationMemberError.InvalidMembership();
        var conversation = await privilegedRepository.GetWithGrantsByIdForUpdateAsync(conversationId, ct);
        if (conversation is null)
            return new AssignConversationMemberError.NotFound(conversationId);
        if (!IsPrincipal(conversation, facts.Actor.TenantId))
            return new AssignConversationMemberError.NotPermitted();
        if (expectedAccessVersion is { } version && conversation.AccessVersion != version)
            return new AssignConversationMemberError.Superseded(conversationId);

        var changed = assign
            ? conversation.AssignMember(
                facts.Actor.TenantId,
                membershipId,
                facts.TargetMembership!.TenantId,
                timeProvider.GetUtcNow().UtcDateTime)
            : conversation.RemoveMemberAssignment(
                facts.Actor.TenantId,
                membershipId,
                timeProvider.GetUtcNow().UtcDateTime);
        if (!changed)
            return assign
                ? new AssignConversationMemberError.AlreadyAssigned()
                : new AssignConversationMemberError.InvalidMembership();
        await privilegedRepository.SaveChangesAsync(ct);
        await bus.PublishAsync(new ConversationChanged(conversationId), ct);
        return new Success();
    }

    public Task<int> GetUnreadCountAsync(CancellationToken ct = default) =>
        membership.Membership is { } actor
            ? messageRepository.GetUnreadCountAsync(actor.TenantId, actor.MembershipId, ct)
            : Task.FromResult(0);

    public async Task<IReadOnlyList<MessagePreviewDto>> GetRecentPreviewsAsync(CancellationToken ct = default)
    {
        if (membership.Membership is not { } actor)
            return [];
        var previews = await messageRepository.GetRecentPreviewsAsync(actor.TenantId, actor.MembershipId, ct);
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

    private static MessageDto ToDto(MessageEntity message, bool canReport = false) => new(
        message.Id,
        message.ConversationId,
        message.Sequence,
        message.SenderTenantId,
        message.SentByUserId,
        message.Content,
        message.SentAt,
        message.Action,
        canReport);

    private async Task<bool> ValidateCreateAuthorityAsync(
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        return actor is not null
               && permissionCatalog.Grants(actor.Role, TenantPermission.MessagesSend)
               && permissionCatalog.AudienceFor(actor.Role, TenantPermission.MessagesSend)
                   == ResourceAudience.TenantResources;
    }

    private async Task<bool> ValidateAccessAsync(
        int conversationId,
        MembershipSnapshot expectedActor,
        TenantPermission permission,
        ConversationAccessScope scope,
        CancellationToken ct)
    {
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        var conversation = await privilegedRepository.GetWithGrantsByIdAsync(conversationId, ct);
        return actor is not null
               && conversation is not null
               && permissionCatalog.Grants(actor.Role, permission)
               && Allows(conversation, actor, permission, scope);
    }

    private async Task<bool> ValidatePrincipalAsync(
        int conversationId,
        MembershipSnapshot expectedActor,
        CancellationToken ct)
    {
        var actor = await authorityFence.RequireCurrentAsync(expectedActor, ct);
        var conversation = await privilegedRepository.GetWithGrantsByIdAsync(conversationId, ct);
        return actor is not null
               && conversation is not null
               && permissionCatalog.Grants(actor.Role, TenantPermission.ResourcesShare)
               && IsPrincipal(conversation, actor.TenantId);
    }

    private bool Allows(
        ConversationEntity conversation,
        MembershipSnapshot actor,
        TenantPermission permission,
        ConversationAccessScope scope) =>
        ResourceGrantPolicy.Allows(
            conversation.AccessGrants,
            scope,
            actor,
            permissionCatalog.AudienceFor(actor.Role, permission),
            timeProvider.GetUtcNow().UtcDateTime);

    private static bool IsPrincipal(ConversationEntity conversation, Guid tenantId) =>
        conversation.AccessGrants.Any(grant =>
            grant.TenantId == tenantId
            && grant.Kind == ResourceGrantKind.Principal
            && grant.MembershipId is null
            && grant.Scope == ConversationAccessScope.Read
            && grant.RevokedAt is null);
}
