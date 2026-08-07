using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Kernel.Errors;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class ErrorDefinitionContractTests
{
    private static readonly ValidationErrors ValidationErrors =
        new([new("Field", "Validation failed.")]);

    public static TheoryData<IError, string, string, ErrorKind> Cases => new()
    {
        {
            new ApplicationEligibilityError.MissingArtist(),
            "application.eligibility.missing_artist",
            "You must have an artist account to apply for a concert opportunity",
            ErrorKind.Forbidden
        },
        {
            new ApplicationEligibilityError.OpportunityNotFound(),
            "application.eligibility.opportunity_not_found",
            "Concert opportunity does not exist",
            ErrorKind.NotFound
        },
        {
            new ApplicationEligibilityError.ApplicationNotFound(),
            "application.eligibility.application_not_found",
            "Concert application does not exist",
            ErrorKind.NotFound
        },
        {
            new ApplicationError.NotFound(42),
            "application.get.not_found",
            "Application 42 was not found.",
            ErrorKind.NotFound
        },
        {
            new ApplyApplicationError.MissingArtist(),
            "application.apply.missing_artist",
            "You must create an artist account before applying for a concert opportunity.",
            ErrorKind.Forbidden
        },
        {
            new ApplyApplicationError.OpportunityNotFound(42),
            "application.apply.opportunity_not_found",
            "Concert opportunity 42 was not found.",
            ErrorKind.NotFound
        },
        {
            new ApplyApplicationError.AlreadyApplied(),
            "application.apply.duplicate",
            "You have already applied to this concert opportunity.",
            ErrorKind.Invalid
        },
        {
            new ApplyApplicationError.GenreMismatch(),
            "application.apply.genre_mismatch",
            "Your artist must share a genre with this concert opportunity.",
            ErrorKind.Invalid
        },
        {
            new ApplyApplicationError.UnsupportedDeal(DealType.FlatFee),
            "application.apply.unsupported_deal",
            "Deal FlatFee does not support applications.",
            ErrorKind.Invalid
        },
        {
            new ApplyApplicationError.MissingTenant(),
            "application.apply.missing_tenant",
            "No active organization was found for the current user.",
            ErrorKind.Forbidden
        },
        {
            new ApplyApplicationError.MissingUser(),
            "application.apply.missing_user",
            "No user was found for the current request.",
            ErrorKind.Forbidden
        },
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
            new ContractError.ApplicationNotFound(42),
            "contract.get_by_application.not_found",
            "No contract was found for application 42.",
            ErrorKind.NotFound
        },
        {
            new ContractError.ConcertNotFound(42),
            "contract.get_by_concert.not_found",
            "No contract was found for concert 42.",
            ErrorKind.NotFound
        },
        {
            new CreateConcertDraftError.BookingNotFound(42),
            "concert.draft.booking_not_found",
            "Booking 42 was not found.",
            ErrorKind.NotFound
        },
        {
            new CreateConcertDraftError.GenreMismatch(),
            "concert.draft.genre_mismatch",
            "The artist does not match any genres required by the concert opportunity.",
            ErrorKind.Invalid
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
            new InvoiceError.ConcertNotFound(42),
            "invoice.get_by_concert.not_found",
            "No invoice was found for concert 42.",
            ErrorKind.NotFound
        },
        {
            new OpportunityError.NotFound(42),
            "opportunity.get.not_found",
            "Opportunity 42 was not found.",
            ErrorKind.NotFound
        },
        {
            new OpportunityMutationError.VenueNotFound(),
            "opportunity.venue_not_found",
            "No venue was found for the current organization.",
            ErrorKind.NotFound
        },
        {
            new OpportunityMutationError.VenueForbidden(),
            "opportunity.venue_forbidden",
            "You do not own this venue.",
            ErrorKind.Forbidden
        },
        {
            new PostConcertError.ConcertNotFound(42),
            "concert.post.not_found",
            "Concert 42 was not found.",
            ErrorKind.NotFound
        },
        {
            new RejectApplicationError.ApplicationNotFound(42),
            "application.reject.not_found",
            "Application 42 was not found.",
            ErrorKind.NotFound
        },
        {
            new RejectApplicationError.InvalidTransition(LifecycleState.Applied, Trigger.Reject),
            "application.reject.invalid_transition",
            "Cannot Reject from Applied.",
            ErrorKind.Conflict
        },
        {
            new UpdateConcertError.ConcertNotFound(42),
            "concert.update.not_found",
            "Concert 42 was not found.",
            ErrorKind.NotFound
        },
        {
            new LifecycleTransitionError.ApplicationNotFound(42),
            "concert.lifecycle.application_not_found",
            "Application 42 was not found.",
            ErrorKind.NotFound
        },
        {
            new LifecycleTransitionError.InvalidTransition(LifecycleState.Applied, Trigger.Reject),
            "concert.lifecycle.invalid_transition",
            "Cannot Reject from Applied.",
            ErrorKind.Conflict
        }
    };

    public static TheoryData<IError, string, string> ValidationCases => new()
    {
        {
            new ApplicationEligibilityError.Invalid(ValidationErrors),
            "application.eligibility.invalid",
            "The application is not eligible."
        },
        {
            new ApplyApplicationError.Invalid(ValidationErrors),
            "application.apply.invalid",
            "The application is not eligible."
        },
        {
            new OpportunityMutationError.InvalidDeal(ValidationErrors),
            "opportunity.deal.invalid",
            "The opportunity deal is invalid."
        },
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
        var definition = Assert.IsType<ValidationErrorDefinition>(error.Definition);

        Assert.Equal(expectedCode, definition.Code);
        Assert.Equal(expectedMessage, definition.Message);
        Assert.Equal(ErrorKind.Invalid, definition.Kind);
        Assert.Single(definition.Errors);
        Assert.Equal(["Validation failed."], definition.Errors["Field"]);
    }
}
