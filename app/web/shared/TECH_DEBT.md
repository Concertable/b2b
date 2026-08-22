# app/web/b2b/shared — Technical Debt

## LOW

### API modules repeat their resource's base route as a string literal per method instead of a `BASE` const

`membersApi.ts` and `organizationApi.ts` each call `apiClient` with the same route prefix written out
fresh in every method instead of declared once as a `const BASE = "/..."` and interpolated. A rename of
the resource's route needs a find-and-replace across every method instead of one edit.

Found while fixing the same pattern in the admin console's `moderationApi.ts`/`adminApi.ts`
(`app/web/admin/src/features/{moderation,admins}/api/`) — see `app/shared/TECH_DEBT.md` for the sibling
entry covering `app/shared`'s own api modules with this shape.

**Resolves when:** each listed file gets a `const BASE = "/..."` at the top, with every method
interpolating it instead of restating the literal.

### The write-boundary pattern's pre-validation form state is named `XBuffer`, not `XDraft`

`useInviteMember.ts`'s `InviteBuffer` (consumed by `InviteForm.tsx`, re-exported from
`features/members/index.ts`) and `useOrganization.ts`'s `OrganizationBuffer` (consumed by
`OrganizationForm.tsx`, re-exported from `features/organizations/index.ts`) name the controlled-input
state before it's parsed into a request `XBuffer`. "Buffer" reads as a binary/IO concept; "draft" is the
clearer, unambiguous name for "user-entered values not yet validated or committed." `useESignature.ts`'s
doc comment ("Owns the signature buffer and its validity...") describes the same concept in prose, not a
type name, but is worth renaming alongside these two for consistency.

Renamed in the admin console's own copy of this pattern (`InviteDraft`, `ResolveDraft` —
`app/web/admin/src/features/{admins,moderation}/`) when this was raised; this entry is what's left.
Sibling debt in `app/shared` (`ReportBuffer`) and `app/web/customer` (`ReviewBuffer`) covers the rest of
the codebase.

**Resolves when:** `InviteBuffer` → `InviteDraft` and `OrganizationBuffer` → `OrganizationDraft` (type,
hook parameter names, the re-exports, `InviteForm.tsx`'s `InviteBuffer["role"]` usages), matching the
admin console's already-renamed pattern; update `useESignature.ts`'s comment to match.
