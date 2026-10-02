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

    public async Task<IReadOnlySet<Guid>> SeverMembershipsAsync(Guid userId, IReadOnlySet<Guid> capturedTenantIds, CancellationToken ct = default)
    {
        var memberships = await this.membershipRepository.ListMembershipsByUserAsync(userId, ct);
        var tenantIds = memberships.Select(m => m.TenantId).Concat(capturedTenantIds).ToHashSet();
        foreach (var membership in memberships)
            this.membershipRepository.Remove(membership);
        await this.membershipRepository.SaveChangesAsync(ct);

        var woundDown = new HashSet<Guid>();
        foreach (var tenantId in tenantIds)
        {
            if (await this.membershipRepository.CountMembersAsync(tenantId, ct) == 0)
                woundDown.Add(tenantId);
        }

        return woundDown;
    }

    public async Task PurgePendingInvitationsAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var invitations = await this.invitationRepository.ListPendingInvitationsByEmailAsync(normalized, ct);
        if (invitations.Count == 0)
            return;

        foreach (var invitation in invitations)
            this.invitationRepository.Remove(invitation);
        await this.invitationRepository.SaveChangesAsync(ct);
    }
}
