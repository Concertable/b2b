using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Identity;
using FluentResults;

namespace Concertable.B2B.Concert.Infrastructure.Validators;

internal sealed class ApplicationValidator : IApplicationValidator
{
    private readonly IConcertAvailability availability;
    private readonly IOpportunityRepository opportunityRepository;
    private readonly IApplicationRepository applicationRepository;
    private readonly IArtistModule artistModule;
    private readonly ICurrentUser currentUser;
    private readonly TimeProvider timeProvider;

    public ApplicationValidator(
        IConcertAvailability availability,
        IOpportunityRepository opportunityRepository,
        IApplicationRepository applicationRepository,
        IArtistModule artistModule,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        this.availability = availability;
        this.opportunityRepository = opportunityRepository;
        this.applicationRepository = applicationRepository;
        this.artistModule = artistModule;
        this.currentUser = currentUser;
        this.timeProvider = timeProvider;
    }

    public async Task<Result> CanApplyAsync(OpportunityEntity opportunity, int artistId)
    {
        var errors = new List<string>();

        if (opportunity.Period.Start < timeProvider.GetUtcNow())
            errors.Add("This concert opportunity has already passed");

        if (await availability.OpportunityHasConcertAsync(opportunity.Id))
            errors.Add("This concert opportunity has already been booked for a concert");

        if (await availability.ArtistHasConcertOnDateAsync(artistId, opportunity.Period.Start))
            errors.Add("You already have a concert on this day");

        return errors.Count > 0 ? Result.Fail(errors) : Result.Ok();
    }

    public async Task<Result> CanApplyAsync(int opportunityId)
    {
        var artistId = await artistModule.GetIdByUserIdAsync(currentUser.GetId());
        if (artistId is null)
            return Result.Fail("You must have an artist account to apply for a concert opportunity");

        var opportunity = await opportunityRepository.GetByIdAsync(opportunityId);
        if (opportunity is null)
            return Result.Fail("Concert opportunity does not exist");

        return await CanApplyAsync(opportunity, artistId.Value);
    }

    public async Task<Result> CanAcceptAsync(OpportunityEntity opportunity, ApplicationEntity application)
    {
        var errors = new List<string>();

        if (opportunity.Venue.UserId != currentUser.GetId())
            errors.Add("You do not own this concert opportunity");

        if (opportunity.Period.Start < timeProvider.GetUtcNow())
            errors.Add("This concert opportunity has already passed");

        if (await availability.OpportunityHasConcertAsync(opportunity.Id))
            errors.Add("This concert opportunity already has a concert booked");

        if (await availability.ArtistHasConcertOnDateAsync(application.ArtistId, opportunity.Period.Start))
            errors.Add("This artist already has a concert on this day");

        if (await availability.VenueHasConcertOnDateAsync(opportunity.VenueId, opportunity.Period.Start))
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
