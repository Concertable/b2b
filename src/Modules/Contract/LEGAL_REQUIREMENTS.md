# Legal Requirements

- VAT registration status + VAT number stored on artist and venue profiles
- VAT calculation in settlement amounts (add/strip 20% based on seller's registration status)
- VAT-compliant invoice generation per settlement (sequential numbering, dated, all HMRC-required fields)
- PRS deduction at 3% of gross door (configurable platform-level constant, deducted before artist/venue split, recorded as separate line item)
- Cancellation flow with automatic escrow refund (terminal `Cancelled` stage on Application/Booking/Concert, refund triggers on transition)
