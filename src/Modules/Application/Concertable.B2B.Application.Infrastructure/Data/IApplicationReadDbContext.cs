namespace Concertable.B2B.Application.Infrastructure.Data;

internal interface IApplicationReadDbContext
{
    IQueryable<ApplicationEntity> Applications { get; }
    IQueryable<ConcertAvailabilityEntity> ConcertAvailabilities { get; }
    IQueryable<VerifyPaymentEntity> VerifyPayments { get; }
}
