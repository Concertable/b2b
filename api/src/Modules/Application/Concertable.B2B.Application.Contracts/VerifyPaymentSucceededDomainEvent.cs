using Concertable.Kernel;

namespace Concertable.B2B.Application.Contracts;

public sealed record VerifyPaymentSucceededDomainEvent(VerifyPaymentSucceeded Payment) : IDomainEvent;
