using System.Globalization;
using System.Net;
using System.Text;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.ValueObjects;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class BookingConfirmationEmailGenerator : IBookingConfirmationEmailGenerator
{
    public BookingConfirmationEmail Generate(BookingConfirmationParty venue, BookingConfirmationParty artist, DateRange period)
    {
        var subject = $"Booking confirmed: {artist.DisplayName} at {venue.DisplayName}";
        var when = period.Start.ToString("dddd d MMMM yyyy", CultureInfo.InvariantCulture);

        var body = new StringBuilder()
            .Append("<div style=\"font-family: Arial, sans-serif;\">")
            .Append("<h1>Booking confirmed</h1>")
            .Append($"<p>{Encode(artist.DisplayName)} is confirmed to perform at {Encode(venue.DisplayName)} on {Encode(when)}.</p>")
            .Append("<h2>Legal trading details</h2>")
            .Append(RenderParty("Venue", venue))
            .Append(RenderParty("Artist", artist))
            .Append("</div>")
            .ToString();

        return new BookingConfirmationEmail(subject, body);
    }

    private static string RenderParty(string role, BookingConfirmationParty party)
    {
        var block = new StringBuilder()
            .Append("<h3>").Append(Encode(role)).Append("</h3>")
            .Append("<p>").Append(Encode(party.Tenant.LegalName));

        if (party.TaxCompliance is { } tax)
        {
            block.Append("<br>").Append(Encode(FormatAddress(tax.RegisteredAddress)));

            if (!string.IsNullOrWhiteSpace(tax.VatNumber))
                block.Append("<br>VAT number: ").Append(Encode(tax.VatNumber));
        }

        return block.Append("</p>").ToString();
    }

    private static string FormatAddress(RegisteredAddressDto address) =>
        string.Join(", ", new[]
        {
            address.Line1,
            address.Line2,
            address.City,
            address.Postcode,
            address.Country
        }.Where(part => !string.IsNullOrWhiteSpace(part)));

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
