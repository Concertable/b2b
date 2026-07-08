using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class BookingAgreementBuilder : IBookingAgreementBuilder
{
    private readonly IContractAccessor contractAccessor;
    private readonly IApplicationRepository applicationRepository;
    private readonly IBookingAgreementRepository agreementRepository;
    private readonly IAgreementTermsRenderer termsRenderer;
    private readonly ICurrentUser currentUser;
    private readonly IClientContext clientContext;
    private readonly LegalSettings legal;
    private readonly TimeProvider timeProvider;

    public BookingAgreementBuilder(
        IContractAccessor contractAccessor,
        IApplicationRepository applicationRepository,
        IBookingAgreementRepository agreementRepository,
        IAgreementTermsRenderer termsRenderer,
        ICurrentUser currentUser,
        IClientContext clientContext,
        IOptions<LegalSettings> legal,
        TimeProvider timeProvider)
    {
        this.contractAccessor = contractAccessor;
        this.applicationRepository = applicationRepository;
        this.agreementRepository = agreementRepository;
        this.termsRenderer = termsRenderer;
        this.currentUser = currentUser;
        this.clientContext = clientContext;
        this.legal = legal.Value;
        this.timeProvider = timeProvider;
    }

    public async Task BuildAsync(ApplicationEntity application, int bookingId)
    {
        var contract = contractAccessor.Contract;
        var (artist, venue) = await applicationRepository.GetArtistAndVenueByIdAsync(application.Id)
            ?? throw new NotFoundException("Application not found");

        var agreement = BookingAgreementEntity.Create(
            bookingId,
            venue.Id,
            venue.Name,
            artist.Id,
            artist.Name,
            application.Opportunity.Period,
            contract,
            termsRenderer.Render(contract),
            legal.PlatformTermsVersion,
            application.ArtistConsent,
            new Consent(
                currentUser.Id ?? throw new ForbiddenException("No user for current request"),
                timeProvider.GetUtcNow().UtcDateTime,
                clientContext.IpAddress,
                clientContext.UserAgent),
            timeProvider.GetUtcNow().UtcDateTime);
        agreement.VenueTenantId = application.VenueTenantId;
        agreement.ArtistTenantId = application.ArtistTenantId;
        agreement.AssignPdfBlobName($"agreements/{bookingId}-{Guid.NewGuid():N}.pdf");

        await agreementRepository.AddAsync(agreement);
    }
}
