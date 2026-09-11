using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Mappers;

internal static class RegisteredAddressMappers
{
    extension(RegisteredAddressDto address)
    {
        /// <summary>The address as one comma-separated line, skipping blank parts — for inline display like
        /// the booking-confirmation email. Invoices keep the parts separate for their own layout, so this is
        /// the single-line presentation, not a canonical rendering.</summary>
        public string ToSingleLine() =>
            string.Join(", ", new[]
            {
                address.Line1,
                address.Line2,
                address.City,
                address.Postcode,
                address.Country
            }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
