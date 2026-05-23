using Concertable.Shared.Imaging.Application;
using Concertable.Venue.Application.Requests;
using FluentValidation;

namespace Concertable.Venue.Application.Validators;

internal class CreateVenueRequestValidator : AbstractValidator<CreateVenueRequest>
{
    public CreateVenueRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.About)
            .MaximumLength(1000);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180);

        RuleFor(x => x.Banner)
            .NotNull()
            .SetValidator(new BannerImageValidator());
    }
}

internal class UpdateVenueRequestValidator : AbstractValidator<UpdateVenueRequest>
{
    public UpdateVenueRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.About)
            .MaximumLength(1000);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180);

        When(x => x.Banner != null, () =>
        {
            RuleFor(x => x.Banner!.File)
                .SetValidator(new BannerImageValidator());
        });

        When(x => x.Avatar != null, () =>
        {
            RuleFor(x => x.Avatar!)
                .SetValidator(new AvatarImageValidator());
        });
    }
}
