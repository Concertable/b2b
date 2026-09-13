using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.ValueObjects;
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
    public void RecordPaymentVerification_Success_StoresAndRaisesVerifyPaymentSucceeded()
    {
        var application = CreateApplication();
        var payment = new SuccessfulPaymentVerification(application.Id);

        var recorded = application.RecordPaymentVerification(payment);

        Assert.True(recorded);
        Assert.Equal(payment, application.Verification);
        var raised = Assert.IsType<VerifyPaymentSucceededDomainEvent>(Assert.Single(application.DomainEvents)).Payment;
        Assert.Equal(payment.ApplicationId, raised.ApplicationId);
    }

    [Fact]
    public void RecordPaymentVerification_Failure_StoresAndRaisesVerifyPaymentFailed()
    {
        var application = CreateApplication();
        var payment = new FailedPaymentVerification(
            application.Id,
            new PaymentVerificationFailure("card_declined", "Declined"));

        application.RecordPaymentVerification(payment);

        Assert.Equal(payment, application.Verification);
        var raised = Assert.IsType<VerifyPaymentFailedDomainEvent>(Assert.Single(application.DomainEvents)).Payment;
        Assert.Equal(payment.ApplicationId, raised.ApplicationId);
        Assert.Equal(payment.Failure.Code, raised.Error.Code);
        Assert.Equal(payment.Failure.Message, raised.Error.Message);
    }

    [Fact]
    public void RecordPaymentVerification_DuplicateDelivery_DoesNotRaiseAgain()
    {
        var application = CreateApplication();
        var payment = new SuccessfulPaymentVerification(application.Id);
        application.RecordPaymentVerification(payment);
        application.ClearDomainEvents();

        var recorded = application.RecordPaymentVerification(
            new SuccessfulPaymentVerification(application.Id));

        Assert.False(recorded);
        Assert.Empty(application.DomainEvents);
    }

    [Fact]
    public void RecordPaymentVerification_FailureAfterSuccess_IsIgnored()
    {
        var application = CreateApplication();
        application.RecordPaymentVerification(new SuccessfulPaymentVerification(application.Id));
        application.ClearDomainEvents();

        var recorded = application.RecordPaymentVerification(new FailedPaymentVerification(
            application.Id,
            new PaymentVerificationFailure("card_declined", "Declined")));

        Assert.False(recorded);
        Assert.Empty(application.DomainEvents);
    }

    private static ApplicationEntity CreateApplication()
    {
        var application = ApplicationEntity.Create(
            11,
            12,
            DealType.DoorSplit,
            Guid.NewGuid(),
            Guid.NewGuid());
        typeof(ApplicationEntity).GetProperty(nameof(ApplicationEntity.Id))!.SetValue(application, 42);
        return application;
    }
}
