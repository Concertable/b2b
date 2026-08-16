using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Identity;
using Reunion.Validation;

namespace Concertable.B2B.Concert.Infrastructure.Validators;

internal sealed class ApplicationValidator : IApplicationValidator
{
    private readonly IConcertAvailability availability;
    private readonly ITenantContext tenantContext;
    private readonly TimeProvider timeProvider;

    public ApplicationValidator(
        IConcertAvailability availability,
        ITenantContext tenantContext,
        TimeProvider timeProvider)
    {
        this.availability = availability;
        this.tenantContext = tenantContext;
        this.timeProvider = timeProvider;
    }

    public async Task<ValidationResult> CanApplyAsync(OpportunityEntity opportunity, int artistId)
    {
        var errors = new List<string>();

        if (opportunity.Period.Start < timeProvider.GetUtcNow())
            errors.Add("This concert opportunity has already passed");

        if (await availability.OpportunityHasConcertAsync(opportunity.Id))
            errors.Add("This concert opportunity has already been booked for a concert");

        if (await availability.ArtistHasConcertOnDateAsync(artistId, opportunity.Period.Start))
            errors.Add("You already have a concert on this day");

        return ToValidationResult(errors);
    }

    public async Task<ValidationResult> CanAcceptAsync(OpportunityEntity opportunity, ApplicationEntity application)
    {
        var errors = new List<string>();

        if (opportunity.TenantId != tenantContext.TenantId)
            errors.Add("You do not own this concert opportunity");

        if (opportunity.Period.Start < timeProvider.GetUtcNow())
            errors.Add("This concert opportunity has already passed");

        if (await availability.OpportunityHasConcertAsync(opportunity.Id))
            errors.Add("This concert opportunity already has a concert booked");

        if (await availability.ArtistHasConcertOnDateAsync(application.ArtistId, opportunity.Period.Start))
            errors.Add("This artist already has a concert on this day");

        if (await availability.VenueHasConcertOnDateAsync(opportunity.VenueId, opportunity.Period.Start))
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
