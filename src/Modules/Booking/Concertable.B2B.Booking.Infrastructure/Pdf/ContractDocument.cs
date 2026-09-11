using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Domain.Entities;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Concertable.B2B.Booking.Infrastructure.Pdf;

internal sealed class ContractDocument : IDocument
{
    private readonly ContractEntity contract;
    private readonly ILogger logger;

    public ContractDocument(ContractEntity contract, ILogger logger)
    {
        this.contract = contract;
        this.logger = logger;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(40);
            page.Size(PageSizes.A4);
            page.DefaultTextStyle(text => text.FontSize(11));

            page.Header().Column(header =>
            {
                header.Item().Text("Contract").FontSize(22).Bold();
                header.Item().Text($"Reference: C-{contract.Id}").FontColor(Colors.Grey.Darken1);
                header.Item().Text($"Generated: {FormatUtc(contract.CreatedAtUtc)}")
                    .FontColor(Colors.Grey.Darken1);
            });

            page.Content().PaddingVertical(20).Column(column =>
            {
                column.Spacing(16);
                Section(column, "Parties", section =>
                {
                    Field(section, "Venue", contract.VenueName);
                    Field(section, "Artist", contract.ArtistName);
                });
                Section(column, "Event", section =>
                {
                    Field(section, "From", FormatUtc(contract.Period.Start));
                    Field(section, "To", FormatUtc(contract.Period.End));
                });
                Section(column, "Terms", section =>
                {
                    Field(section, "Contract type", contract.DealType.ToString());
                    Field(section, "Payment method", contract.PaymentMethod.ToString());
                    section.Item().PaddingTop(4).Text(contract.TermsText);
                    Field(section, "Platform terms version", contract.PlatformTermsVersion);
                });
                Section(column, "Signatures", section =>
                {
                    Signature(section, "Artist", contract.ArtistSignature);
                    Signature(section, "Venue", contract.VenueSignature);
                });
            });

            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("Concertable - this contract records the terms both parties e-signed. ");
                text.Span($"Platform terms {contract.PlatformTermsVersion}.")
                    .FontColor(Colors.Grey.Darken1);
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

    private void Signature(ColumnDescriptor section, string party, Signature signature)
    {
        section.Item().PaddingTop(6).Column(block =>
        {
            block.Item().Text(party).SemiBold();
            block.Item().Text(text =>
            {
                text.Span("Signed by ");
                text.Span(signature.SignatoryName).SemiBold();
            });

            var drawn = DecodeDrawnSignature(party, signature.DrawnSignatureImage);
            if (drawn is not null)
                block.Item().PaddingVertical(2).Width(180).Image(drawn);

            var detail = $"{FormatUtc(signature.AtUtc)} - user {signature.UserId} - IP {signature.Ip}";
            block.Item().Text(detail).FontSize(9).FontColor(Colors.Grey.Darken1);
        });
    }

    private byte[]? DecodeDrawnSignature(string party, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var payload = value;
        var comma = payload.IndexOf(',');
        if (payload.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0)
            payload = payload[(comma + 1)..];

        try
        {
            return Convert.FromBase64String(payload);
        }
        catch (FormatException)
        {
            logger.DrawnSignatureDecodeFailed(contract.Id, party);
            return null;
        }
    }

    private static string FormatUtc(DateTime value) =>
        value.ToString("yyyy-MM-dd HH:mm 'UTC'", System.Globalization.CultureInfo.InvariantCulture);
}
