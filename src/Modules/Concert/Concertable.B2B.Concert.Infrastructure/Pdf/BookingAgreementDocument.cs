using Concertable.B2B.Concert.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Concertable.B2B.Concert.Infrastructure.Pdf;

/// <summary>
/// The human-readable booking agreement, rendered from the immutable <see cref="BookingAgreementEntity"/>
/// snapshot — never from the live contract. Mirrors the QuestPDF <c>IDocument</c> precedent
/// (Customer's TicketReceiptDocument): plain data in via the ctor, one <see cref="Compose"/>.
/// </summary>
internal sealed class BookingAgreementDocument : IDocument
{
    private readonly BookingAgreementEntity agreement;

    public BookingAgreementDocument(BookingAgreementEntity agreement) => this.agreement = agreement;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(40);
            page.Size(PageSizes.A4);
            page.DefaultTextStyle(t => t.FontSize(11));

            page.Header().Column(header =>
            {
                header.Item().Text("Booking Agreement").FontSize(22).Bold();
                header.Item().Text($"Reference: BA-{agreement.Id}").FontColor(Colors.Grey.Darken1);
                header.Item().Text($"Generated: {FormatUtc(agreement.CreatedAtUtc)}").FontColor(Colors.Grey.Darken1);
            });

            page.Content().PaddingVertical(20).Column(column =>
            {
                column.Spacing(16);

                Section(column, "Parties", section =>
                {
                    Field(section, "Venue", agreement.VenueName);
                    Field(section, "Artist", agreement.ArtistName);
                });

                Section(column, "Event", section =>
                {
                    Field(section, "From", FormatUtc(agreement.Period.Start));
                    Field(section, "To", FormatUtc(agreement.Period.End));
                });

                Section(column, "Terms", section =>
                {
                    Field(section, "Contract type", agreement.ContractType.ToString());
                    Field(section, "Payment method", agreement.PaymentMethod.ToString());
                    section.Item().PaddingTop(4).Text(agreement.TermsText);
                    Field(section, "Platform terms version", agreement.PlatformTermsVersion);
                });

                Section(column, "Consent", section =>
                {
                    Consent(section, "Artist", agreement.ArtistESignature);
                    Consent(section, "Venue", agreement.VenueESignature);
                });
            });

            page.Footer().AlignCenter().Text(t =>
            {
                t.Span("Concertable — this agreement records the terms both parties click-wrapped. ");
                t.Span($"Platform terms {agreement.PlatformTermsVersion}.").FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private static void Section(ColumnDescriptor column, string title, Action<ColumnDescriptor> body)
    {
        column.Item().Column(section =>
        {
            section.Spacing(4);
            section.Item().Text(title).FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
            body(section);
        });
    }

    private static void Field(ColumnDescriptor section, string label, string value)
    {
        section.Item().Row(row =>
        {
            row.ConstantItem(160).Text(label).SemiBold();
            row.RelativeItem().Text(value);
        });
    }

    private static void Consent(ColumnDescriptor section, string party, ESignature? eSignature)
    {
        if (eSignature is null)
        {
            Field(section, party, "No recorded consent (predates click-wrap)");
            return;
        }

        var detail = $"agreed {FormatUtc(eSignature.AtUtc)} · user {eSignature.UserId}";
        if (!string.IsNullOrWhiteSpace(eSignature.Ip))
            detail += $" · IP {eSignature.Ip}";
        Field(section, party, detail);
    }

    private static string FormatUtc(DateTime value) =>
        value.ToString("yyyy-MM-dd HH:mm 'UTC'", System.Globalization.CultureInfo.InvariantCulture);
}
