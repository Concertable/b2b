using System.Reflection;
using Concertable.Shared.Email.Application;

namespace Concertable.B2B.Concert.Infrastructure.Emails;

internal sealed class BookingConfirmationEmailContent : IEmailContent
{
    private static readonly string TemplateSource = LoadTemplate("BookingConfirmation.mjml");

    public BookingConfirmationEmailContent(EmailParty venue, EmailParty artist, string when)
    {
        this.Venue = venue;
        this.Artist = artist;
        this.When = when;
    }

    public EmailParty Venue { get; }
    public EmailParty Artist { get; }
    public string When { get; }

    public string Subject => $"Booking confirmed: {Artist.DisplayName} at {Venue.DisplayName}";
    public string Template => TemplateSource;

    private static string LoadTemplate(string fileName)
    {
        var assembly = typeof(BookingConfirmationEmailContent).Assembly;
        var resource = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith(fileName, StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

/// <summary>
/// One counterparty's trading details for the booking-confirmation email. <see cref="Vat"/> and
/// <see cref="Address"/> are null until the tenant completes tax-compliance setup, so the template omits
/// those lines. Values are raw; the template HTML-escapes them.
/// </summary>
internal sealed record EmailParty(string DisplayName, string LegalName, string? Vat, string? Address);
