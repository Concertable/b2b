using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Tax;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Identity;
using Reunion.Errors;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class TenantService : ITenantService
{
    private readonly ITenantRepository repository;
    private readonly IMembershipRepository membershipRepository;
    private readonly IInvitationRepository invitationRepository;
    private readonly ITenantContext tenantContext;
    private readonly IVatPolicy vatPolicy;
    private readonly IPermissionCatalog permissionCatalog;
    private readonly IOutboxUnitOfWorkBehavior unitOfWork;
    private readonly TimeProvider timeProvider;
    private readonly IReadOnlyList<ITenantDeletionGuard> deletionGuards;
    private readonly ICurrentUser currentUser;

    public TenantService(
        ITenantRepository repository,
        IMembershipRepository membershipRepository,
        IInvitationRepository invitationRepository,
        ITenantContext tenantContext,
        IVatPolicy vatPolicy,
        IPermissionCatalog permissionCatalog,
        IOutboxUnitOfWorkBehavior unitOfWork,
        TimeProvider timeProvider,
        IEnumerable<ITenantDeletionGuard> deletionGuards,
        ICurrentUser currentUser)
    {
        this.repository = repository;
        this.membershipRepository = membershipRepository;
        this.invitationRepository = invitationRepository;
        this.tenantContext = tenantContext;
        this.vatPolicy = vatPolicy;
        this.permissionCatalog = permissionCatalog;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
        this.deletionGuards = deletionGuards.ToList();
        this.currentUser = currentUser;
    }

    public async Task<Option<TenantDto>> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        (await repository.GetByIdAsync(id, ct)).ToOption().Map(tenant => tenant.ToDto());

    public async Task<IReadOnlyList<MembershipDto>> GetMembershipsAsync(Guid userId, CancellationToken ct = default)
    {
        var memberships = await membershipRepository.GetMembershipsAsync(userId, ct);
        return memberships
            .Select(m => new MembershipDto(
                m.MembershipId,
                m.TenantId,
                m.LegalName,
                m.Role,
                m.PermissionVersion,
                m.BusinessActivities,
                [.. permissionCatalog.For(m.Role).Select(permission => permission.Value)]))
            .ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetMemberUserIdsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var memberships = await membershipRepository.ListMembershipsByTenantAsync(tenantId, ct);
        return memberships.Select(m => m.UserId).ToList();
    }

    public Task<IReadOnlyList<MembershipSnapshot>> GetCurrentMembershipsAsync(
        IReadOnlyCollection<Guid> tenantIds,
        CancellationToken ct = default) =>
        membershipRepository.GetSnapshotsByTenantIdsAsync(tenantIds, ct);

    public Task<bool> IsCurrentMembershipAsync(Guid tenantId, Guid membershipId, CancellationToken ct = default) =>
        membershipRepository.ExistsByTenantIdAndIdAsync(tenantId, membershipId, ct);

    public async Task<Option<TenantBusinessDetails>> GetTenantBusinessDetailsAsync(Guid tenantId, CancellationToken ct = default) =>
        (await repository.GetTenantBusinessDetailsByTenantIdAsync(tenantId, ct)).ToOption();

    public Task<IReadOnlyList<TenantBusinessDetails>> GetTenantBusinessDetailsAsync(
        IReadOnlyCollection<Guid> tenantIds,
        CancellationToken ct = default) =>
        repository.GetTenantBusinessDetailsByTenantIdsAsync(tenantIds, ct);

    public Task<bool> HasBusinessActivityAsync(
        Guid tenantId,
        TenantBusinessActivityKind kind,
        CancellationToken ct = default) =>
        repository.HasActiveBusinessActivityAsync(tenantId, kind, ct);

    public async Task<Option<TenantDetails>> GetDetailsAsync(CancellationToken ct = default)
    {
        if (tenantContext.TenantId is not { } tenantId)
            return Option.None<TenantDetails>();

        return (await repository.GetByIdAsync(tenantId, ct)).ToOption().Map(ToDetails);
    }

    public Task<Result<TenantDetails, CreateTenantError>> CreateAsync(
        CreateTenantRequest request,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(() => CreateCoreAsync(request, ct), ct);

    private async Task<Result<TenantDetails, CreateTenantError>> CreateCoreAsync(
        CreateTenantRequest request,
        CancellationToken ct)
    {
        if (currentUser.Id is not { } userId)
            return new CreateTenantError.Unauthenticated();
        if (await repository.GetByCreatedByUserIdForCreationAsync(userId, ct) is not null)
            return new CreateTenantError.AlreadyOwnsTenant();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var tenant = TenantEntity.Create(request.DisplayName, request.ContactEmail, userId, now);
        foreach (var activity in request.Activities)
            tenant.ActivateBusinessActivity(activity, now);

        await repository.InsertAsync(tenant, ct);
        await membershipRepository.InsertAsync(
            TenantMembershipEntity.Create(tenant.Id, userId, TenantRole.Owner, null, now),
            ct);
        return ToDetails(tenant);
    }

    public Task<Result<TenantDetails, UpdateTenantError>> UpdateAsync(
        UpdateTenantRequest request,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(() => UpdateCoreAsync(request, ct), ct);

    private async Task<Result<TenantDetails, UpdateTenantError>> UpdateCoreAsync(
        UpdateTenantRequest request,
        CancellationToken ct)
    {
        var tenantId = tenantContext.GetTenantId();
        var tenant = await repository.GetByIdForAdministrationAsync(tenantId, ct);
        if (tenant is null)
            return new UpdateTenantError.TenantNotFound(tenantId);
        if (tenant.Version != request.ExpectedVersion)
            return new UpdateTenantError.Superseded();

        var validation = request.TaxCompliance.ToTaxCompliance()
            .Bind(taxCompliance => tenant
                .UpdateLegalDetails(request.LegalName, taxCompliance))
            .Bind(() => tenant.UpdateContactEmail(request.ContactEmail));
        if (validation.TryGetError(out var errors))
            return new UpdateTenantError.Invalid(errors);

        return ToDetails(tenant);
    }

    public Task<Result<TenantDetails, ChangeBusinessActivityError>> ActivateBusinessActivityAsync(
        TenantBusinessActivityKind kind,
        ChangeBusinessActivityRequest request,
        CancellationToken ct = default) =>
        ChangeBusinessActivityAsync(kind, request, activate: true, ct);

    public Task<Result<TenantDetails, ChangeBusinessActivityError>> RetireBusinessActivityAsync(
        TenantBusinessActivityKind kind,
        ChangeBusinessActivityRequest request,
        CancellationToken ct = default) =>
        ChangeBusinessActivityAsync(kind, request, activate: false, ct);

    private Task<Result<TenantDetails, ChangeBusinessActivityError>> ChangeBusinessActivityAsync(
        TenantBusinessActivityKind kind,
        ChangeBusinessActivityRequest request,
        bool activate,
        CancellationToken ct) =>
        unitOfWork.ExecuteAsync(async () =>
        {
            if (!Enum.IsDefined(kind))
            {
                return new ChangeBusinessActivityError.Invalid(
                    new ValidationErrors([new(nameof(kind), "The organization activity is invalid.")]));
            }

            var tenantId = tenantContext.GetTenantId();
            var tenant = await repository.GetByIdForAdministrationAsync(tenantId, ct);
            if (tenant is null)
                return new ChangeBusinessActivityError.TenantNotFound(tenantId);
            if (tenant.EligibilityVersion != request.ExpectedEligibilityVersion)
                return new ChangeBusinessActivityError.Superseded();

            var now = timeProvider.GetUtcNow().UtcDateTime;
            if (activate)
                tenant.ActivateBusinessActivity(kind, now);
            else
                tenant.RetireBusinessActivity(kind, now);

            return Result.Success<TenantDetails, ChangeBusinessActivityError>(ToDetails(tenant));
        }, ct);

    public Task<UnitResult<DeleteTenantError>> DeleteAsync(CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(() => DeleteCoreAsync(ct), ct);

    private async Task<UnitResult<DeleteTenantError>> DeleteCoreAsync(CancellationToken ct)
    {
        var tenantId = tenantContext.GetTenantId();
        var tenant = await repository.GetByIdForAdministrationAsync(tenantId, ct);
        if (tenant is null)
            return new DeleteTenantError.TenantNotFound(tenantId);

        foreach (var guard in deletionGuards)
        {
            if (await guard.HasLiveObligationsAsync(tenantId, ct))
                return new DeleteTenantError.CannotDeleteWithLiveObligations();
        }

        foreach (var membership in await membershipRepository.ListMembershipsByTenantAsync(tenantId, ct))
            membershipRepository.Remove(membership);

        foreach (var invitation in await invitationRepository.ListInvitationsByTenantAsync(tenantId, ct))
            invitationRepository.Remove(invitation);

        await repository.RemoveBusinessActivitiesByTenantIdAsync(tenantId, ct);

        repository.Remove(tenant);
        return new Success();
    }

    public async Task<bool> IsTaxComplianceCompleteAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await repository.GetByIdAsync(tenantId, ct);
        return tenant?.TaxCompliance is not null;
    }

    public async Task<Option<TaxComplianceDto>> GetTaxComplianceAsync(Guid tenantId, CancellationToken ct = default) =>
        (await repository.GetByIdAsync(tenantId, ct))
            .ToOption()
            .Bind(tenant => tenant.TaxCompliance.ToOption())
            .Map(compliance => compliance.ToDto());

    public async Task<Result<VatCalculation, VatCalculationError>> GetVatCalculationAsync(
        Guid tenantId,
        decimal gross,
        CancellationToken ct = default)
    {
        var tenant = await repository.GetByIdAsync(tenantId, ct);
        if (tenant is null)
            return new VatCalculationError.TenantNotFound(tenantId);

        var compliance = tenant.TaxCompliance
            ?? throw new InvalidOperationException(
                $"Tenant {tenantId} has no tax compliance; the settlement tax-gate should guarantee it by invoice time.");

        return vatPolicy.Apply(gross, compliance.VatNumber);
    }

    private TenantDetails ToDetails(TenantEntity tenant) => new()
    {
        Id = tenant.Id,
        LegalName = tenant.LegalName,
        ContactEmail = tenant.ContactEmail,
        Version = tenant.Version,
        EligibilityVersion = tenant.EligibilityVersion,
        BusinessActivities = [.. tenant.BusinessActivities.Where(activity => activity.IsActive).Select(activity => activity.Kind)],
        TaxCompliance = tenant.TaxCompliance?.ToDto(),
    };
}
