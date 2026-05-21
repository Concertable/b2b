using FluentResults;

namespace Concertable.Concert.Infrastructure.Validators;

internal class ApplicationValidator : IApplicationValidator
{
    private readonly IConcertRepository concertRepository;
    private readonly IOpportunityRepository opportunityRepository;
    private readonly IApplicationRepository applicationRepository;
    private readonly ICurrentUser currentUser;
    private readonly TimeProvider timeProvider;

    public ApplicationValidator(
        IConcertRepository concertRepository,
        IOpportunityRepository opportunityRepository,
        IApplicationRepository applicationRepository,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        this.concertRepository = concertRepository;
        this.opportunityRepository = opportunityRepository;
        this.applicationRepository = applicationRepository;
        this.currentUser = currentUser;
        this.timeProvider = timeProvider;
    }

    public async Task<Result> CanApplyAsync(OpportunityEntity opportunity, int artistId)
    {
        var errors = new List<string>();

        if (opportunity.Period.Start < timeProvider.GetUtcNow())
            errors.Add("This concert opportunity has already passed");

        if (await concertRepository.OpportunityHasConcertAsync(opportunity.Id))
            errors.Add("This concert opportunity has already been booked for a concert");

        if (await concertRepository.ArtistHasConcertOnDateAsync(artistId, opportunity.Period.Start))
            errors.Add("You already have a concert on this day");

        return errors.Count > 0 ? Result.Fail(errors) : Result.Ok();
    }

    public async Task<Result> CanApplyAsync(int opportunityId, int artistId)
    {
        var opportunity = await opportunityRepository.GetByIdAsync(opportunityId);

        if (opportunity is null)
            return Result.Fail("Concert opportunity does not exist");

        return await CanApplyAsync(opportunity, artistId);
    }

    public async Task<Result> CanAcceptAsync(OpportunityEntity opportunity, ApplicationEntity application)
    {
        var errors = new List<string>();

        if (application.Status == ApplicationStatus.Accepted)
            return Result.Fail("This application has already been accepted");

        if (opportunity.Venue.UserId != currentUser.GetId())
            errors.Add("You do not own this concert opportunity");

        if (opportunity.Period.Start < timeProvider.GetUtcNow())
            errors.Add("This concert opportunity has already passed");

        if (await concertRepository.OpportunityHasConcertAsync(opportunity.Id))
            errors.Add("This concert opportunity already has a concert booked");

        if (await concertRepository.ArtistHasConcertOnDateAsync(application.ArtistId, opportunity.Period.Start))
            errors.Add("This artist already has a concert on this day");

        if (await concertRepository.VenueHasConcertOnDateAsync(opportunity.VenueId, opportunity.Period.Start))
            errors.Add("You already have a concert on this day");

        return errors.Count > 0 ? Result.Fail(errors) : Result.Ok();
    }

    public async Task<Result> CanAcceptAsync(int applicationId)
    {
        var opportunity = await opportunityRepository.GetByApplicationIdAsync(applicationId);
        var application = await applicationRepository.GetByIdAsync(applicationId);

        if (opportunity is null)
            return Result.Fail("Concert opportunity does not exist");

        if (application is null)
            return Result.Fail("Concert application does not exist");

        return await CanAcceptAsync(opportunity, application);
    }
}
