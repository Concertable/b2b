using System.Globalization;
using Concertable.B2B.Concert.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Concertable.B2B.Concert.Infrastructure.Pdf;

/// <summary>
/// The self-billed VAT invoice, rendered from the immutable <see cref="InvoiceEntity"/> snapshot — never
/// from live tenant data. Carries the HMRC self-billing legends: the document is raised by the customer on
/// the supplier's behalf, and (when the supplier is VAT-registered) the VAT shown is the supplier's output
/// tax due to HMRC. Both parties' VAT numbers appear in their identity blocks. Mirrors <c>ContractDocument</c>.
/// </summary>
internal sealed class InvoiceDocument : IDocument
{
    private static readonly CultureInfo Gb = CultureInfo.GetCultureInfo("en-GB");

    private readonly InvoiceEntity invoice;

    public InvoiceDocument(InvoiceEntity invoice)
    {
        this.invoice = invoice;
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
                header.Item().Text("Self-Billed VAT Invoice").FontSize(22).Bold();
                header.Item().Text("SELF-BILLING").FontSize(12).SemiBold().FontColor(Colors.Red.Darken2);
                header.Item().Text($"Invoice number: {invoice.InvoiceNumber}").FontColor(Colors.Grey.Darken1);
                header.Item().Text($"Tax point: {FormatDate(invoice.TaxPointUtc)}").FontColor(Colors.Grey.Darken1);
                header.Item().Text($"Issued: {FormatUtc(invoice.CreatedAtUtc)}").FontColor(Colors.Grey.Darken1);
            });

            page.Content().PaddingVertical(20).Column(column =>
            {
                column.Spacing(16);

                Party(column, "Supplier (whom the invoice is raised for)", invoice.Supplier);
                Party(column, "Customer (who raises this invoice)", invoice.Customer);

                Section(column, "Supply", section =>
                {
                    Field(section, "Description", $"Booking settlement — {invoice.DealType}");
                    Field(section, "Net", Money(invoice.Amounts.Net));
                    Field(section, "VAT rate", invoice.Amounts.Rate.ToString("P0", CultureInfo.InvariantCulture));
                    Field(section, "VAT", Money(invoice.Amounts.Vat));
                    Field(section, "Total (gross)", Money(invoice.Amounts.Gross));
                });

                Section(column, "Self-billing", section =>
                {
                    section.Item().Text(
                        "This is a self-billed invoice raised by the customer on behalf of the supplier under a self-billing agreement.");
                    if (!string.IsNullOrWhiteSpace(invoice.Supplier.VatNumber))
                        section.Item().Text("The VAT shown is the supplier's output tax due to HMRC.").SemiBold();
                });
            });

            page.Footer().AlignCenter().Text(t =>
            {
                t.Span("Concertable — self-billed VAT invoice for a settled booking. ");
                t.Span(invoice.InvoiceNumber).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private static void Party(ColumnDescriptor column, string title, InvoiceParty party) =>
        Section(column, title, section =>
        {
            Field(section, "Name", party.LegalName);
            Field(section, "Address", FormatAddress(party));
            Field(section, "VAT number", party.VatNumber ?? "Not VAT registered");
        });

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

    private static string FormatAddress(InvoiceParty party)
    {
        var lines = new[] { party.AddressLine1, party.AddressLine2, party.City, party.Postcode, party.Country };
        return string.Join(", ", lines.Where(l => !string.IsNullOrWhiteSpace(l)));
    }

    private static string Money(decimal value) => value.ToString("C2", Gb);

    private static string FormatDate(DateTime value) =>
        value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatUtc(DateTime value) =>
        value.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);
}
