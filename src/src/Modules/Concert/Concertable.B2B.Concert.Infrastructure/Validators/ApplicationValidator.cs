using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Concert.Infrastructure.Validators;

internal sealed class ApplicationValidator : IApplicationValidator
{
    private readonly IConcertAvailability availability;
    private readonly IOpportunityRepository opportunityRepository;
    private readonly IApplicationRepository applicationRepository;
    private readonly IArtistModule artistModule;
    private readonly ITenantContext tenantContext;
    private readonly TimeProvider timeProvider;

    public ApplicationValidator(
        IConcertAvailability availability,
        IOpportunityRepository opportunityRepository,
        IApplicationRepository applicationRepository,
        IArtistModule artistModule,
        ITenantContext tenantContext,
        TimeProvider timeProvider)
    {
        this.availability = availability;
        this.opportunityRepository = opportunityRepository;
        this.applicationRepository = applicationRepository;
        this.artistModule = artistModule;
        this.tenantContext = tenantContext;
        this.timeProvider = timeProvider;
    }

    public async Task<UnitResult<ValidationErrors>> CanApplyAsync(OpportunityEntity opportunity, int artistId)
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

    public async Task<UnitResult<ApplicationEligibilityError>> CanApplyAsync(int opportunityId)
    {
        var artistId = await artistModule.GetIdForCurrentTenantAsync();
        if (!artistId.TryGetValue(out var value))
            return UnitResult.Failure(ApplicationEligibilityError.MissingArtist());

        var opportunity = await opportunityRepository.GetByIdAsync(opportunityId);
        if (opportunity is null)
            return UnitResult.Failure(ApplicationEligibilityError.OpportunityNotFound());

        return (await CanApplyAsync(opportunity, value))
            .MapError(ApplicationEligibilityError.Invalid);
    }

    public async Task<UnitResult<ValidationErrors>> CanAcceptAsync(OpportunityEntity opportunity, ApplicationEntity application)
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

    public async Task<UnitResult<ApplicationEligibilityError>> CanAcceptAsync(int applicationId)
    {
        var opportunity = await opportunityRepository.GetByApplicationIdAsync(applicationId);
        var application = await applicationRepository.GetByIdAsync(applicationId);

        if (opportunity is null)
            return UnitResult.Failure(ApplicationEligibilityError.OpportunityNotFound());

        if (application is null)
            return UnitResult.Failure(ApplicationEligibilityError.ApplicationNotFound());

        return (await CanAcceptAsync(opportunity, application))
            .MapError(ApplicationEligibilityError.Invalid);
    }

    private static UnitResult<ValidationErrors> ToValidationResult(IEnumerable<string> messages)
    {
        var errors = messages.ToArray();
        return errors.Length == 0
            ? UnitResult.Success<ValidationErrors>()
            : UnitResult.Failure(new ValidationErrors(
                new Dictionary<string, string[]> { ["application"] = errors }));
    }
}
