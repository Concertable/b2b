# B2B Legal & Compliance Requirements

Status (2026-06-01): **prioritised backlog**, not a description of what exists. As of
writing, none of the substantive items below are implemented — the only legal-adjacent
thing in code is Stripe Connect KYC onboarding (`StripeAccountClient`, Express accounts,
GB, verification-status tracking).

**Scope: B2B only.** B2B is deployed standalone and sold on its own — the Customer
marketplace is a later, optional add-on (that separation is the whole reason the monolith
was split). So this doc covers *only* venue↔artist obligations. Fan / ticket-buyer
obligations (consumer refund rights, all-in pricing, fake-review controls, ticket-sale VAT
posture) live in the separate Customer doc:
[`../../../Concertable.Customer/LEGAL_REQUIREMENTS.md`](../../../Concertable.Customer/LEGAL_REQUIREMENTS.md).
Do not merge the two — they are two separate systems.

## The product these obligations protect: the contract engine

B2B's selling point is the **four typed contracts** — FlatFee, DoorSplit, VenueHire, Versus
(see [`ARCHITECTURE.md`](./ARCHITECTURE.md)). No competitor offers door
splits, artist-pays-venue, or guarantee+split. The legal work is not generic "add VAT" — it
is **per-contract-type**, because each type moves money in a different direction and so has
a different supply/VAT/invoicing shape. That is the spine of everything below.

---

## 0. Foundational decision — VAT posture for booking settlement: Agent

**Decided 2026-06-01 (accountant sign-off before launch).**

For booking settlement (the only money flow in standalone B2B), Concertable is an **agent**,
not a principal. The deal is venue-to-artist (or artist-to-venue for VenueHire); we
facilitate via Stripe Connect and charge a platform fee. We account for VAT **only on our
own platform fee**. Each party accounts for its own VAT on the settlement amount according
to *that party's* registration status.

This mirrors GigPig (agent; auto-generates the artist's invoice to the venue on the
artist's behalf; charges its own booking fee). HMRC judges agent-vs-principal on substance —
to remain an agent, money for the venue↔artist leg must move via the parties' own Stripe
connected accounts, never become Concertable's own revenue (only the platform fee is ours).

The ticket-sale posture is a *Customer-service* question and is decided in that doc — not
here, because standalone B2B has no fan ticket sales.

This posture anchors the tenancy model: the legal/VAT entity *is* the tenant (realized in the
Tenant module — `TenantEntity` + the `Compliance` value object).

---

## 1. Per-contract-type legal handling — the core work — ABSENT

Each contract type has a distinct supply direction. Settlement code
(`*FinishStep`, `*AcceptStep`) currently does raw arithmetic with no VAT or invoicing.

| Contract | Supplier → customer | Who invoices whom | VAT consideration | Money path (from ARCHITECTURE §2.8) |
|----------|---------------------|-------------------|-------------------|----------------------------------------------|
| **FlatFee** | artist → venue (performance) | artist invoices venue (self-billed) | artist's fee VATable iff artist VAT-registered | Escrow capture at Accept → release at Finish |
| **DoorSplit** | artist → venue (performance) | artist invoices venue for the share | artist's share VATable iff artist registered | off-session pay at Finish |
| **Versus** | artist → venue (performance) | artist invoices venue (guarantee + split) | as DoorSplit, on the greater amount | off-session pay at Finish |
| **VenueHire** | **venue → artist** (hire of premises) | **venue invoices artist** | venue's hire fee VATable iff venue registered | Escrow deposit by artist at Accept → release at Finish |

**The trap:** VenueHire reverses the direction. A blanket "add 20% to the artist payout"
rule is wrong for VenueHire (the artist is the *buyer* there) and wrong whenever the
relevant party is not VAT-registered. VAT handling must branch on **contract type** and on
**the registered status of the party who is the supplier for that type**.

**Build:** a per-contract VAT/settlement calculator that knows the supply direction, reads
the supplier's VAT status (item 3), and produces net/VAT/gross + the correct invoice
direction (item 4). `TicketPayeeResolver` already encodes the VenueHire direction flip — the
VAT calculator needs the same awareness.

### Revenue source for DoorSplit / Versus (standalone-B2B lead)

DoorSplit and Versus settle against **door/ticket revenue**. In the full product that comes
from the Customer marketplace's ticket sales. **In standalone B2B there is no marketplace**,
so that figure has to come from somewhere else — manual door-count/revenue entry by the
venue at settlement, or an import from a third-party ticketer (DICE/Skiddle/Eventbrite).
Without one of those, DoorSplit/Versus can't settle. This is a product gap to close before
selling door-split contracts on a marketplace-less B2B deploy. (Lead only — not a legal
requirement, but it blocks the contract types that *are* the USP.)

---

## 2. Booking agreement + e-signature — ABSENT
**Legal basis:** contract law; Electronic Communications Act 2000; retained eIDAS.
The `Accept` lifecycle transition is an *implicit* agreement that produces **no signed
record**. Concertable has a sophisticated *settlement* contract (`ContractEntity`) but no
legal *agreement artifact* — the thing GigPig/GigXchange market as "contract signing". This
matters **more** here than for them: money moves in multiple directions (incl. artist-pays-
venue VenueHire), so evidence of what was agreed carries higher stakes. It is also the
**backbone** for several items below — item 4 (the invoice references the agreed terms),
item 6 (cancellation terms are stated *and consented to* here), item 7 (terms acceptance),
and item 9 (the audit trail of who agreed to what).
**Build:**
- A `BookingAgreement` entity — **not** named `Contract` (don't collide with
  `ContractEntity`). It **snapshots** the agreed terms at Accept: both parties + details,
  contract type + numbers, date, cancellation terms, platform-terms version. Snapshot, **not**
  an FK to the live contract — `ContractEntity` can be edited later and the agreement must
  freeze what was agreed (the existing purchase-time-snapshot convention).
- Capture affirmative consent. **Tier 1 (do first): click-wrap** — an "I agree" gate wired
  into the existing `AcceptExecutor` / checkout step, recording identity + UTC timestamp
  (+ optionally IP / user-agent). In the UK that is a legally binding electronic signature
  for a B2B booking — no DocuSign needed.
- Generate the agreement PDF via `IPdfService` (QuestPDF, already in
  `Concertable.Shared.Pdf`, currently unused in B2B); store immutably; expose download. Same
  plumbing the invoice (item 4) reuses.
- **Tier 2 (later / optional):** full e-signature (drawn/typed, or DocuSign / Dropbox Sign /
  Yousign). Overkill for grassroots bookings — add only if a customer demands it.
**Lives in:** the Concert module (owns the `Accept` transition + the parties); reads terms
via `IContractModule` to build the snapshot.

---

## 3. VAT registration status + VAT number — ABSENT
**Legal basis:** VAT Act 1994.
No `VatNumber`/`VatRegistered` on `ArtistEntity`, `VenueEntity`, or `TenantEntity`.
**Build:** capture VAT-registered (bool) + VAT number on the **tenant / legal entity** (see
tenancy design), resolved from a venue's/artist's owning organisation — not duplicated per
profile. Validate format. Feeds the per-type calculator in item 1.

## 4. VAT-compliant invoice / self-billing — ABSENT
**Legal basis:** HMRC VAT invoice rules; self-billing agreement rules.
`IPdfService` (QuestPDF) exists in `Concertable.Shared.Pdf` but is unused in B2B. No
`InvoiceEntity`, no numbering.
**Build:** per settlement, generate an invoice in the **direction item 1 dictates** with
sequential, gap-free numbering scoped per issuing entity, tax-point date, both parties' VAT
details, net/VAT/gross + line items. Mirror GigPig: the platform self-bills (generates the
supplier's invoice on their behalf) — requires a self-billing clause in the artist/venue
terms. Store immutably; expose download. Reuse `IPdfService`.

## 5. PRS pass-through (corrected) — ABSENT
**Legal basis:** Copyright, Designs and Patents Act 1988; PRS for Music tariffs.
**Correction to prior version of this file:** PRS is **not a flat 3%** and is **not the
platform's liability**. The live popular-music tariff is ~**4.2% of box office** (6.5%
classical) and rises over time; PRS is the **venue's** obligation (venues hold their own
licence or use the Gigs & Clubs scheme, and typically pass cost to the artist in the hire
agreement). A hardcoded "platform deducts 3% PRS" risks double-charging an already-licensed
venue.
**Build (only if product wants it):** a **per-venue/per-contract configurable pass-through**,
gated on a "venue is self-licensed for PRS" flag, rate from config (default to current
tariff, not a literal), recorded as a separate line item deducted from gross door before the
split. Low priority — many venues handle PRS entirely off-platform. The self-licensed flag +
rate live on the **tenant configuration surface**, not as constants (PRS is the motivating
example, but it's one of several per-tenant settings).

## 6. Cancellation + escrow refund (venue↔artist) — ABSENT
**Legal basis:** contract.
No `Cancelled` value on `ConcertStage` / `BookingStatus` / `ApplicationStatus` (`Withdrawn`
exists on applications but triggers no refund). `EscrowEntity.Refund()` exists but nothing
in B2B calls it.
**Build:** terminal `Cancelled` stages; on transition, raise an event that triggers the
escrow refund (held funds back to payer — note this means *artist* for VenueHire). Define
cancellation authority and any non-refundable-deposit terms per contract type. (If a
marketplace is later attached, this event also drives ticket-buyer refunds — handled in the
Customer doc, not here.)

## 7. Terms acceptance + privacy/GDPR consent — ABSENT
**Legal basis:** UK GDPR; PECR (cookies).
No ToS-acceptance flag, consent record, or privacy acknowledgment in the Auth registration
flow; no cookie consent.
**Build:** record terms-version + timestamp + consent at registration (immutable consent
log); cookie consent on the SPA; versioned terms so re-acceptance can be forced on material
change. Launch-blocking and independent of the money model.

## 8. GDPR data retention + right to erasure — ABSENT
**Legal basis:** UK GDPR arts. 5(1)(e), 17.
No soft-delete, retention policy, or anonymisation/purge path. PII held for artists, venues,
managers.
**Build:** retention policy per data category; erasure path that **anonymises the person but
preserves the statutory financial records** (invoices must survive erasure for HMRC). Note
the tension explicitly.

## 9. Audit trail on the booking lifecycle — PARTIAL
**Legal basis:** dispute evidence; financial record-keeping.
`IAuditable` is on `EscrowEntity` but **not** on `ApplicationEntity` / `BookingEntity` /
`ConcertEntity` — so who accepted what contract terms, and when, has no record, despite
money moving on those transitions.
**Build:** apply `IAuditable` (or an append-only event log) to the booking lifecycle
entities so every money-affecting transition is attributable and timestamped. The booking
agreement (item 2) captures the *terms* consented to; this captures the *transitions* — the
two together are the full evidential record.

---

## Suggested sequencing

1. **Item 0** (posture) — done; accountant sign-off before launch.
2. **Tenancy / legal-entity model** (shipped — Tenant module) +
   **item 3** (VAT status) — unblocks the rest.
3. **Item 2** (booking agreement + e-signature) — backbone for items 4/6/7/9; ship the
   click-wrap tier early (it also gives you item 9's terms record for free).
4. **Items 7, 8** (consent, retention) — launch-blocking, money-model-independent.
5. **Item 1 → 4** (per-contract VAT calc, then invoicing) — the core, in order; reuses the
   item 2 PDF plumbing.
6. **Item 6** (cancellation/refund) + the DoorSplit/Versus revenue-source gap.
7. **Items 5, 9** (PRS pass-through, lifecycle audit) — lower priority.
