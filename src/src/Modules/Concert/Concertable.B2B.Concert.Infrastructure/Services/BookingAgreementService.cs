using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class BookingAgreementService : IBookingAgreementService
{
    private readonly IBookingAgreementRepository repository;
    private readonly IBookingAgreementPdfService pdfService;

    public BookingAgreementService(
        IBookingAgreementRepository repository,
        IBookingAgreementPdfService pdfService)
    {
        this.repository = repository;
        this.pdfService = pdfService;
    }

    public async Task<BookingAgreementDto> GetByApplicationIdAsync(int applicationId) =>
        ToDto(await LoadForCallerAsync(applicationId));

    public async Task<AgreementPdf> GetPdfByApplicationIdAsync(int applicationId)
    {
        var agreement = await LoadForCallerAsync(applicationId);
        return await ToPdfAsync(agreement);
    }

    public async Task<AgreementPdf> GetPdfByConcertIdAsync(int concertId)
    {
        var agreement = await repository.GetByConcertIdAsync(concertId)
            ?? throw new NotFoundException("Booking agreement not found");
        return await ToPdfAsync(agreement);
    }

    private async Task<AgreementPdf> ToPdfAsync(BookingAgreementEntity agreement)
    {
        var bytes = await pdfService.GetOrCreateAsync(agreement);
        return new AgreementPdf(bytes, $"booking-agreement-BA-{agreement.Id}.pdf");
    }

    /* Tenant-filtered read: a non-party sees null and gets a 404, exactly like reading the
       application — the deal document never reveals its existence to a stranger. */
    private async Task<BookingAgreementEntity> LoadForCallerAsync(int applicationId) =>
        await repository.GetByApplicationIdAsync(applicationId)
            ?? throw new NotFoundException("Booking agreement not found");

    private static BookingAgreementDto ToDto(BookingAgreementEntity a) =>
        new(a.Id,
            a.VenueName,
            a.ArtistName,
            a.Period.Start,
            a.Period.End,
            a.ContractType,
            a.PaymentMethod,
            a.TermsText,
            a.PlatformTermsVersion,
            a.ArtistESignature is null ? null : new ESignatureDto(a.ArtistESignature.UserId, a.ArtistESignature.AtUtc, a.ArtistESignature.SignatoryName),
            new ESignatureDto(a.VenueESignature.UserId, a.VenueESignature.AtUtc, a.VenueESignature.SignatoryName),
            a.CreatedAtUtc);
}
