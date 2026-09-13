namespace Concertable.B2B.Concert.Infrastructure.Services;

/// <summary>The HMRC self-billing clause text frozen into each agreement. Single source shared by the grant
/// service and the dev/E2E seeder so a seeded agreement carries the same wording a supplier would sign.</summary>
internal static class SelfBillingClause
{
    public static string Render(string supplierLegalName) =>
        $"""
        This self-billing agreement is made between Concertable (the customer) and {supplierLegalName} (the supplier).

        The supplier agrees:
        - to accept invoices raised by the customer on the supplier's behalf until the expiry of this agreement;
        - not to raise its own VAT invoices for the supplies covered by this agreement;
        - to notify the customer immediately if it changes its VAT registration number, ceases to be VAT registered, or transfers its business as a going concern.

        The customer agrees:
        - to raise self-billed invoices for all supplies made by the supplier under this agreement until it expires;
        - to state on each invoice that it is a self-billed invoice raised on the supplier's behalf;
        - to make a new self-billing agreement with the supplier if the VAT registration number changes.

        This agreement is reviewed at least every 12 months and self-billed invoices are only raised while it is in force.
        """;
}
