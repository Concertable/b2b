namespace Concertable.B2B.Concert.Api.Requests;

internal sealed record ApplyRequest(bool AgreedToTerms, string? PaymentMethodId = null);

internal sealed record AcceptRequest(bool AgreedToTerms, string? PaymentMethodId = null);
