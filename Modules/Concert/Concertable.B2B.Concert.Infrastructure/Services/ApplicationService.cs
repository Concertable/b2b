using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Conversations.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Shared.Email.Application;
using Concertable.Kernel.Enums;
using Concertable.Kernel.Exceptions;
using Concertable.B2B.User.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal class ApplicationService : IApplicationService
{
    private readonly IApplicationRepository applicationRepository;
    private readonly ICurrentUser currentUser;
    private readonly IApplicationValidator applicationValidator;
    private readonly IConversationsModule conversationsModule;
    private readonly IEmailSender emailSender;
    private readonly IOpportunityService opportunityService;
    private readonly IOpportunityRepository opportunityRepository;
    private readonly IArtistModule artistModule;
    private readonly IUserModule userModule;
    private readonly IApplyDispatcher applyDispatcher;
    private readonly IAcceptanceDispatcher acceptanceDispatcher;
    private readonly ICheckoutDispatcher checkoutDispatcher;
    private readonly IApplicationMapper mapper;

    public ApplicationService(
        IApplicationRepository applicationRepository,
        ICurrentUser currentUser,
        IApplicationValidator applicationValidator,
        IConversationsModule conversationsModule,
        IEmailSender emailSender,
        IOpportunityService opportunityService,
        IOpportunityRepository opportunityRepository,
        IArtistModule artistModule,
        IUserModule userModule,
        IApplyDispatcher applyDispatcher,
        IAcceptanceDispatcher acceptanceDispatcher,
        ICheckoutDispatcher checkoutDispatcher,
        IApplicationMapper mapper)
    {
        this.applicationRepository = applicationRepository;
        this.currentUser = currentUser;
        this.applicationValidator = applicationValidator;
        this.conversationsModule = conversationsModule;
        this.emailSender = emailSender;
        this.opportunityService = opportunityService;
        this.opportunityRepository = opportunityRepository;
        this.artistModule = artistModule;
        this.userModule = userModule;
        this.applyDispatcher = applyDispatcher;
        this.acceptanceDispatcher = acceptanceDispatcher;
        this.checkoutDispatcher = checkoutDispatcher;
        this.mapper = mapper;
    }

    public async Task<IEnumerable<ApplicationDto>> GetByOpportunityIdAsync(int id)
    {
        var response = await opportunityService.OwnsOpportunityAsync(id);

        if (!response)
            throw new ForbiddenException("You do not own this Concert Opportunity");

        var applications = await applicationRepository.GetByOpportunityIdAsync(id);

        return await mapper.ToDtosAsync(applications);
    }

    public async Task<IEnumerable<ApplicationDto>> GetPendingForArtistAsync()
    {
        var artistId = await artistModule.GetIdByUserIdAsync(currentUser.GetId())
            ?? throw new ForbiddenException("You must have an Artist account");
        var applications = await applicationRepository.GetPendingByArtistIdAsync(artistId);
        return await mapper.ToDtosAsync(applications);
    }

    public async Task<IEnumerable<ApplicationDto>> GetRecentDeniedForArtistAsync()
    {
        var artistId = await artistModule.GetIdByUserIdAsync(currentUser.GetId())
            ?? throw new ForbiddenException("You must have an Artist account");
        var applications = await applicationRepository.GetRecentDeniedByArtistIdAsync(artistId);
        return await mapper.ToDtosAsync(applications);
    }

    public async Task<ApplicationDto> ApplyAsync(int opportunityId)
    {
        var artistId = await ResolveArtistIdAsync();
        var opportunityOwner = await ValidateAndLoadOwnerAsync(opportunityId, artistId);

        var application = await applyDispatcher.ApplyAsync(opportunityId, artistId);
        await NotifyAppliedAsync(opportunityOwner);

        return await GetByIdAsync(application.Id);
    }

    public async Task<ApplicationDto> ApplyAsync(int opportunityId, string paymentMethodId)
    {
        var artistId = await ResolveArtistIdAsync();
        var opportunityOwner = await ValidateAndLoadOwnerAsync(opportunityId, artistId);

        var application = await applyDispatcher.ApplyAsync(opportunityId, artistId, paymentMethodId);
        await NotifyAppliedAsync(opportunityOwner);

        return await GetByIdAsync(application.Id);
    }

    private async Task<int> ResolveArtistIdAsync() =>
        await artistModule.GetIdByUserIdAsync(currentUser.GetId())
            ?? throw new ForbiddenException("You must create an Artist account before you apply for a concert opportunity");

    private async Task<ManagerDto> ValidateAndLoadOwnerAsync(int opportunityId, int artistId)
    {
        var opportunityOwnerId = await opportunityRepository.GetOwnerByIdAsync(opportunityId)
            ?? throw new NotFoundException("Concert Opportunity owner not found");
        var opportunityOwner = await userModule.GetManagerByIdAsync(opportunityOwnerId)
            ?? throw new NotFoundException("Venue manager not found for opportunity owner");
        var opportunity = await opportunityRepository.GetByIdAsync(opportunityId)
            ?? throw new NotFoundException("Concert Opportunity not found");

        var result = await applicationValidator.CanApplyAsync(opportunity, artistId);
        if (result.IsFailed)
            throw new BadRequestException(result.Errors);

        var artistGenres = await artistModule.GetGenresAsync(artistId);
        var opportunityGenres = opportunity.Genres.ToHashSet();

        if (opportunityGenres.Count > 0 && !artistGenres.Overlaps(opportunityGenres))
            throw new BadRequestException("You need to have the same genres as the Concert Opportunity to be able to apply to it");

        return opportunityOwner;
    }

    private async Task NotifyAppliedAsync(ManagerDto opportunityOwner)
    {
        await conversationsModule.SendAsync(
            fromUserId: currentUser.GetId(),
            toUserId: opportunityOwner.Id,
            content: $"{currentUser.Email} has applied to your concert opportunity",
            action: MessageAction.ApplicationReceived);

        await emailSender.SendEmailAsync(opportunityOwner.Email!, "Concert Application", $"{currentUser.Email} has applied to your concert opportunity");
    }

    public async Task<Checkout> ApplyCheckoutAsync(int opportunityId)
    {
        var artistId = await ResolveArtistIdAsync();
        var result = await applicationValidator.CanApplyAsync(opportunityId, artistId);
        if (result.IsFailed)
            throw new BadRequestException(result.Errors);

        return await checkoutDispatcher.ApplyCheckoutAsync(opportunityId);
    }

    public Task<Checkout> AcceptCheckoutAsync(int applicationId) =>
        checkoutDispatcher.AcceptCheckoutAsync(applicationId);

    public async Task AcceptAsync(int applicationId, string? paymentMethodId)
    {
        var result = await applicationValidator.CanAcceptAsync(applicationId);

        if (result.IsFailed)
            throw new BadRequestException(result.Errors);

        await acceptanceDispatcher.AcceptAsync(applicationId, paymentMethodId);
        await NotifyAcceptedAsync(applicationId);
    }

    private async Task NotifyAcceptedAsync(int applicationId)
    {
        var (artist, venue) = await applicationRepository.GetArtistAndVenueByIdAsync(applicationId)
            ?? throw new NotFoundException("Concert application not found");

        await conversationsModule.SendAndNotifyAsync(
            fromUserId: venue.UserId,
            toUserId: artist.UserId,
            content: "Your application has been accepted!",
            action: MessageAction.ApplicationAccepted);

        await emailSender.SendEmailAsync(artist.Email!, "Concert Application Accepted", "Your application was accepted! A concert has been scheduled for you.");
    }

    public async Task<(ArtistReadModel, VenueReadModel)?> GetArtistAndVenueByIdAsync(int id) =>
        await applicationRepository.GetArtistAndVenueByIdAsync(id);

    public async Task<ApplicationDto> GetByIdAsync(int id)
    {
        var application = await applicationRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Application not found");
        return await mapper.ToDtoAsync(application);
    }
}
