namespace Concertable.B2B.Conversations.Infrastructure.Services;

internal sealed class ConversationsErasureService : IConversationsErasureService
{
    private const string ErasedPlaceholder = "[erased]";

    private readonly IMessagePrivilegedRepository messages;
    private readonly IParticipantProfilePrivilegedRepository participantProfiles;

    public ConversationsErasureService(
        IMessagePrivilegedRepository messages,
        IParticipantProfilePrivilegedRepository participantProfiles)
    {
        this.messages = messages;
        this.participantProfiles = participantProfiles;
    }

    public async Task SeverAuthoredMessagesAsync(Guid userId, CancellationToken ct = default)
    {
        var authored = await messages.ListBySenderUserAsync(userId, ct);
        if (authored.Count == 0)
            return;

        foreach (var message in authored)
            message.SeverAuthor();
        await messages.SaveChangesAsync(ct);
    }

    public async Task ScrubParticipantProfilesAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default)
    {
        if (tenantIds.Count == 0)
            return;

        var profiles = await participantProfiles.ListByTenantIdsAsync(tenantIds, ct);
        if (profiles.Count == 0)
            return;

        foreach (var profile in profiles)
            profile.Update(ErasedPlaceholder, ErasedPlaceholder, ErasedPlaceholder);
        await participantProfiles.SaveChangesAsync(ct);
    }
}
