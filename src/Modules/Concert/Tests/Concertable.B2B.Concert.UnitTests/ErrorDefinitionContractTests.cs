using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Domain.State;
using Concertable.Payment.Contracts.Errors;
using Reunion.Errors;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class ErrorDefinitionContractTests
{
    private static readonly ValidationErrors ValidationErrors =
        new([new("Field", "Validation failed.")]);

    public static TheoryData<IError, string, string, ErrorKind> Cases => new()
    {
        {
            new ConcertError.NotFound(42),
            "concert.get.not_found",
            "Concert 42 was not found.",
            ErrorKind.NotFound
        },
        {
            new ConcertError.ApplicationNotFound(42),
            "concert.get_by_application.not_found",
            "No concert was found for application 42.",
            ErrorKind.NotFound
        },
        {
            new CancelConcertError.ConcertNotFound(42),
            "concert.cancel.not_found",
            "Concert 42 was not found.",
            ErrorKind.NotFound
        },
        {
            new CancelConcertError.InvalidState(ConcertState.Complete),
            "concert.cancel.invalid_state",
            "A concert in Complete cannot be cancelled.",
            ErrorKind.Invalid
        },
        {
            new CancelConcertError.EscrowRefundFailure(new EscrowRefundError.Conflict()),
            "escrow.refund_conflict",
            "Another refund changed the refundable amount.",
            ErrorKind.Conflict
        },
        {
            new DeclareDoorRevenueError.ConcertNotFound(42),
            "concert.door_revenue.not_found",
            "Concert 42 was not found.",
            ErrorKind.NotFound
        },
        {
            new DeclareDoorRevenueError.VenueForbidden(),
            "concert.door_revenue.forbidden",
            "Only the concert's venue can declare its door revenue.",
            ErrorKind.Forbidden
        },
        {
            new DeclareDoorRevenueError.WrongDealType(),
            "concert.door_revenue.wrong_deal_type",
            "Door revenue can only be declared for a revenue-share concert.",
            ErrorKind.Invalid
        },
        {
            new DeclareDoorRevenueError.TooEarly(),
            "concert.door_revenue.too_early",
            "Door revenue can only be declared after the concert has ended.",
            ErrorKind.Invalid
        },
        {
            new DeclareDoorRevenueError.AlreadySettled(),
            "concert.door_revenue.already_settled",
            "Door revenue can only be declared before the concert has settled.",
            ErrorKind.Conflict
        },
        {
            new DeclareDoorRevenueError.Negative(),
            "declare.door_revenue_negative",
            "Door revenue must be zero or greater.",
            ErrorKind.Invalid
        },
        {
            new FinishConcertError.ConcertNotFound(42),
            "concert.finish.not_found",
            "Concert 42 was not found.",
            ErrorKind.NotFound
        },
        {
            new FinishConcertError.ConcertNotEnded(),
            "concert.finish.not_ended",
            "The concert cannot be finished before it has ended.",
            ErrorKind.Invalid
        },
        {
            new FinishConcertError.DoorRevenueRequired(),
            "concert.finish.door_revenue_required",
            "Door revenue must be declared before the concert can be finished.",
            ErrorKind.Invalid
        },
        {
            new FinishConcertError.InvalidState(ConcertState.Cancelled),
            "concert.finish.invalid_state",
            "A concert in Cancelled cannot be finished.",
            ErrorKind.Invalid
        },
        {
            new FinishConcertError.ManagerPaymentFailure(
                new ManagerPaymentError.PaymentFailure(new PaymentError.PaymentRejected())),
            "payment.rejected",
            "The payment was rejected.",
            ErrorKind.PaymentRequired
        },
        {
            new FinishConcertError.EscrowReleaseFailure(new EscrowReleaseError.EscrowNotHeld()),
            "escrow.release_not_held",
            "Only held escrow can be released.",
            ErrorKind.Conflict
        },
        {
            new GrantSelfBillingAgreementError.MissingTenant(),
            "self_billing.grant.missing_tenant",
            "No active organization was found for the current user.",
            ErrorKind.Forbidden
        },
        {
            new GrantSelfBillingAgreementError.TenantNotFound(Guid.Empty),
            "self_billing.grant.tenant_not_found",
            $"Tenant {Guid.Empty} was not found.",
            ErrorKind.NotFound
        },
        {
            new GrantSelfBillingAgreementError.MissingTaxCompliance(),
            "self_billing.grant.missing_tax_compliance",
            "Complete your tax details before granting a self-billing agreement.",
            ErrorKind.Invalid
        },
        {
            new GrantSelfBillingAgreementError.MissingUser(),
            "self_billing.grant.missing_user",
            "No user was found for the current request.",
            ErrorKind.Forbidden
        },
        {
            new InvoiceError.ConcertNotFound(42),
            "invoice.get_by_concert.not_found",
            "No invoice was found for concert 42.",
            ErrorKind.NotFound
        },
        {
            new PostConcertError.ConcertNotFound(42),
            "concert.post.not_found",
            "Concert 42 was not found.",
            ErrorKind.NotFound
        },
        {
            new SelfBillingAgreementPdfError.NotFound(),
            "self_billing.pdf.not_found",
            "Self-Billing Agreement not found",
            ErrorKind.NotFound
        },
        {
            new UpdateConcertError.ConcertNotFound(42),
            "concert.update.not_found",
            "Concert 42 was not found.",
            ErrorKind.NotFound
        }
    };

    public static TheoryData<IError, string, string> ValidationCases => new()
    {
        {
            new PostConcertError.Invalid(ValidationErrors),
            "concert.post.invalid",
            "The concert cannot be posted."
        },
        {
            new UpdateConcertError.Invalid(ValidationErrors),
            "concert.update.invalid",
            "The concert update is invalid."
        }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Definition_ErrorCase_ReturnsStableDefinition(
        IError error,
        string expectedCode,
        string expectedMessage,
        ErrorKind expectedKind)
    {
        var definition = error.Definition;

        Assert.Equal(expectedCode, definition.Code);
        Assert.Equal(expectedMessage, definition.Message);
        Assert.Equal(expectedKind, definition.Kind);
    }

    [Theory]
    [MemberData(nameof(ValidationCases))]
    public void Definition_ValidationCase_ReturnsStableDefinition(
        IError error,
        string expectedCode,
        string expectedMessage)
    {
        var definition = Assert.IsType<ValidationError>(error.Definition);

        Assert.Equal(expectedCode, definition.Code);
        Assert.Equal(expectedMessage, definition.Message);
        Assert.Equal(ErrorKind.Invalid, definition.Kind);
        Assert.Single(definition.Errors.Errors);
        Assert.Equal(["Validation failed."], definition.Errors.Errors["Field"]);
    }
}
