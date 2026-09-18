using Concertable.B2B.Tenant.Domain.Events;
using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain.Entities;

public sealed class TenantEntity : IGuidEntity, IEventRaiser
{
    private TenantEntity() { }

    public Guid Id { get; private set; }
    public string LegalName { get; private set; } = null!;

    /// <summary>
    /// Where this business is reached. Held separately from <see cref="LegalName"/> because setup replaces the
    /// name and a business with no marketplace profile has no other inbox to fall back to.
    /// </summary>
    public string ContactEmail { get; private set; } = null!;

    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Bumped by every change to what this tenant is eligible for. An authorised read re-checks the revision it
    /// resolved against, so a profile retired between resolution and query denies rather than serves.
    /// </summary>
    public long AuthorityVersion { get; private set; }

    /// <summary>
    /// The legal/tax identity backing settlement and tax reporting (<c>LEGAL_REQUIREMENTS.md</c> item 3).
    /// Null until the operator completes organization setup — provisioning creates the tenant bare.
    /// </summary>
    public TaxCompliance? TaxCompliance { get; private set; }

    private readonly List<TenantBusinessProfileEntity> businessProfiles = [];
    public IReadOnlyList<TenantBusinessProfileEntity> BusinessProfiles => businessProfiles;

    private readonly EventRaiser events = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => events.DomainEvents;
    public void ClearDomainEvents() => events.Clear();

    /// <summary>
    /// Creates a tenant from the operator's registration <paramref name="email"/> — the bare provisioning
    /// state before organization setup, with no marketplace profile activated. The email seeds both the
    /// placeholder <see cref="LegalName"/> and <see cref="ContactEmail"/>, and is carried on
    /// <see cref="TenantCreatedDomainEvent"/> as the Stripe account email, so Payment provisions off the
    /// resulting <c>PayoutOwnerRegisteredEvent</c>. <paramref name="id"/> lets seeders supply a deterministic
    /// id (so the event carries it, not a throwaway one); production omits it for a random id.
    /// </summary>
    public static TenantEntity Create(string email, Guid createdByUserId, DateTime createdAt, Guid? id = null)
    {
        var tenant = new TenantEntity
        {
            Id = id ?? Guid.NewGuid(),
            LegalName = email,
            ContactEmail = email,
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
            AuthorityVersion = 1,
        };
        tenant.events.Raise(new TenantCreatedDomainEvent(tenant.Id, createdByUserId, email));
        return tenant;
    }

    /// <summary>
    /// Re-raises <see cref="TenantCreatedDomainEvent"/> for an already-persisted tenant. The dev/E2E seeder
    /// inserts tenants directly (deterministic ids) with their create event cleared, so registration is the
    /// single provisioning trigger: <c>Announce</c> fires once the ASB subscriptions exist, where the seeder's
    /// own startup-time publish would race subscription creation and be dropped.
    /// </summary>
    public void Announce() => events.Raise(new TenantCreatedDomainEvent(Id, CreatedByUserId, ContactEmail));

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

        LegalName = legalName;
        TaxCompliance = taxCompliance;
        AuthorityVersion++;
        return new Success();
    }

    public UnitResult<ValidationErrors> UpdateContactEmail(string contactEmail)
    {
        if (string.IsNullOrWhiteSpace(contactEmail))
            return new ValidationErrors([new(nameof(ContactEmail), "ContactEmail is required.")]);

        if (contactEmail.Length > 320)
            return new ValidationErrors([new(nameof(ContactEmail), "ContactEmail must be 320 characters or fewer.")]);

        ContactEmail = contactEmail;
        return new Success();
    }

    public bool HasActiveProfile(TenantBusinessProfileKind kind) =>
        businessProfiles.Exists(profile => profile.Kind == kind && profile.IsActive);

    public void ActivateBusinessProfile(TenantBusinessProfileKind kind, DateTime at)
    {
        if (businessProfiles.Find(profile => profile.Kind == kind) is { } existing)
        {
            if (existing.IsActive)
                return;

            existing.Reactivate(at);
        }
        else
        {
            businessProfiles.Add(TenantBusinessProfileEntity.Create(Id, kind, at));
        }

        AuthorityVersion++;
    }

    public void RetireBusinessProfile(TenantBusinessProfileKind kind, DateTime at)
    {
        if (businessProfiles.Find(profile => profile.Kind == kind && profile.IsActive) is not { } active)
            return;

        active.Retire(at);
        AuthorityVersion++;
    }
}
