using Concertable.B2B.Tenant.Domain.Events;
using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain.Entities;

public sealed class TenantEntity : IGuidEntity, IEventRaiser
{
    private TenantEntity() { }

    public Guid Id { get; private set; }
    public string LegalName { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public long DisplayVersion { get; private set; }
    public long Version { get; private set; }
    public string EffectiveDisplayName => TaxCompliance is null ? DisplayName : LegalName;

    public string ContactEmail { get; private set; } = null!;

    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public long EligibilityVersion { get; private set; }

    /// <summary>
    /// The legal/tax identity backing settlement and tax reporting (<c>LEGAL_REQUIREMENTS.md</c> item 3).
    /// Null until the operator completes organization setup — provisioning creates the tenant bare.
    /// </summary>
    public TaxCompliance? TaxCompliance { get; private set; }

    private readonly List<TenantBusinessActivityEntity> businessActivities = [];
    public IReadOnlyList<TenantBusinessActivityEntity> BusinessActivities => businessActivities;

    private readonly EventRaiser events = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => events.DomainEvents;
    public void ClearDomainEvents() => events.Clear();

    public static TenantEntity Create(
        string displayName,
        string contactEmail,
        Guid createdByUserId,
        DateTime createdAt,
        Guid? id = null)
    {
        var tenant = new TenantEntity
        {
            Id = id ?? Guid.NewGuid(),
            LegalName = displayName,
            DisplayName = displayName,
            DisplayVersion = 1,
            Version = 1,
            ContactEmail = contactEmail,
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
            EligibilityVersion = 1,
        };
        tenant.events.Raise(new TenantCreatedDomainEvent(tenant.Id, createdByUserId, contactEmail));
        tenant.events.Raise(new TenantDisplayChangedDomainEvent(tenant));
        return tenant;
    }

    public void Announce()
    {
        // Registration must announce after ASB subscriptions exist; seeder startup can race their creation.
        events.Raise(new TenantCreatedDomainEvent(Id, CreatedByUserId, ContactEmail));
        events.Raise(new TenantDisplayChangedDomainEvent(this));
    }

    /// <summary>
    /// Tenant setup: replaces the provisioning placeholder legal name (the registration email)
    /// and the tax-compliance details in one transition — the organization form submits them together.
    /// </summary>
    public UnitResult<ValidationErrors> UpdateLegalDetails(string legalName, TaxCompliance taxCompliance)
    {
        var errors = new List<KeyValuePair<string, string>>();

        if (string.IsNullOrWhiteSpace(legalName))
            errors.Add(new(nameof(LegalName), "LegalName is required."));
        else if (legalName.Length > 200)
            errors.Add(new(nameof(LegalName), "LegalName must be 200 characters or fewer."));

        if (taxCompliance is null)
            errors.Add(new(nameof(TaxCompliance), "TaxCompliance is required."));

        if (errors.Count > 0)
            return new ValidationErrors(errors);

        var previousDisplayName = EffectiveDisplayName;
        LegalName = legalName;
        TaxCompliance = taxCompliance;
        Version++;
        if (EffectiveDisplayName != previousDisplayName)
        {
            DisplayVersion++;
            events.Raise(new TenantDisplayChangedDomainEvent(this));
        }
        return new Success();
    }

    public UnitResult<ValidationErrors> UpdateContactEmail(string contactEmail)
    {
        if (string.IsNullOrWhiteSpace(contactEmail))
            return new ValidationErrors([new(nameof(ContactEmail), "ContactEmail is required.")]);

        if (contactEmail.Length > 320)
            return new ValidationErrors([new(nameof(ContactEmail), "ContactEmail must be 320 characters or fewer.")]);

        if (ContactEmail != contactEmail)
        {
            ContactEmail = contactEmail;
            Version++;
        }
        return new Success();
    }

    public bool HasActiveActivity(TenantBusinessActivityKind kind) =>
        businessActivities.Exists(activity => activity.Kind == kind && activity.IsActive);

    public void ActivateBusinessActivity(TenantBusinessActivityKind kind, DateTime at)
    {
        if (businessActivities.Find(activity => activity.Kind == kind) is { } existing)
        {
            if (existing.IsActive)
                return;

            existing.Reactivate(at);
        }
        else
        {
            businessActivities.Add(TenantBusinessActivityEntity.Create(Id, kind, at));
        }

        EligibilityVersion++;
        Version++;
    }

    public void RetireBusinessActivity(TenantBusinessActivityKind kind, DateTime at)
    {
        if (businessActivities.Find(activity => activity.Kind == kind && activity.IsActive) is not { } active)
            return;

        active.Retire(at);
        EligibilityVersion++;
        Version++;
    }
}
