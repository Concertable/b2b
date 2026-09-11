using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Tenant.Contracts.Events;

// Wire-compatible with Payment's own Concertable.Payment.Contracts.Events.PayoutOwnerRegisteredEvent
// (same message type + shape) — deliberately not a shared type, so B2B never compile-depends on
// Payment.Contracts to publish it. Keep the two in sync by hand if either changes.
[MessageType("concertable.payment.payout-owner-registered.v1")]
public sealed record PayoutOwnerRegisteredEvent(
    Guid OwnerId,
    string Email) : IIntegrationEvent;
