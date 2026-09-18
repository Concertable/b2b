using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IApplicationPrivilegedRepository
{
    Task AddAsync(ApplicationEntity application, CancellationToken ct = default);

    Task<bool> ExistsByOpportunityIdAndArtistTenantIdAsync(
        int opportunityId,
        Guid artistTenantId,
        CancellationToken ct = default);

    Task<ApplicationEntity?> GetByIdForUpdateAsync(
        int applicationId,
        CancellationToken ct = default);

    Task<ApplicationEntity?> GetDecisionByIdForUpdateAsync(
        int applicationId,
        CancellationToken ct = default);

    Task<ApplicationState?> GetStateByIdAsync(
        int applicationId,
        CancellationToken ct = default);

    Task<Guid?> GetVenueTenantIdAsync(
        int applicationId,
        CancellationToken ct = default);

    Task<int?> GetOpportunityIdAsync(
        int applicationId,
        CancellationToken ct = default);

    void MarkChanged(ApplicationEntity application);

    Task<bool> AnyAcceptedByOpportunityIdAsync(
        int opportunityId,
        CancellationToken ct = default);

    Task<IReadOnlyList<ApplicationEntity>> RejectAllExceptAsync(
        int opportunityId,
        int applicationId,
        CancellationToken ct = default);

    Task<bool> OpportunityHasConcertAsync(int opportunityId, CancellationToken ct = default);

    Task<bool> ArtistHasConcertOnDateAsync(
        int artistId,
        DateTime date,
        CancellationToken ct = default);

    Task<bool> VenueHasConcertOnDateAsync(
        int venueId,
        DateTime date,
        CancellationToken ct = default);

    Task<bool> CanSubmitAsync(
        int applicationId,
        MembershipSnapshot actor,
        ResourceAudience audience,
        DateTime at,
        CancellationToken ct = default);

    Task<bool> CanDecideAsync(
        int applicationId,
        MembershipSnapshot actor,
        ResourceAudience audience,
        DateTime at,
        CancellationToken ct = default);
}
