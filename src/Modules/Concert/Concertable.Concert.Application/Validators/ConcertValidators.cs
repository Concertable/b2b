using Concertable.Concert.Application.Requests;
using FluentValidation;

namespace Concertable.Concert.Application.Validators;

internal class UpdateConcertRequestValidator : AbstractValidator<UpdateConcertRequest>
{
    public UpdateConcertRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.About).MaximumLength(1000);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TotalTickets).GreaterThanOrEqualTo(0);
    }
}

internal class BookingParamsValidator : AbstractValidator<BookingParams>
{
    public BookingParamsValidator()
    {
        RuleFor(x => x.PaymentMethodId).NotEmpty().WithMessage("Payment method ID is required");
        RuleFor(x => x.ApplicationId).GreaterThan(0).WithMessage("Application ID is required");
    }
}
