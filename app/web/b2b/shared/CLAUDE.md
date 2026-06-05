# app/web/b2b/shared — code shared across the B2B (manager) SPAs

## Everything here compiles into BOTH manager apps (venue + artist). Nothing customer-facing goes here. Nothing single-app goes here. Ever.

Concertable is two products. The customer marketplace and the B2B contract platform share a design
system and infrastructure (`app/web/shared`), not features. This tier exists for the B2B feature
layer: opportunities, contracts, applications, manager payouts — concepts a customer must never see
and the customer app cannot even resolve (`@b2b/*` is aliased only in the venue and artist
tsconfig/vite configs; an import from customer code is a module-resolution error, not a lint
warning).

Three buckets, three homes:

- **Every site** (customer + managers) → `app/web/shared`. Design system, auth infra, search,
  messaging, user basics, concert/venue/artist details views.
- **Both manager apps, no customer** → here. Opportunities, contract UI, application checkout
  hooks, payout onboarding.
- **One app only** → that app's `src/`. `useMyVenue` belongs to venue, `useApply` to artist,
  ticket surfaces to customer. Two consumers is the minimum bar for this folder — "venue uses it
  and artist might later" is single-app code.

Rules inherited from `app/web/shared/CLAUDE.md` still apply, scoped to the two manager apps:

- **Route rule** — only literals BOTH manager apps register (`/`, `/my`,
  `/my/concerts/concert/$id`, `/find`, `/find/{artist,venue,concert}/$id`, `/create`,
  `/settings`, `/settings/payment` + the universal auth/stripe routes). Venue-only literals
  (`/my/opportunities/...`, `/applications/...`) and artist-only literals
  (`/opportunity/checkout/...`) are injected by the owning app via props/slots.
- **Identity rule** — no `isVenueManager`/`isArtistManager` branching. The two apps compose the
  difference in (e.g. `OpportunitySection({ renderActions })`: venue injects View Applications,
  artist injects Apply).
- **Backend rule** — own-site `api` (B2B), `searchAxios`, `paymentAxios`. Manager-only B2B
  endpoints are fine here — that's the point of the tier.

Enforcement is the type system: both manager builds (`tsc -b`) compile this tree against their own
route trees, and the customer build proves it can't reach it. All four web builds green is the gate.

The litmus: *"do BOTH manager sites render this and run every call it makes, with their own tokens,
today — and would a customer ever be allowed to see it?"* Both-managers-yes + customer-no → here.
Anything else has a different home.

This tier was created because it was missing: with only `app/web/shared` as a meeting point,
venue+artist code (opportunities, contracts) defaulted into the all-sites bucket and customers
could browse venue opportunities and open contract details on the customer site. Don't recreate
that by parking B2B code in `app/web/shared` "for now".
