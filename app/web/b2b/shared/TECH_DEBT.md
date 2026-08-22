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

### Hand-rolled `useState` + manual zod `safeParse` per field, where `react-hook-form` is now the standard

`useInviteMember.ts`'s `InviteBuffer` (consumed by `InviteForm.tsx`, re-exported from
`features/members/index.ts`) and `useOrganization.ts`'s `OrganizationBuffer` (consumed by
`OrganizationForm.tsx`, re-exported from `features/organizations/index.ts`) hand-roll the write-boundary
pattern: a `useState` per field, a manually named pre-validation type ("buffer" also reads as a
binary/IO concept), and a facade hook exposing `validate`/`submit` that calls `schema.safeParse` by hand.
`react-hook-form` + `@hookform/resolvers/zod` does this same job with less code and no separate draft
type at all. `useESignature.ts`'s doc comment ("Owns the signature buffer and its validity...") describes
the same concept in prose; its canvas-drawn signature isn't a normal text/select field though, so it may
stay a `useState` even after this migration — worth a second look, not a given.

Migrated in the admin console's own copy of this pattern (`InviteForm`/`useInviteAdmin`,
`ResolveReportDialog`/`useResolveReport` — `app/web/admin/src/features/{admins,moderation}/`) when this
was raised; that's the model to follow. Sibling debt in `app/shared` (`useReportMessage`) and
`app/web/customer` (`useAddReview`) covers the rest of the codebase.

**Resolves when:** `InviteForm`/`useInviteMember` and `OrganizationForm`/`useOrganization` use `useForm`
+ `zodResolver` the same way, `InviteBuffer`/`OrganizationBuffer` and the manual `validate`/`safeParse`
calls disappear entirely rather than getting renamed; revisit `useESignature.ts` on its own merits.
