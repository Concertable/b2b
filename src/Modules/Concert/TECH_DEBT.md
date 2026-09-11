# Concert tech debt

## Durable financial lifecycle operations

Accepting, withdrawing, or cancelling an application carries the HTTP request cancellation token through Payment's irreversible capture, deposit, or refund and into the later B2B lifecycle save. A disconnect or process failure after Payment succeeds can therefore leave money moved while the application remains in its previous state. A request-independent token does not close the process-failure window, and the existing Payment event cannot reconstruct the missing B2B transition.

Owner decision: authorize a separate cross-service B2B + Payment saga/package cut-over, or explicitly accept the unresolved financial/state inconsistency risk. The durable design must persist the lifecycle intent and intermediate state before the remote financial operation, stage a transactional outbox command, make Payment operations idempotent by booking, and reconcile pending work in a worker. It must cover cancellation after Payment succeeds.

Resolves when: the cross-service saga is implemented and verified with cancellation-after-payment and process-recovery tests, and application financial state can no longer diverge from Payment after request cancellation or service failure.

## Settlement outcome processors mutate before their inbox row (review IR19)

`SettlementPaymentProcessor` and `SettlementPaymentFailedProcessor` run the settlement mutation through
`ISettlementService` in its own transaction, then open a second one to check and write the inbox row. The
envelope-id inbox therefore gives that path no idempotency of its own; convergence rests on
`SettlementService`'s state check and `InvoiceIssuer`'s existence check. The two refund processors do it
correctly in one unit of work.

Owner decision: bring the settlement mutation and the inbox row under one `IOutboxUnitOfWorkBehavior`
scope, which means `ISettlementService.CompleteAsync` / `RecordFailureAsync` join the caller's transaction
rather than owning one through `IUnitOfWorkBoundary`.

Resolves when: a redelivered `PaymentSucceededEvent` for a completed settlement is rejected by the inbox row
before any settlement read, proven by a test that dispatches the same envelope twice and observes one
`CompleteAsync` call.

## `ReleaseEscrowCompleteStep`'s escrow-release-failure branch has no coverage (review IR18)

`FinishConcertError.EscrowReleaseFailure` is reachable when Payment rejects the release of a FlatFee or
VenueHire escrow, but no test drives it: the only failing escrow double was the never-called
`UseFailingPayment` test option, deleted with IR18.

Owner decision: a Concert integration test that finishes a past FlatFee concert against an
`IEscrowOperationsClient` whose `ReleaseAsync` fails, asserting the concert stays `AwaitingSettlement` with
the release failure recorded and no invoice issued.

Resolves when: that test exists and passes.

## `ContractIssuer.IssueAsync` throws for a missing application instead of returning a Result

`IssueAsync` returns plain `Task`, so both of its lookups end in `OrNotFound` and a missing application or opportunity leaves the method as a `NotFoundException`. Neither is an exceptional condition: contract issuing runs inside the acceptance flow, where the caller already decides between lifecycle outcomes, and `result-carriers` puts an outcome the caller must decide about in a `Result<T, TError>` rather than an exception. The exception also crosses the Infrastructure boundary untyped, so a caller cannot distinguish a missing application from a missing opportunity without catching and inspecting a message.

Owner decision: change `IssueAsync` to return `UnitResult<TError>` over a Concert error union covering both lookups, and adapt the acceptance path that calls it. The two `OrNotFound` calls become `OrFailure`, and the error surfaces through the existing lifecycle error terminal rather than an exception filter.

Resolves when: `IssueAsync` returns a Result, neither lookup throws, and a test proves a missing application yields the typed error rather than a `NotFoundException`.

