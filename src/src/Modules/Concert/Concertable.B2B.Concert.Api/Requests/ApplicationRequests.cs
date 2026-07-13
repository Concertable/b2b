using Concertable.B2B.Concert.Application.Requests;

namespace Concertable.B2B.Concert.Api.Requests;

/* The e-signature object replaces the old agreedToTerms bool — its presence IS the consent.
   Identity/time/IP are stamped server-side; the client supplies only the name (+ optional drawing). */
internal sealed record ApplyRequest(ESignatureRequest ESignature, string? PaymentMethodId = null);

internal sealed record AcceptRequest(ESignatureRequest ESignature, string? PaymentMethodId = null);
