using Concertable.B2B.Application.Application.Mappers;
using Concertable.B2B.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Services.Payment;

internal sealed class PaymentVerificationRecorder : IPaymentVerificationRecorder
{
    private readonly ApplicationPrivilegedDbContext context;
    private readonly IPrivilegedUnitOfWorkBehavior unitOfWorkBehavior;

    public PaymentVerificationRecorder(
        ApplicationPrivilegedDbContext context,
        IPrivilegedUnitOfWorkBehavior unitOfWorkBehavior)
    {
        this.context = context;
        this.unitOfWorkBehavior = unitOfWorkBehavior;
    }

    public Task RecordAsync(VerifyPayment payment, CancellationToken ct = default) =>
        unitOfWorkBehavior.ExecuteAsync(async () =>
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 SELECT 1
                 FROM application."Applications"
                 WHERE "Id" = {payment.ApplicationId}
                 FOR UPDATE
                 """,
                ct);
            var application = await context.Applications
                .Include(value => value.VerifyPayment)
                .SingleOrDefaultAsync(value => value.Id == payment.ApplicationId, ct)
                ?? throw new InvalidOperationException($"Application {payment.ApplicationId} was not found.");
            if (!application.RecordPaymentVerification(payment.ToPaymentVerification()))
                return;

            context.Entry(application).Property(value => value.State).IsModified = true;
        }, ct);
}
