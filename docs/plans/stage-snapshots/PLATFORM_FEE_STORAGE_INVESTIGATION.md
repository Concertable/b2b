# Platform-fee storage — is the per-row snapshot a smell? (investigation)

> **Commissioned:** to get a fresh, evidence-led verdict on how Concertable stores the platform fee on
> its financial rows. One opinion ("copying the value onto every row is a smell — use a version
> reference") was already on the table; this report does **not** assume it is right. It reaches the
> conclusion the evidence supports and marks where that conclusion flips.

## The question

In a .NET / EF Core financial system, what is the best-practice way to model **rarely-changing
platform-level pricing/fee config** with respect to the financial transactions it applies to?

Concrete case: a platform fee charged per settled booking, snapshotted so a later fee change never
retro-alters historic settlements. Today the resolved fee is a `PlatformFee` money column **directly on
each financial row** — `EscrowEntity` (escrow flow) and `SettlementTransactionEntity` (direct flow); the
escrow row also carries `Amount` (= gross + fee). Release transfers `Amount − PlatformFee`; refund
returns `Amount` in full.

The worry driving the investigation: copying the value onto every row *feels* like a smell — and worse
if projected to N ≈ 4–5 rarely-changing platform values that all rows must freeze. Roadmap direction:
per-tenant fee overrides and a `%-of-agreed-fee` variant, i.e. the fee grows from one scalar into a
structured, possibly per-tenant object.

## TL;DR verdict

**Snapshotting the resolved fee onto the financial row is not a smell — it is mandatory ledger practice,
and it is exactly what the gold-standard systems do.** The instinct to reach for a version reference is
half-right, but it is aimed at the wrong column. The correct shape is a **hybrid with a crisp boundary**:

- **Resolved money OUTPUTS** (`PlatformFee`, `Amount`) — **always snapshot on the row, by construction.**
  Never a live join, never recomputed on read. This is what makes the ledger self-describing and
  reconcilable. Do not remove these.
- **Config INPUTS** (the fee scalar today; the fee/%/min/max/per-tenant bundle tomorrow) — snapshot the
  raw scalar directly **while N is 1–2 and flat**; switch to an **immutable version FK → a rate-card
  row** once the inputs become a structured, multi-valued, and/or per-tenant object.

So: **v1 as it stands is correct — keep it, add nothing.** The version-reference idea earns its place
**only at the roadmap state** (bundle + per-tenant + %-variant), and even then it is added *alongside*
the money snapshot, never *instead of* it. The one thing to never do is what the "smell" instinct
tempts you toward: replace the money snapshot with a version FK (or a resolve-by-timestamp lookup) and
recompute the fee on read. That reintroduces exactly the retro-alteration the snapshot exists to prevent.

## The candidate patterns

| # | Pattern | One-line shape |
|---|---|---|
| 1 | **Per-row snapshot** *(current)* | Denormalized copy of each resolved value on the transaction row. |
| 2 | **Versioned rate-card + FK** | Append-only config record; row stores an FK to the version in effect at charge time; values read by join. |
| 3 | **Hybrid** | Version FK for the *inputs* **+** resolved money snapshotted on the row for the *outputs*. |
| 4 | **Append-log resolved by timestamp** | Config is an effective-dated log; no FK — resolve the row by charge time (`AS OF`). |
| 5 | **DB temporal tables** (SQL:2011) | Pattern 2/4 mechanized: system-versioned history table + `FOR SYSTEM_TIME AS OF`. |

## Evaluation matrix

The decisive axis is **"immutable by construction vs. by convention"** — does history stay frozen because
the schema *cannot* express a retro-change, or only because nobody runs the query that would?

| Criterion | 1. Per-row snapshot | 2. Version FK (pure) | 3. Hybrid (FK + money snapshot) | 4. Timestamp-resolve | 5. Temporal tables |
|---|---|---|---|---|---|
| **History immutable for money** | ✅ **by construction** | ❌ money recomputed on read — a resolution/rounding/currency change silently rewrites history | ✅ **by construction** (money is snapshotted) | ❌ by convention only | ⚠️ config history frozen, but money still recomputed |
| **Scales as N inputs grows** | ❌ N columns copied per row | ✅ one FK regardless of N | ✅ one FK + only the resolved output(s) | ✅ one timestamp | ✅ |
| **Read cost on money path** | ✅ zero join | ⚠️ join to resolve every amount | ✅ zero join for money; FK join only for audit | ⚠️ range/`AS OF` join | ⚠️ `AS OF` join |
| **Auditable / self-describing row** | ✅ money, but inputs not identified | ⚠️ inputs identified, money not stored | ✅ **both** — resolved money *and* which policy produced it | ❌ must reconstruct | ⚠️ |
| **Explains *which* config applied (per-tenant, %-variant)** | ❌ a bare number can't say which rule made it | ✅ FK names the exact version | ✅ FK names it; money proves it | ⚠️ inferred from time | ✅ |
| **Write / maintenance cost** | ✅ trivial now | ⚠️ extra table + resolver + FK | ⚠️ extra table + resolver (money write unchanged) | ⚠️ effective-dating logic | ⚠️ schema + housekeeping |
| **Ceremony at N=1 scalar** | ✅ none | ❌ single-row table + FK = pure overhead (YAGNI) | ❌ same overhead | ❌ | ❌ |

Reading the matrix: **#1 wins decisively at N=1**; **#3 wins decisively once inputs become structured**;
**#2, #4, #5 all share the same disqualifier for a money path — they recompute the amount on read**, so
none is acceptable *alone* for the fee that actually moves money. #2's version FK is valuable, but only
as the *input* half of #3, never as a replacement for the money snapshot.

## Real-world evidence

### Stripe — the direct answer: it does BOTH (hybrid), deliberately

An invoice **line item both references the immutable Price by id AND stores its own snapshotted amount**:

- `pricing.price_details.price` — *"The ID of the price this item is associated with."* (the version FK)
- `amount` — *"The amount, in the smallest currency unit."* (the money snapshot)

Stripe's stated reason: *"if the Price object is later modified, the invoice line item retains the
original monetary amounts that were actually charged."* Price/Plan objects are **immutable** — to change
one you create a new price and migrate; you never mutate the amount of an existing one.
([invoice line item object](https://docs.stripe.com/api/invoice-line-item/object),
[manage prices](https://docs.stripe.com/products-prices/manage-prices))

**Stripe Tax Rate is pattern-2 in its purest form** and maps 1:1 onto the roadmap's per-tenant/% bundle:
`percentage`, `country`, `state` are **immutable and settable only at create**; to change them you
**create a new tax rate and archive the old** (`active=false`), and *"this tax rate cannot be used with
new applications... but will still work for subscriptions and invoices that already have it set."*
Historic invoices keep pointing at the old id; nothing retro-changes.
([tax rates](https://docs.stripe.com/billing/taxes/tax-rates),
[tax rate object](https://docs.stripe.com/api/tax_rates/object)) — this is the append-only rate-card,
referenced by immutable id, and it is exactly what a multi-valued fee policy should become.

### Double-entry ledgers (Modern Treasury) — the posted amount is frozen, corrections are new entries

*"Once a ledger transaction is posted... it is financially immutable and its Entries cannot be
updated."* Adjustments are made by **appending an opposing/correcting transaction**, never by editing.
For FX, *"the historical rate is the exchange rate... at the point in time the transaction was
recorded"* — the resolved converted amount is captured on the entry; you do not re-derive a posted
amount from a live rate table. This is the accounting discipline behind snapshotting the *output*.
([enforcing immutability](https://www.moderntreasury.com/journal/enforcing-immutability-in-your-double-entry-ledger),
[ledger transactions](https://docs.moderntreasury.com/ledgers/docs/ledger-transactions-overview),
[FX in the GL](https://controller.ucsf.edu/reference/general-accounting/understanding-how-foreign-currency-amounts-are-recorded-general-ledger))

### Kimball dimensional modeling — the canonical "attributes that change slowly" framework

**SCD Type 2** is the version-FK pattern: a config change **adds a new dimension row with a new surrogate
key**, and *"historical fact rows are linked through the surrogate key to the version of the dimension
row that was current when the fact was recorded."* Crucially, Kimball also holds that **measured facts
live on the fact row** — you store the measure, you don't recompute it by joining to a dimension. Real
star schemas therefore do **both**: a surrogate FK to the versioned dimension (the inputs) *and* the
additive measures on the fact (the outputs). That is pattern 3.
([Type 2](https://www.kimballgroup.com/data-warehouse-business-intelligence-resources/kimball-techniques/dimensional-modeling-techniques/type-2/),
[SCD types 4–7](https://www.kimballgroup.com/2013/02/design-tip-152-slowly-changing-dimension-types-0-4-5-6-7/))

### SQL:2011 / SQL Server temporal tables — a mechanism, not the answer for a money path

System-versioned temporal tables make any config table append-only automatically (history table +
`FOR SYSTEM_TIME AS OF`), giving free audit of the *config table itself*.
([MS Learn: temporal tables](https://learn.microsoft.com/en-us/sql/relational-databases/tables/temporal-tables))
But resolving a historic fee via `AS OF charge_time` is immutable **by convention**: the transaction row
doesn't pin the version, and any change to the *resolution logic* (rounding, tenant precedence, %
basis) re-derives a different number for old rows. Fine for auditing config; **not** a substitute for
snapshotting the money the customer was actually charged.

### Where sources agree vs. diverge

- **Unanimous (settled best practice):** the **resolved monetary amount is snapshotted onto the
  immutable transaction/line/entry** — Stripe (`amount`), ledgers (posted entry), Kimball (fact
  measure). Recompute-on-read of a posted amount is universally rejected.
- **Unanimous:** rarely-changing config is modelled **append-only / immutable-versioned** (new Price,
  new Tax Rate, new Type-2 row) — you never mutate a version other rows still reference.
- **Judgment call (not settled):** *whether to also carry an explicit version FK* alongside the money
  snapshot. Stripe and Kimball say yes once the config is a real object; nobody bothers for a single
  scalar. This is the N=1-vs-N-many boundary, and it is the crux of Concertable's actual decision.

## Verdict, and exactly when it changes

**The per-row money snapshot is correct and must stay.** The "smell" is misdiagnosed: it reads the
denormalized *money* as the problem, when the money snapshot is the one part every authority insists on.
The real cost the instinct is sensing is **copying N raw config _inputs_ onto every row** — and the fix
for that is not to stop snapshotting money, it is to reference the *input bundle* by an immutable id.

| State | Recommendation |
|---|---|
| **v1 — single scalar fee, default 0, one platform-wide value** | **Per-row snapshot ALONE (pattern 1). Add nothing.** A version FK to a one-row config table is pure ceremony (YAGNI); a bare `PlatformFee` money column already tells the whole story and reconciles by construction. |
| **Roadmap — bundle of values + per-tenant overrides + %-variant** | **Switch to hybrid (pattern 3):** add an immutable, append-only rate-card row (the input bundle) + a version **FK on each financial row**, **and keep** the resolved `PlatformFee` / `Amount` money snapshot. The FK earns its place here because a bare number can no longer answer *"which tenant's override / which % on what basis produced this?"* — the FK pins the exact policy version by construction; the money snapshot keeps history frozen. |

**Trigger to migrate v1 → hybrid:** the moment the fee stops being a single platform-wide scalar — i.e.
the first of {a second config value that must be frozen together with the fee, a per-tenant override, the
%-variant} ships. Not before.

**Never do (the anti-pattern the smell tempts toward):** replace the money snapshot with a version FK
(pattern 2) or a timestamp lookup (pattern 4/5) and resolve the fee on read. That is the one change that
*reintroduces* retro-alteration of settled money.

## Concrete shapes for THIS codebase (EF Core)

### v1 — keep exactly what is there

`EscrowEntity`: `Money Amount` (= gross + fee) and `Money PlatformFee`, each an EF `ComplexProperty`
(as configured in `EscrowEntityConfiguration`). `SettlementTransactionEntity`: `long Amount` (= gross +
fee, minor units) and `long PlatformFee`. `PlatformFeeOptions.Fee` (scalar `decimal`, default 0) is the
**live default**; the persisted column is the **snapshot**. This already satisfies pattern 1 correctly —
**no config table, no FK, no change.** Building a rate-card now would be premature.

### Roadmap — hybrid, when the fee becomes a structured/per-tenant object

Add an immutable, append-only pricing-policy entity (the rate card) in the service that owns the pricing
config. Append-only = a change **inserts a new row**; existing rows are never updated (mirrors Stripe
create-new-Price / archive-old-TaxRate):

```csharp
public sealed class PlatformPricingPolicyEntity : IIdEntity   // Id is the version handle
{
    public int Id { get; private set; }
    public Guid? TenantId { get; private set; }        // null = platform default; set = per-tenant override
    public Money FlatFee { get; private set; }         // the flat variant
    public int? FeeBasisPoints { get; private set; }   // the %-of-agreed-fee variant (bps; null = flat only)
    public Money? MinFee { get; private set; }
    public Money? MaxFee { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public bool IsActive { get; private set; }          // archive an old version, don't mutate it
}
```

Each financial row then gains a **version FK** and **keeps** the resolved-money snapshot:

```csharp
// EscrowEntity / SettlementTransactionEntity gain:
public int PricingPolicyId { get; private set; }   // the input bundle in effect at charge time
// PlatformFee / Amount stay exactly as they are — the resolved OUTPUT, still snapshotted.
```

Resolution fits the codebase's **keyed strategy resolver** pattern (`api/docs/CODE_PATTERNS.md`): a
resolver picks the policy row (tenant override else platform default, effective at charge time) and a
per-variant strategy (`FlatFee` vs `%-of-agreed-fee`) computes the fee, then writes **both** the
`PricingPolicyId` and the resolved `PlatformFee` onto the row. History never recomputes: the money is
frozen by the snapshot, and the FK is auditable proof of which policy version produced it.

**Ownership note (already an open decision, not re-litigated here):** `PLATFORM_COMMISSION.md` §1.2/§6
keeps the flat fee Payment-owned for v1 and says the `%-variant` would be resolved in B2B (which holds
deal context) and the resolved fee/basis passed to Payment. That boundary decision is orthogonal to this
one: **whichever service resolves the policy writes the version id + resolved money; Payment snapshots
the money it settles regardless.** The rate-card table lives with whoever owns pricing config at that
point; the money snapshot on the Payment rows is invariant across that choice.

## North-star: the money system of record is a double-entry ledger

> **Scope of this section.** It answers a *different, sharper* objection than the one above: not "should
> money be snapshotted" (settled — yes), but that the resolved `PlatformFee` **appears as a column on
> every money-flow entity** (`EscrowEntity`, `SettlementTransactionEntity`, and every future flow). The
> hybrid answer kills the duplicated config *inputs* (one version FK), but the resolved fee *amount*
> still repeats per entity type. This section tests the genuine long-term "perfect" shape that dissolves
> *that*. **It does not change the verdict above:** v1-as-is is correct; hybrid at the roadmap trigger.
> The ledger is a **north-star**, sequenced strictly after both.

### The thesis under test

*Move the money-of-record OUT of the operational entities into a dedicated, append-only **double-entry
ledger**. "Platform fee" then stops being a column smeared across each entity and becomes a **ledger
entry against a platform-revenue account** — recorded once per settlement, in one place, queryable as
one ledger. `EscrowEntity` / `SettlementTransactionEntity` demote to **state machines** holding only
operationally-necessary facts (the real Stripe hold amount, statuses, Stripe ids) and **reference** the
pricing policy + the ledger; they are no longer the system of record for fee revenue.*

This is not exotic — it is how money-movement businesses are actually built, and it is a direct structural
answer to "the fee column repeats." If revenue lives in **one account's entries**, a fifth or tenth money
flow adds *zero* fee columns anywhere; it just posts one more entry to the same account.

### Evidence

**Stripe is itself a ledger, and this is exactly how it models fees.** Every fund movement is a single
`balance_transaction` with `amount`, `net`, and a `fee_details[]` breakdown — where `net = amount − fee`
and each fee detail carries a `type` (`application_fee`, `stripe_fee`, `tax`, …) and, for Connect, the
`application` that earned it. The platform's fee is **not** a column bolted onto each product object; it
is a typed line on the one unified ledger of everything crossing the balance.
([balance transaction object](https://docs.stripe.com/api/balance_transactions/object),
[query transactions](https://docs.stripe.com/data/query-transactions)) For Connect, the platform's cut is
an `application_fee` recorded on that ledger — the canonical "fee is an account/entry, not a per-object
column" design.

**Ledger-design practice says the same, explicitly.** The ledger is *"the source of truth for money"*;
balances are **derived** (*"compute balances from journal lines as the source of truth... keep cached
balances... treat them as derived data"*), postings are **atomic and balanced** (*"both the debit and
credit must post successfully, or not at all"*), and the discipline separates the **operational
transactional ledger** from operational product tables.
([SDK.finance](https://sdk.finance/blog/what-is-a-double-entry-ledger-in-fintech/),
[Fintechly: ledger system design](https://fintechly.com/infrastructure/infrastructure-ledger-system-design/),
[Lithic: modern ledgering](https://www.lithic.com/blog/modern-ledgering-guide)) Modern Treasury's posted
transaction is financially immutable with balanced entries and corrections-as-new-entries (cited above) —
the same account/entry model this thesis proposes.

### Does it hold up, or is it over-engineering? (honest read)

**It holds up — for a business that is fundamentally moving money, which Concertable is** (escrow holds,
Connect settlement, transfers, refunds). The ledger is the one shape that makes "platform-fee revenue"
a **single queryable truth** independent of how many operational entity types exist, gives provable
balances, and turns VAT/DAC7/revenue reporting into a query over one account rather than a `UNION` across
every money-flow table. For a settlement platform this is the recognised end-state, not gold-plating.

**But two honest caveats keep it a north-star, not a now:**

1. **Stripe already is that ledger.** `balance_transaction` + `application_fee` is a real, reconcilable,
   authoritative record of fee revenue *today*. Until Concertable needs balances Stripe can't answer —
   its **own** authoritative money-of-record, cross-provider/multi-rail movement, internal accounts that
   don't map 1:1 to a Stripe object, or point-in-time internal reporting — building a home-grown GL
   **duplicates Stripe's ledger** for little gain. The first, cheapest step of the north-star is often
   *"reconcile fee revenue from Stripe's ledger"*, not *"build our own."*
2. **It is a Payment-service-wide investment far larger than the fee feature** — double-entry posting,
   balance derivation, correction-entry discipline, Stripe reconciliation, migration of *existing* money
   history. Pulling it forward to solve one repeated column would be the textbook over-engineering the
   commissioner is right to be wary of in the *other* direction.

So: **genuinely the perfect long-term shape, genuinely premature now.** The repeated `PlatformFee` column
across two entities is a real but *small* smell; the ledger is a large, correct, and separately-motivated
investment that happens to also erase it. You adopt the ledger when the *ledger's own* triggers fire —
not to de-duplicate a column.

### Concrete shape for THIS codebase (Payment service, EF Core)

Append-only, immutable, balanced — mirrors Modern Treasury / Stripe's model:

```csharp
public sealed class LedgerAccount : IIdEntity            // few, long-lived, seeded
{
    public int Id { get; private set; }
    public LedgerAccountType Type { get; private set; }  // PlatformRevenue | PayerClearing | PayeePayable | StripeCash
    public Guid? OwnerId { get; private set; }           // null for the single platform-revenue account
    public Currency Currency { get; private set; }
}

public sealed class LedgerTransaction : IIdEntity        // one balanced posting; immutable once written
{
    public int Id { get; private set; }
    public int BookingId { get; private set; }
    public int? PricingPolicyId { get; private set; }    // the rate-card version that produced the fee entry
    public DateTime OccurredAt { get; private set; }
    // navigation: IReadOnlyList<LedgerEntry> Entries — sum(debits) == sum(credits), enforced on write
}

public sealed class LedgerEntry : IIdEntity              // append-only; never updated or deleted
{
    public int Id { get; private set; }
    public int LedgerTransactionId { get; private set; }
    public int LedgerAccountId { get; private set; }
    public EntryDirection Direction { get; private set; } // Debit | Credit
    public Money Amount { get; private set; }
}
```

**A settlement posts one balanced transaction** (debits == credits by construction):

| Direction | Account | Amount |
|---|---|---|
| Debit | Payer clearing | `gross + fee` |
| Credit | Payee payable | `gross` |
| Credit | **Platform revenue** | `fee` |

The fee is now **one credit entry to the platform-revenue account** — recorded once, in one place, for
*every* flow type. A refund posts the reversing balanced transaction. Platform-fee revenue for any period
is `SUM(entries WHERE account = PlatformRevenue)` — a single query, no per-entity `PlatformFee` column
involved.

**What honestly stays operational vs. moves to the ledger:**

- **Stays on the operational entity (real operational facts):** `EscrowEntity.Amount` — the escrow *does*
  ring-fence `gross + fee` on the payer's card in Stripe, so the hold total is a genuine operational
  truth the state machine must hold to release/refund correctly. Statuses, Stripe `ChargeId`/`TransferId`
  /`RefundId`, timestamps — all operational. The entity gains a reference (e.g. a `LedgerTransactionId`)
  to its posting.
- **Moves to the ledger (money-of-record truth):** *fee-as-revenue* and *payee-owed* become entries, not
  columns. `PlatformFee` on the entities stops being the system of record; if kept at all it is a
  denormalized operational convenience (like a cached balance — *derived*, per the ledger-design rule),
  not the truth. `SettlementTransactionEntity`'s reason to carry a `PlatformFee` column disappears
  entirely — its revenue truth is the platform-revenue credit.

### Phased path — three hops, explicit triggers

| Hop | Shape | Trigger to take the hop |
|---|---|---|
| **0 → 1 (now)** | **Per-row snapshot** (v1, current) | Already here. Correct. Do nothing. |
| **1 → 2** | **Hybrid** — immutable rate-card row + version FK on each row, money still snapshotted | The fee stops being a single platform-wide scalar: first of {a 2nd frozen value, a per-tenant override, the %-variant}. |
| **2 → 3 (north-star)** | **Double-entry ledger** as money-of-record; operational entities reference it; fee becomes a platform-revenue entry | The *ledger's own* triggers, any of: ≥3 money-flow entity types each needing revenue truth; need for **authoritative internal balances / financial reporting independent of Stripe**; multi-provider / multi-rail money movement; VAT/DAC7/audit reporting that wants one queryable money-of-record. **First sub-step is usually "reconcile from Stripe's ledger," not "build a GL."** |

Each hop is **additive** and strictly ordered: hop 3 sits on top of the rate-card FK from hop 2 (the
`LedgerTransaction.PricingPolicyId` above), and neither hop 2 nor hop 3 revisits or invalidates hop 0.
**None of this changes the v1 decision.**

### North-star verdict

**Yes — a dedicated append-only double-entry ledger is genuinely the correct "perfect" long-term design
for Concertable's money-of-record, and it is the shape that structurally dissolves the "fee column on
every entity" concern** (fee becomes an *account*, recorded once, not a column per flow type). It is what
Stripe itself does (`balance_transaction` + `application_fee`) and what ledger-design practice prescribes.

**It is worth the investment only when the ledger's own triggers fire** — authoritative internal balances
independent of Stripe, several money-flow types, multi-rail movement, or one-source financial/tax
reporting. Until then it is a large, separately-motivated architectural programme, and pulling it forward
to de-duplicate one column would be over-engineering. **Stay at per-row snapshot for v1; move to hybrid at
the roadmap trigger; hold the ledger as the sequenced north-star** — and when you do reach for it, start
by reconciling against Stripe's existing ledger before building your own general ledger.

## Sources

- Stripe — [invoice line item object](https://docs.stripe.com/api/invoice-line-item/object) ·
  [manage prices](https://docs.stripe.com/products-prices/manage-prices) ·
  [tax rates](https://docs.stripe.com/billing/taxes/tax-rates) ·
  [tax rate object](https://docs.stripe.com/api/tax_rates/object) ·
  [balance transaction object](https://docs.stripe.com/api/balance_transactions/object) ·
  [query transactions](https://docs.stripe.com/data/query-transactions)
- Ledger-first / double-entry design —
  [SDK.finance](https://sdk.finance/blog/what-is-a-double-entry-ledger-in-fintech/) ·
  [Fintechly: ledger system design](https://fintechly.com/infrastructure/infrastructure-ledger-system-design/) ·
  [Lithic: modern ledgering](https://www.lithic.com/blog/modern-ledgering-guide)
- Modern Treasury —
  [enforcing immutability](https://www.moderntreasury.com/journal/enforcing-immutability-in-your-double-entry-ledger) ·
  [ledger transactions](https://docs.moderntreasury.com/ledgers/docs/ledger-transactions-overview) ·
  [scaling a ledger, part V](https://www.moderntreasury.com/journal/how-to-scale-a-ledger-part-v)
- Foreign-currency in the GL —
  [UCSF Controller](https://controller.ucsf.edu/reference/general-accounting/understanding-how-foreign-currency-amounts-are-recorded-general-ledger)
- Kimball Group — [SCD Type 2](https://www.kimballgroup.com/data-warehouse-business-intelligence-resources/kimball-techniques/dimensional-modeling-techniques/type-2/) ·
  [SCD Types 0/4/5/6/7](https://www.kimballgroup.com/2013/02/design-tip-152-slowly-changing-dimension-types-0-4-5-6-7/)
- Microsoft — [SQL Server system-versioned temporal tables](https://learn.microsoft.com/en-us/sql/relational-databases/tables/temporal-tables)
