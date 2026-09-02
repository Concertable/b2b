# Concert tech debt

## Durable financial lifecycle operations

Accepting, withdrawing, or cancelling an application carries the HTTP request cancellation token through Payment's irreversible capture, deposit, or refund and into the later B2B lifecycle save. A disconnect or process failure after Payment succeeds can therefore leave money moved while the application remains in its previous state. A request-independent token does not close the process-failure window, and the existing Payment event cannot reconstruct the missing B2B transition.

Owner decision: authorize a separate cross-service B2B + Payment saga/package cut-over, or explicitly accept the unresolved financial/state inconsistency risk. The durable design must persist the lifecycle intent and intermediate state before the remote financial operation, stage a transactional outbox command, make Payment operations idempotent by booking, and reconcile pending work in a worker. It must cover cancellation after Payment succeeds.

Resolves when: the cross-service saga is implemented and verified with cancellation-after-payment and process-recovery tests, and application financial state can no longer diverge from Payment after request cancellation or service failure.
