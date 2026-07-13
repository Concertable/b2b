using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Mappers;
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

    public async Task<BookingAgreementDto> GetByApplicationIdAsync(int applicationId)
    {
        var agreement = await repository.GetByApplicationIdAsync(applicationId)
            ?? throw new NotFoundException("Booking agreement not found");
        return agreement.ToDto();
    }

    public async Task<FileDownload> GetPdfByApplicationIdAsync(int applicationId)
    {
        var agreement = await repository.GetByApplicationIdAsync(applicationId)
            ?? throw new NotFoundException("Booking agreement not found");
        return agreement.ToFileDownload(await pdfService.GetOrCreateAsync(agreement));
    }

    public async Task<FileDownload> GetPdfByConcertIdAsync(int concertId)
    {
        var agreement = await repository.GetByConcertIdAsync(concertId)
            ?? throw new NotFoundException("Booking agreement not found");
        return agreement.ToFileDownload(await pdfService.GetOrCreateAsync(agreement));
    }
}
