# GDPR Subject Rights — Erasure + Data Portability (Concertable)

> **Disclaimer:** Research-grounded working draft, **not legal advice**. Everything here is a first
> draft for a UK solicitor to validate before you rely on it. Calls whose legal basis needs solicitor
> sign-off are marked **[LEGAL]**; product/ops decisions still open are marked **[DECIDE]**. Owner: you
> (with solicitor). This is the standing compliance record for the erasure/export capability; it
> satisfies the `LAUNCH_CHECKLIST.md` Phase 2 "DSAR process documented" / "Data retention schedule
> documented" gates and closes item 8 of `../../../api/src/Modules/Deal/LEGAL_REQUIREMENTS.md`.

## Why we're in scope

UK GDPR gives every data subject the **right to erasure** (art. 17) and the **right of access** /
**right to data portability** (arts. 15 / 20 — a machine-readable copy of their personal data).
Concertable holds personal data for artists, venues, their managers, and (in the Customer system) fans,
so both rights apply. This document is the ratified design for meeting them.

**This is not a `DELETE`.** Concertable is obliged to *keep* records the same law that grants erasure
carves out (art. 17(3) — retention required for a legal obligation or the establishment/exercise/defence
of legal claims):

- **HMRC six-year financial retention** — self-billed VAT invoices, self-billing agreements, ledger
  entries, and the settlement identity behind them.
- **Contract-law evidence** — signed booking contracts and their e-signatures, for the limitation
  period.
- **Online Safety Act evidence** — content reports, for as long as the OSA record obligation is live
  (see [`OSA_COMPLIANCE.md`](OSA_COMPLIANCE.md)).

So the capability is a **designed retain-vs-erase split**, never a cascade delete: anonymise the natural
person (name, email, contact, credentials, tokens) and **retain the financial/legal record with the
personal identity severed, or — where the record itself is the evidence — retained intact**. The table
below is the register of every one of those calls; the legal basis is stated so the solicitor can
confirm each.

### The subject, and who owns erasure/export

A **data subject is a natural person**, identified by the Auth credential `sub` (`Guid Id` on
`CredentialEntity`). A person is either a **B2B subject** (a venue/artist manager) or a **Customer
subject** (a fan). B2B and Customer are two separate systems, so a person present in both is **two
subject records**, one per system, each erased/exported independently. Erasure and export are owned by
the data service the subject belongs to; it reaches the **adapter** services (Auth, Payment) through
integration events and synchronous data→adapter calls only — never a cross-service data query. A
`Tenant` is a **legal entity, not a person**: erasing a human severs their membership and anonymises
their personal rows; the tenant's settlement identity persists per the table.

---

## The retain-vs-erase decision register (the spine — exhaustive)

Every row is a call the solicitor must confirm. Decisions:
**ERASE** = anonymise/scrub in place; **SEVER** = keep the record, drop or pseudonymise the identity
link, keep the non-personal (financial) fields; **RETAIN** = keep the row intact for the statutory
window because the row *is* the evidence; **PURGE** = hard-delete (transient, no retention need).
`[LEGAL]` marks a call whose basis needs solicitor ratification before merge.

### Auth (`api/Concertable.Auth`) — identity root

| Store · field | Decision | Basis |
|---|---|---|
| `CredentialEntity.Email` | ERASE → stable tombstone pseudonym (keep the `sub` row so downstream FKs stay valid) | identity; no retention need |
| `CredentialEntity.PasswordHash`, `IsEmailVerified` | ERASE (scrub) | secret |
| `EmailVerificationTokenEntity`, `PasswordResetTokenEntity` | PURGE | transient |
| Duende persisted grants / refresh tokens (`PersistedGrantDbContext`) | PURGE (revoke) | session |

### B2B (`api/Concertable.B2B`)

| Store · field | Decision | Basis |
|---|---|---|
| `Modules/User` `UserEntity` — `Email`, `Address`, `Location`, `Avatar` | ERASE | no statutory need |
| `Modules/Tenant` `TenantMembershipEntity` | PURGE (sever), guarded by the **last-owner invariant** (mirror `MembershipService.IsLastOwnerAsync`) | membership link |
| `Modules/Tenant` `TenantInvitationEntity.Email` | PURGE pending; accepted rows already severed via membership | transient |
| `Modules/Tenant` `TenantEntity.LegalName` + `TaxCompliance` (`SellerIdentifier` [NINO/UTR], `VatNumber`, `BankReference`, `RegisteredAddress`) | RETAIN while the tenant has other members or settled financial history in the 6-year window (it is a distinct legal entity); when the erased person is the **last member** of a sole-trader tenant, SEVER the membership but RETAIN the tax identity for the HMRC six-year window, flagged pending-purge, purged by the sweep after the window | HMRC six-year retention; agent VAT posture **[LEGAL]** |
| `Modules/Concert` `InvoiceEntity` + `InvoiceParty` (LegalName, VatNumber, address) | RETAIN intact | HMRC VAT Act invoice-retention |
| `Modules/Concert` `SelfBillingAgreementEntity` (`InvoiceParty` + `ESignature`) | RETAIN intact | HMRC self-billing agreement |
| `Modules/Concert` `ContractEntity` (VenueName, ArtistName, both `ESignature`) | RETAIN intact for the contract-limitation period | contract-law / dispute evidence |
| `Modules/Concert` `ESignature` VO (`SignatoryName`, `Ip`, `UserAgent`, `DrawnSignatureImage`) | RETAIN intact — the signature **is** the consent evidence; erasing it destroys the record it exists to preserve | ECA 2000 / eIDAS **[LEGAL]** |
| `Modules/Conversations` `MessageEntity.Content` (+ `SenderTenantId`/`SentByUserId`) | SEVER — keep the body, drop the personal identity link — for the limitation/OSA window, then PURGE by the sweep | dispute evidence / OSA **[LEGAL]** |
| `Modules/Conversations` `ContentReportEntity` (`MessageExcerpt`, ids, notes) | RETAIN while the OSA record obligation is live | OSA evidence |
| `Modules/Conversations` `ParticipantProfile` read model (Name, Address) | ERASE (re-project to pseudonym) | projection copy |

### Customer (`api/Concertable.Customer`)

| Store · field | Decision | Basis |
|---|---|---|
| `Modules/User` `UserEntity` — `Email`, `Location`, `Address` | ERASE | no statutory need |
| `Modules/Ticket` `TicketEntity` (`UserId`, `QrCode`, denormalised names, `Price`) | SEVER — drop the `UserId` link, keep the financial proof-of-purchase fields; scrub `QrCode` post-event | consumer-rights / financial record |
| `Modules/Review` `ReviewEntity` (**keyed by `Email`**, `Details`) | ERASE — scrub `Email` + free-text `Details`; keep the anonymous `Stars` aggregate | no statutory need; DMCCA genuine-review nuance **[LEGAL]** |
| `Modules/Preference` `PreferenceEntity` (`UserId`) | PURGE | no need |

### Payment (`api/Concertable.Payment`) — opaque-owner keyed, financial

| Store · field | Decision | Basis |
|---|---|---|
| `PayoutAccountEntity.Email` (+ `StripeAccountId`/`StripeCustomerId`) | ERASE (scrub `Email`); detach the Stripe customer; keep the row for ledger linkage | only direct PII in Payment |
| `TransactionEntity`/`LedgerEntryEntity`/`LedgerAccountEntity`/`EscrowEntity` (opaque `OwnerId`/`PayerId`/`PayeeId`, amounts, `IAuditable` stamps) | RETAIN intact — no names; opaque keys only | HMRC six-year / double-entry integrity |
| `StripeEventEntity` (raw webhook payload) | PURGE after the idempotency window (may embed PII) **[LEGAL]** | webhook idempotency only |

### Search (`api/Concertable.Search`)

| Store | Decision | Basis |
|---|---|---|
| Projections holding copied artist/venue names + emails | ERASE — re-project to pseudonym driven by the erasure event it already consumes | projection copy |

---

## The DSAR response SLA

UK GDPR sets a **statutory one calendar month** to respond to a subject-rights request (erasure,
access, or portability), running from the day the request is received. It is **extendable by up to two
further months** for complex or numerous requests, provided the subject is told of the extension and the
reason within the first month.

| Stage | Target |
|---|---|
| Acknowledge the request to the subject | On receipt (immediate) |
| Verify the requester is the subject (identity check) | **Within 3 working days** — a DSAR must not itself become a data-leak vector **[DECIDE]** operator identity-proofing steps |
| Erasure executed **or** deferral recorded with reason | **Within 1 calendar month** of receipt |
| Portable export delivered | **Within 1 calendar month** of receipt |
| Extension notified (complex/numerous only) | **Within the first calendar month**, with reasons |
| Deferred erasure completed once the last obligation clears | Automatically, by the hourly self-healing sweep — no further human action |

A **deferred** erasure (a subject with a live financial obligation — unsettled/booked concert, held
escrow, pending payout, un-expired self-billing agreement) still meets the SLA: the deferral and its
reason are recorded within the month, and the sweep completes the erasure the moment the obligation
settles. This is the fail-closed posture — anonymising identity mid-settlement would break settlement
and the audit trail, so the request waits rather than corrupting the record.

---

## Sign-off checklist (mirrors `LAUNCH_CHECKLIST.md` Phase 2)

Each `[LEGAL]` row above is a decision the solicitor confirms; this checklist tracks the ratification.

- [ ] Retain-vs-erase register (the whole table above) reviewed + accepted **[LEGAL]**
- [ ] The `[LEGAL]`-flagged rows confirmed individually:
  - [ ] Sole-trader tenant tax-identity RETAIN + pending-purge window (agent VAT posture) **[LEGAL]**
  - [ ] `ESignature` RETAIN-intact (ECA 2000 / eIDAS) **[LEGAL]**
  - [ ] `MessageEntity.Content` SEVER-then-purge window (dispute + OSA evidence vs minimisation) **[LEGAL]**
  - [ ] Customer `ReviewEntity` scrub (DMCCA genuine-review nuance) **[LEGAL]**
  - [ ] Payment `StripeEventEntity` purge-after-idempotency-window **[LEGAL]**
- [ ] Data-retention schedule (the statutory windows the register encodes) documented + accepted **[LEGAL]**
- [ ] DSAR process — the one-calendar-month SLA, deferral handling, and requester identity-proofing —
  documented + accepted **[LEGAL]**
- [ ] Retention/erasure summary reflected in the platform privacy policy the solicitor drafts **[LEGAL]**
- [ ] Operator identity-proofing steps for a raised DSAR decided **[DECIDE]**

Reference: ICO — Right to erasure https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/individual-rights/right-to-erasure/
· Right of access https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/individual-rights/right-of-access/
