using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Deal.Contracts.Enums;

namespace Concertable.B2B.Application.UnitTests;

public sealed class VerifyPaymentTests
{
    [Fact]
    public void VerifyPaymentError_BlankCode_ThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() => new VerifyPaymentError(" ", "Declined"));

    [Fact]
    public void VerifyPaymentError_BlankMessage_ThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() => new VerifyPaymentError("card_declined", " "));

    [Fact]
    public void RecordVerifyPayment_Success_StoresAndRaisesCaseSpecificFact()
    {
        var application = CreateApplication();
        var payment = new VerifyPaymentSucceeded(application.Id, "seti_123");

        application.RecordVerifyPayment(payment);

        Assert.Equal(payment, application.Verification);
        Assert.Same(payment, Assert.Single(application.DomainEvents));
    }

    [Fact]
    public void RecordVerifyPayment_DuplicateDelivery_DoesNotRaiseAgain()
    {
        var application = CreateApplication();
        var payment = new VerifyPaymentSucceeded(application.Id, "seti_123");
        application.RecordVerifyPayment(payment);
        application.ClearDomainEvents();

        application.RecordVerifyPayment(payment);

        Assert.Empty(application.DomainEvents);
    }

    [Fact]
    public void RecordVerifyPayment_ConflictingOutcomeForTransaction_ThrowsInvalidOperationException()
    {
        var application = CreateApplication();
        application.RecordVerifyPayment(new VerifyPaymentSucceeded(application.Id, "seti_123"));
        application.ClearDomainEvents();

        var action = () => application.RecordVerifyPayment(new VerifyPaymentFailed(
            application.Id,
            "seti_123",
            new VerifyPaymentError("card_declined", "Declined")));

        Assert.Throws<InvalidOperationException>(action);
        Assert.Empty(application.DomainEvents);
    }

    private static ApplicationEntity CreateApplication()
    {
        var application = StandardApplication.Create(
            11,
            12,
            DealType.DoorSplit,
            Guid.NewGuid(),
            Guid.NewGuid());
        typeof(ApplicationEntity).GetProperty(nameof(ApplicationEntity.Id))!.SetValue(application, 42);
        return application;
    }
}
