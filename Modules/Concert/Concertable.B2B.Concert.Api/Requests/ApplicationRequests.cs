namespace Concertable.B2B.Concert.Api.Requests;

internal sealed record ApplyRequest(string PaymentMethodId);

internal sealed record AcceptRequest(string PaymentMethodId);
