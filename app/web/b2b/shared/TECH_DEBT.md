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
