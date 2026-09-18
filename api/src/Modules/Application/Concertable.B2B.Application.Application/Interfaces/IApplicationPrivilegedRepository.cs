using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.Lifecycle;

namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IApplicationPrivilegedRepository
{
    Task<ApplicationEntity?> GetByIdForUpdateAsync(
        int applicationId,
        CancellationToken ct = default);

    Task<ApplicationEntity?> GetDecisionByIdForUpdateAsync(
        int applicationId,
        CancellationToken ct = default);

    Task<ApplicationState?> GetStateByIdAsync(
        int applicationId,
        CancellationToken ct = default);

    Task<bool> AnyAcceptedByOpportunityIdAsync(
        int opportunityId,
        CancellationToken ct = default);

    Task<IReadOnlyList<int>> RejectAllExceptAsync(
        int opportunityId,
        int applicationId,
        CancellationToken ct = default);
}
