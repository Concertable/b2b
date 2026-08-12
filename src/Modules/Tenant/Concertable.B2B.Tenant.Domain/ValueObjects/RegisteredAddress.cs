using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain.ValueObjects;

public sealed record RegisteredAddress
{
    public string Line1 { get; private init; } = null!;
    public string? Line2 { get; private init; }
    public string City { get; private init; } = null!;
    public string Postcode { get; private init; } = null!;
    public string Country { get; private init; } = null!;

    private RegisteredAddress() { }

    private RegisteredAddress(string line1, string? line2, string city, string postcode, string country)
    {
        Line1 = line1;
        Line2 = string.IsNullOrWhiteSpace(line2) ? null : line2;
        City = city;
        Postcode = postcode;
        Country = country;
    }

    public static Result<RegisteredAddress, ValidationErrors> Create(
        string line1,
        string? line2,
        string city,
        string postcode,
        string country)
    {
        var errors = new List<KeyValuePair<string, string>>();
        ValidateRequired(errors, nameof(Line1), line1, 200);

        if (line2?.Length > 200)
            errors.Add(new(nameof(Line2), "Line2 must be 200 characters or fewer."));

        ValidateRequired(errors, nameof(City), city, 100);
        ValidateRequired(errors, nameof(Postcode), postcode, 20);
        ValidateRequired(errors, nameof(Country), country, 100);

        return errors.Count == 0
            ? Result.Success<RegisteredAddress, ValidationErrors>(
                new RegisteredAddress(line1, line2, city, postcode, country))
            : Result.Failure<RegisteredAddress, ValidationErrors>(new ValidationErrors(errors));
    }

    private static void ValidateRequired(
        ICollection<KeyValuePair<string, string>> errors,
        string field,
        string value,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(new(field, $"{field} is required."));
        else if (value.Length > maximumLength)
            errors.Add(new(field, $"{field} must be {maximumLength} characters or fewer."));
    }
}
