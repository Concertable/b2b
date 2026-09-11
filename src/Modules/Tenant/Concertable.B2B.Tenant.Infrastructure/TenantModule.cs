namespace Concertable.B2B.Tenant.Infrastructure;

internal sealed class TenantModule : ITenantModule
{
    private readonly ITenantService service;
    private readonly ITenantActivityService activityService;
    private readonly IVerificationService verificationService;

    public TenantModule(
        ITenantService service,
        ITenantActivityService activityService,
        IVerificationService verificationService)
    {
        this.service = service;
        this.activityService = activityService;
        this.verificationService = verificationService;
    }

    public Task<Option<TenantDto>> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        service.GetByIdAsync(id, ct);

    public Task<IReadOnlyList<MembershipDto>> GetMembershipsAsync(Guid userId, CancellationToken ct = default) =>
        service.GetMembershipsAsync(userId, ct);

    public Task<IReadOnlyList<Guid>> GetMemberUserIdsAsync(Guid tenantId, CancellationToken ct = default) =>
        service.GetMemberUserIdsAsync(tenantId, ct);

    public Task<bool> IsTaxComplianceCompleteAsync(Guid tenantId, CancellationToken ct = default) =>
        service.IsTaxComplianceCompleteAsync(tenantId, ct);

    public Task<bool> IsVerifiedAsync(Guid tenantId, CancellationToken ct = default) =>
        verificationService.IsVerifiedAsync(tenantId, ct);

    public Task<Option<TaxComplianceDto>> GetTaxComplianceAsync(Guid tenantId, CancellationToken ct = default) =>
        service.GetTaxComplianceAsync(tenantId, ct);

    public Task<Result<VatCalculation, VatCalculationError>> GetVatCalculationAsync(
        Guid tenantId,
        decimal gross,
        CancellationToken ct = default) =>
        service.GetVatCalculationAsync(tenantId, gross, ct);

    public Task<IReadOnlyList<ActivityItemDto>> GetRecentActivityAsync(
        Guid tenantId,
        int take,
        CancellationToken ct = default) =>
        activityService.GetRecentAsync(tenantId, take, ct);
}
