using Concertable.B2B.Contract.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// Renders a contract's terms as the human-readable sentence frozen onto a booking agreement
/// (and reused by its PDF). Deal terms only — payment method and event dates are separate
/// agreement fields.
/// </summary>
internal interface IAgreementTermsRenderer
{
    string Render(IContract contract);
}
