namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// The fail-closed self-billing gate: whether a supplier tenant holds a current (in-force) self-billing
/// agreement, answered by explicit tenant id with no tenant filter. Called from <c>FinishExecutor</c> for a
/// supplier who is not the request tenant, and from the tenant-less hourly completion sweep — mirroring how
/// <c>ITenantModule.IsTaxComplianceCompleteAsync</c> answers the tax gate.
/// </summary>
internal interface ISelfBillingAgreementGate
{
    Task<bool> HasCurrentAsync(Guid supplierTenantId, DateTime nowUtc, CancellationToken ct = default);
}
