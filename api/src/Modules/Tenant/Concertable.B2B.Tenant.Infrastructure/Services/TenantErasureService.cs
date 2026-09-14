namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class TenantErasureService : ITenantErasureService
{
    private readonly IMembershipRepository membershipRepository;
    private readonly IInvitationRepository invitationRepository;

    public TenantErasureService(IMembershipRepository membershipRepository, IInvitationRepository invitationRepository)
    {
        this.membershipRepository = membershipRepository;
        this.invitationRepository = invitationRepository;
    }

    public async Task<IReadOnlyList<Guid>> SeverMembershipsAsync(Guid userId, CancellationToken ct = default)
    {
        var memberships = await membershipRepository.ListMembershipsByUserAsync(userId, ct);
        if (memberships.Count == 0)
            return [];

        var tenantIds = memberships.Select(m => m.TenantId).Distinct().ToList();
        foreach (var membership in memberships)
            membershipRepository.Remove(membership);
        await membershipRepository.SaveChangesAsync(ct);

        var woundDown = new List<Guid>();
        foreach (var tenantId in tenantIds)
        {
            if (await membershipRepository.CountMembersAsync(tenantId, ct) == 0)
                woundDown.Add(tenantId);
        }

        return woundDown;
    }

    public async Task PurgePendingInvitationsAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var invitations = await invitationRepository.ListPendingInvitationsByEmailAsync(normalized, ct);
        if (invitations.Count == 0)
            return;

        foreach (var invitation in invitations)
            invitationRepository.Remove(invitation);
        await invitationRepository.SaveChangesAsync(ct);
    }
}
