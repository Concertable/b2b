using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Opportunity.Contracts;
using Reunion.Validation;

namespace Concertable.B2B.Application.Infrastructure.Validators;

internal sealed class ApplicationValidator : IApplicationValidator
{
    private readonly IApplicationAvailabilityProjection availabilityProjection;
    private readonly ITenantContext tenantContext;
    private readonly TimeProvider timeProvider;

    public ApplicationValidator(
        IApplicationAvailabilityProjection availabilityProjection,
        ITenantContext tenantContext,
        TimeProvider timeProvider)
    {
        this.availabilityProjection = availabilityProjection;
        this.tenantContext = tenantContext;
        this.timeProvider = timeProvider;
    }

    public async Task<ValidationResult> CanApplyAsync(
        OpportunityDto opportunity,
        int artistId,
        CancellationToken ct = default)
    {
        var errors = new List<string>();

        if (opportunity.StartDate < timeProvider.GetUtcNow())
            errors.Add("This concert opportunity has already passed");

        if (await availabilityProjection.OpportunityHasConcertAsync(opportunity.Id, ct))
            errors.Add("This concert opportunity has already been booked for a concert");

        if (await availabilityProjection.ArtistHasConcertOnDateAsync(artistId, opportunity.StartDate, ct))
            errors.Add("You already have a concert on this day");

        return ToValidationResult(errors);
    }

    public async Task<ValidationResult> CanAcceptAsync(
        OpportunityDto opportunity,
        ApplicationEntity application,
        CancellationToken ct = default)
    {
        var errors = new List<string>();

        if (opportunity.VenueTenantId != tenantContext.TenantId)
            errors.Add("You do not own this concert opportunity");

        if (!opportunity.IsOpen)
            errors.Add("This concert opportunity is no longer open");

        if (opportunity.StartDate < timeProvider.GetUtcNow())
            errors.Add("This concert opportunity has already passed");

        if (await availabilityProjection.OpportunityHasConcertAsync(opportunity.Id, ct))
            errors.Add("This concert opportunity already has a concert booked");

        if (await availabilityProjection.ArtistHasConcertOnDateAsync(application.ArtistId, opportunity.StartDate, ct))
            errors.Add("This artist already has a concert on this day");

        if (await availabilityProjection.VenueHasConcertOnDateAsync(opportunity.VenueId, opportunity.StartDate, ct))
            errors.Add("You already have a concert on this day");

        return ToValidationResult(errors);
    }

    private static ValidationResult ToValidationResult(IEnumerable<string> messages)
    {
        var errors = messages.ToArray();
        return errors.Length == 0
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(new ValidationErrors(
                new Dictionary<string, string[]> { ["application"] = errors }));
    }
}
