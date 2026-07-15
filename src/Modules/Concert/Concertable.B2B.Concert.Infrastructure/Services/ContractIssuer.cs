using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ContractIssuer : IContractIssuer
{
    private readonly IDealAccessor dealAccessor;
    private readonly IApplicationRepository applicationRepository;
    private readonly IContractRepository contractRepository;
    private readonly IDealTermsRenderer termsRenderer;
    private readonly ICurrentUser currentUser;
    private readonly IClientContext clientContext;
    private readonly LegalSettings legal;
    private readonly TimeProvider timeProvider;

    public ContractIssuer(
        IDealAccessor dealAccessor,
        IApplicationRepository applicationRepository,
        IContractRepository contractRepository,
        IDealTermsRenderer termsRenderer,
        ICurrentUser currentUser,
        IClientContext clientContext,
        IOptions<LegalSettings> legal,
        TimeProvider timeProvider)
    {
        this.dealAccessor = dealAccessor;
        this.applicationRepository = applicationRepository;
        this.contractRepository = contractRepository;
        this.termsRenderer = termsRenderer;
        this.currentUser = currentUser;
        this.clientContext = clientContext;
        this.legal = legal.Value;
        this.timeProvider = timeProvider;
    }

    public async Task IssueAsync(ApplicationEntity application, int bookingId, ESignatureRequest venueESignature)
    {
        var deal = dealAccessor.Deal;
        var (artist, venue) = await applicationRepository.GetArtistAndVenueByIdAsync(application.Id)
            .OrNotFound(DisplayNames.Application);

        var contract = ContractEntity.Create(
            bookingId,
            venue.Id,
            venue.Name,
            artist.Id,
            artist.Name,
            application.Opportunity.Period,
            deal,
            termsRenderer.Render(deal),
            legal.PlatformTermsVersion,
            application.ArtistESignature,
            new ESignature(
                currentUser.Id ?? throw new ForbiddenException("No user for current request"),
                timeProvider.GetUtcNow().UtcDateTime,
                clientContext.IpAddress,
                clientContext.UserAgent,
                venueESignature.SignatoryName,
                venueESignature.DrawnSignatureImage),
            timeProvider.GetUtcNow().UtcDateTime);
        contract.VenueTenantId = application.VenueTenantId;
        contract.ArtistTenantId = application.ArtistTenantId;

        await contractRepository.AddAsync(contract);
    }
}
