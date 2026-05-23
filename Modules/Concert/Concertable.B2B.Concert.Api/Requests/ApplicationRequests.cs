namespace Concertable.B2B.Concert.Api.Requests;

internal record ApplyRequest(string PaymentMethodId);

internal record AcceptRequest(string PaymentMethodId);
