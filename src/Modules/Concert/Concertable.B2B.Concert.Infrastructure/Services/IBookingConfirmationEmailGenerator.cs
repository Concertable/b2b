using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.ValueObjects;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal interface IBookingConfirmationEmailGenerator
{
    BookingConfirmationEmail Generate(BookingConfirmationParty venue, BookingConfirmationParty artist, DateRange period);
}

/// <summary>One counterparty on the booking-confirmation email. <see cref="TaxCompliance"/> is absent until the
/// tenant completes organization setup, so the address and VAT lines render only when it is present.</summary>
internal sealed record BookingConfirmationParty(string DisplayName, TenantDto Tenant, TaxComplianceDto? TaxCompliance);

internal sealed record BookingConfirmationEmail(string Subject, string Body);
