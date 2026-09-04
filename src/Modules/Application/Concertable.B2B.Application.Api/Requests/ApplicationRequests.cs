using Concertable.B2B.Application.Application.Requests;

namespace Concertable.B2B.Application.Api.Requests;

/* The e-signature object replaces the old agreedToTerms bool — its presence IS the consent.
   Identity/time/IP are stamped server-side; the client supplies only the name (+ optional drawing). */
internal sealed record ApplyRequest(ESignatureRequest ESignature);

internal sealed record AcceptRequest(ESignatureRequest ESignature);
