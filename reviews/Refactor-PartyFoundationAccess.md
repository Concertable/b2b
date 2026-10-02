# Code review — Refactor/PartyFoundationAccess



> **This file is a work order, not a discussion.** Fix open `[ ]` findings directly and record their disposition.



**Review status:** `complete`
**Reviewed up to commit:** `b6109c043c3a65b91225c5a077993f4999b887c6`  `(2026-09-28)`

**Judgment:** `changes-requested`
**Security-reviewed up to commit:** `b6109c043c3a65b91225c5a077993f4999b887c6`  `(2026-09-28)`



## Review pass — 2026-09-27 — staged



**Candidate base:** `9bf08dcf1c3dc11e6642faf77e2e8ced35026a16`

**Candidate head:** `498119854487c485d5f4e476ce2fa729051ace1c`

**Candidate branch:** `Refactor/PartyFoundationAccess`

**Candidate scope:** `all`

**Candidate path-set:** `sha256:101c0c2516a991020de37e0b542603ce0b79466778e19986ec90f38bc9472f4a` `(543 paths)`

**Candidate patch:** `sha256:eee3c2c1f6f626e22e9b01ca01cc0b272d969a0c9001b3de6bbf96abcb90b8c5`

**Candidate bundle:** `C:/Users/TommySeery/source/repos/Concertable/b2b/.git/agent-workflow/runs/party-access-review-20260927/review/5744e829ee5ce8fd6847d5e19a20eb7a6bc95ce17d43cb9c242de967b64be58c`

**Candidate bundle identity:** `sha256:2b97de42a8f91e165c03666577eaec2f9247f02e9151915a9f9d442a9df01965`

**Work-order path:** `reviews/Refactor-PartyFoundationAccess.md`

**Work-order mode:** `new`

**Pass judgment:** `changes-requested`

**Synchronization:** `origin` fetched once; parent `9bf08dcf1` unchanged; candidate was clean and already based on parent.



### Findings



- [x] **A1 — P1 — tenant authority** — `api/src/Modules/Tenant/Concertable.B2B.Tenant.Infrastructure/Services/TenantService.cs:135`
  Tenant update, activity, and deletion operations trust request-time membership after waiting for the tenant lock. Revalidate membership identity, permission version, and permission under the lock.
- [x] **A2 — P1 — deletion obligations** — `api/src/Modules/Tenant/Concertable.B2B.Tenant.Infrastructure/Services/TenantService.cs:202`
  Accepted bookings and frozen contracts exist before a concert. Add a Booking deletion guard so tenant deletion cannot orphan them.
- [x] **A3 — P2 — validation** — `api/src/Modules/Tenant/Concertable.B2B.Tenant.Application/Validators/TenantValidators.cs:24`
  Null activities reach a duplicate validator that dereferences the collection and returns a 500. Stop validation after the null failure.
- [x] **A4 — P2 — tenant hydration** — `app/shared/src/features/tenant/hooks/useTenant.ts:17`
  The hook resolves an empty loading fallback and clears the persisted tenant choice. Resolve only after the identity query has completed.
- [x] **A5 — P1 — tenant cache isolation** — `app/web/shared/src/features/tenant/hooks/useTenant.ts:40`
  Tenant switching clears only keys starting with `tenant`; the legacy tenant-scoped mailbox and venue draft query keys survive. Clear all non-identity queries during the switch.
- [x] **A6 — P2 — mailbox refresh** — `app/web/artist/src/features/notifications/hooks/useArtistNotifications.ts:11`
  Artist and venue notification hooks removed `MessageReceived` invalidation while both still mount the legacy mailbox. Restore session-guarded invalidation.
- [x] **A7 — P2 — mobile navigation** — `app/mobile/src/navigation/BusinessNavigator.tsx:66`
  Authenticated business tabs omit ProfileStack, leaving no profile edit, location, or sign-out route. Restore the profile tab.
- [x] **A8 — P2 — failed switch recovery** — `app/web/shared/src/features/tenant/hooks/useTenant.ts:40`
  A failed tenant selection rolls back session state but leaves SignalR stopped. Restart notifications on failure.
- [x] **A9 — P2 — payment alert ordering** — `api/src/Modules/Application/Concertable.B2B.Application.Infrastructure/Services/Payment/VerifyPaymentFailedProcessor.cs:99`
  External failure alerts are sent before the inbox transaction commits. Concurrent duplicate deliveries can alert twice before one fails the inbox uniqueness check. Send only after commit.
- [x] **A10 — P1 — settlement sweep fairness** — `api/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure/Repositories/ConcertReadRepository.cs:35`
  A fixed first page of 200 deferred concerts repeats on every sweep and starves later eligible concerts. Page through all eligible IDs with a stable cursor.



## Coverage



- [x] Authority infrastructure and hosting — 67 files — `Authorization`, `DataAccess`, shared infrastructure, Web, Workers, solution

- [x] Tenant and profiles — 150 files — `Tenant`, `Artist`, `Venue`, `Dashboard`

- [x] Remaining modules and lifecycle — 71 files — `Application`, `Booking`, `Opportunity`, `Deal`, `Conversations`, Seed and lifecycle tests

- [x] Concert access and processing — 116 files — `api/src/Modules/Concert/**`

- [x] Frontend and mobile — 139 files — `app/**`, `package-lock.json`



## Rules manifest



Route source: frozen tree has no `.agents/skill-routes.json`; installed technical skills and nearest `AGENTS.md` guidance are applied by domain. Root `AGENTS.md`, `CODE_PATTERNS.md`, Concert `AGENTS.md`, web shared `AGENTS.md`, and changed test directory guidance apply where relevant. Security: yes for all five stages.



## Cross-area notes



The foundation lens also flagged `TryExecuteAsync`'s nested error handler. The current settlement reservation acquires a PostgreSQL `FOR UPDATE` concert row lock before reading and mutating the row, so two completion requests serialize at that lock. No demonstrated concurrent-completion path reaches the skipped EF concurrency handler; keep this as a transaction follow-up rather than a finding on this candidate.



## Parent finalization



**Cross-area notes status:** `complete`

**Parent summary status:** `complete`

All 543 frozen candidate paths are covered by the five areas. Ten confirmed findings require repair. Post-anchor edits will be reviewed as a separate incremental pass.

## Review pass — 2026-09-28 — incremental

**Candidate base:** `498119854487c485d5f4e476ce2fa729051ace1c`
**Candidate head:** `399954c8721696a711799699f944400ac7f02419`
**Candidate branch:** `Refactor/PartyFoundationAccess`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:b177ffc2a8f6287df10eb3736a925aac262f3415d36048bdfe6c91de27588afb` `(30 paths)`
**Candidate patch:** `sha256:9d7b32d62b7c90e2b53c10ec53d3df1c37243563b312c54d5874435f915f100d`
**Candidate bundle:** `C:/Users/TommySeery/source/repos/Concertable/b2b/.git/agent-workflow/runs/party-access-repair-20260928/review/d48601fafc2b8ca66429e2cad480c5396ff45b3dd16ac9d5385aa80716a5d313`
**Candidate bundle identity:** `sha256:d34ab069b84383b3215abdd2207b921825cf00838ddbb6d6e9879c57b90c73bf`
**Work-order path:** `reviews/Refactor-PartyFoundationAccess.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`
**Synchronization:** no new fetch; the staged pass synchronized once with origin, and the local repair commit descends from its frozen head.

### Findings

- [x] **A11 — HIGH — payment alert durability** — `api/src/Modules/Application/Concertable.B2B.Application.Infrastructure/Services/Payment/VerifyPaymentFailedProcessor.cs:103`
  The alert is sent after inbox commit, but a send failure leaves the inbox processed and the alert unretryable. Stage a notification command in the same transaction through the outbox.
  Resolved: the processor stages a command with the inbox and verification record; the Application integration and lifecycle tests pass.
- [x] **A12 — HIGH — tenant deletion race** — `api/src/Modules/Booking/Concertable.B2B.Booking.Infrastructure/Services/BookingWorkflow.cs:159`
  Booking creation does not lock either tenant row, so a concurrent tenant deletion can pass its Booking guard before the booking insert and orphan the new booking. Lock both tenant rows before insertion.
  Resolved: Booking acquires both Tenant-owned row locks before insertion; the provider-real lock test and deletion guard test pass.
- [x] **A13 — MEDIUM — dashboard inbox refresh** — `app/web/artist/src/features/notifications/hooks/useArtistNotifications.ts:17`, `app/web/venue/src/features/notifications/hooks/useVenueNotifications.ts:18`
  MessageReceived invalidates the mailbox query but leaves the dashboard inbox preview and unread indicator stale until polling. Invalidate each dashboard inbox query too.
  Resolved: both notification hooks invalidate their dashboard inbox query; artist and venue production builds pass.

## Review pass — 2026-09-28 — incremental

**Candidate base:** `399954c8721696a711799699f944400ac7f02419`
**Candidate head:** `b6109c043c3a65b91225c5a077993f4999b887c6`
**Candidate branch:** `Refactor/PartyFoundationAccess`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:20fa14a94857ecb70efe3d98e95eb9e1f4a8d54ea9fab23b834ea0accb65196a` `(17 paths)`
**Candidate patch:** `sha256:2b98e6625d7c9d17dd98c93e2876daaaf83d1118ce98403d1f8d3d4bcdbd4034`
**Candidate bundle:** `C:/Users/TommySeery/source/repos/Concertable/b2b/.git/agent-workflow/runs/party-access-followup-20260928/review/3aad074090cd5a4c47a30cc5ff3db34afc2c9f03d779f691521eefd5be7e0e03`
**Candidate bundle identity:** `sha256:a408076cc3236cf8c5312377349885afcf8eb5726d80732bae323ae2d4d4251e`
**Work-order path:** `reviews/Refactor-PartyFoundationAccess.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`
**Synchronization:** no new fetch; this local repair commit descends from the completed watermark.

### Findings

- [x] **A14 — MEDIUM — acceptance conflict** — `api/src/Modules/Tenant/Concertable.B2B.Tenant.Infrastructure/Services/TenantBookingFence.cs:14`
  If tenant deletion wins the row lock, the fence throws InvalidOperationException. Acceptance does not classify it and returns HTTP 500 for a valid concurrent lifecycle change. Return a distinct missing-tenant outcome and map it to a typed 409 after rollback.
  Resolved: TenantUnavailableException rolls back acceptance and maps to application.accept.party_unavailable; the deleted-artist integration case passes.
- [x] **A15 — LOW — module boundary** — `api/src/Modules/Booking/Concertable.B2B.Booking.Infrastructure/Services/BookingWorkflow.cs:164`
  Booking calls a separate Tenant booking-fence contract directly, bypassing the required ITenantModule facade. Expose the lock operation through ITenantModule while retaining the shared transaction.
  Resolved: Booking calls ITenantModule, which forwards to TenantService; the provider-real lock and deletion guard cases pass.
- [x] **A16 — HIGH — notification authorization** — `api/src/Modules/Application/Concertable.B2B.Application.Infrastructure/Handlers/NotifyApplicationPaymentVerificationFailedCommandHandler.cs:15`
  Queued delivery resolves the venue profile creator after commit and sends payment failure details without current tenant membership. A removed creator can receive a later notification. Resolve current members with application decision permission when handling the command and send only to them.
  Resolved: delivery rechecks venue membership, prefers the authorized profile creator, and falls back to a current authorized owner; the removal and lifecycle cases pass.
