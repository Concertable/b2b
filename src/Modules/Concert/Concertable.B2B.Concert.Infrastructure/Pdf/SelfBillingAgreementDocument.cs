using System.Globalization;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Concertable.B2B.Concert.Infrastructure.Pdf;

/// <summary>
/// The human-readable self-billing agreement, rendered from the immutable <see cref="SelfBillingAgreementEntity"/>
/// snapshot — never from live tenant data. Records the supplier's frozen legal identity, the accepted/expiry
/// dates of the 12-month window, the platform terms version, the clause the supplier accepted, and the supplier's
/// e-signature. Mirrors <c>ContractDocument</c>.
/// </summary>
internal sealed class SelfBillingAgreementDocument : IDocument
{
    private readonly SelfBillingAgreementEntity agreement;
    private readonly ILogger logger;

    public SelfBillingAgreementDocument(SelfBillingAgreementEntity agreement, ILogger logger)
    {
        this.agreement = agreement;
        this.logger = logger;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(40);
            page.Size(PageSizes.A4);
            page.DefaultTextStyle(t => t.FontSize(11));

            page.Header().Column(header =>
            {
                header.Item().Text("Self-Billing Agreement").FontSize(22).Bold();
                header.Item().Text($"Reference: SB-{agreement.Id}").FontColor(Colors.Grey.Darken1);
                header.Item().Text($"Generated: {FormatUtc(agreement.CreatedAtUtc)}").FontColor(Colors.Grey.Darken1);
            });

            page.Content().PaddingVertical(20).Column(column =>
            {
                column.Spacing(16);

                Section(column, "Parties", section =>
                {
                    Field(section, "Customer", "Concertable");
                    Field(section, "Supplier", agreement.Supplier.LegalName);
                    Field(section, "Supplier address", FormatAddress(agreement.Supplier));
                    Field(section, "Supplier VAT number", agreement.Supplier.VatNumber ?? "Not VAT registered");
                });

                Section(column, "Review period", section =>
                {
                    Field(section, "Accepted", FormatUtc(agreement.AcceptedAtUtc));
                    Field(section, "Expires", FormatUtc(agreement.ExpiresAtUtc));
                    Field(section, "Platform terms version", agreement.PlatformTermsVersion);
                });

                Section(column, "Agreement", section =>
                    section.Item().PaddingTop(4).Text(agreement.ClauseText));

                Section(column, "Signature", section =>
                    Signature(section, "Supplier", agreement.SupplierESignature));
            });

            page.Footer().AlignCenter().Text(t =>
            {
                t.Span("Concertable — this agreement authorises the customer to raise self-billed invoices for the supplier. ");
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

    private void Signature(ColumnDescriptor section, string party, ESignature eSignature)
    {
        section.Item().PaddingTop(6).Column(block =>
        {
            block.Item().Text(party).SemiBold();

            block.Item().Text(t =>
            {
                t.Span("Signed by ");
                t.Span(eSignature.SignatoryName).SemiBold();
            });

            var drawn = DecodeDrawnSignature(party, eSignature.DrawnSignatureImage);
            if (drawn is not null)
                block.Item().PaddingVertical(2).Width(180).Image(drawn);

            var detail = $"{FormatUtc(eSignature.AtUtc)} · user {eSignature.UserId} · IP {eSignature.Ip}";
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

        try { return Convert.FromBase64String(payload); }
        catch (FormatException)
        {
            logger.DrawnSignatureDecodeFailed(agreement.Id, party);
            return null;
        }
    }

    private static string FormatAddress(InvoiceParty party)
    {
        var lines = new[] { party.AddressLine1, party.AddressLine2, party.City, party.Postcode, party.Country };
        return string.Join(", ", lines.Where(l => !string.IsNullOrWhiteSpace(l)));
    }

    private static string FormatUtc(DateTime value) =>
        value.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);
}
