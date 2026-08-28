using Concertable.B2B.Application.Domain.ValueObjects;
using Concertable.Kernel;

namespace Concertable.B2B.Application.Domain.Events;

internal sealed record PaymentVerificationRecordedDomainEvent(PaymentVerification Verification) : IDomainEvent;
