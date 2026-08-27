# app/web/b2b/shared — Technical Debt

## LOW

### `organizationApi.get` returns `undefined` on HTTP 204, which throws in a TanStack Query v5 `queryFn`

`features/organizations/api/organizationApi.ts` returns `undefined` when the organization GET responds
204. `useOrganizationQuery` passes `organizationApi.get` straight as `queryFn`, and TanStack Query v5
throws `Query data cannot be undefined` for a query function that resolves to `undefined`. Latent today —
a signed-in B2B manager always has an organization (org setup is mandatory before a tenant exists), so the
204 branch is unreachable in practice. The identical pattern in `features/verification/api/verificationApi.ts`
*was* reachable (a tenant that has never submitted verification is the common case) and was fixed to return
`null` on `Feature/launch_tenant-verification`.

**Resolves when:** `organizationApi.get` returns `Organization | null` (`null` on 204), matching
`verificationApi.get` and the `apiClient.getOptional` (`data: null`) convention.

### API modules repeat their resource's base route as a string literal per method instead of a `BASE` const

`membersApi.ts` and `organizationApi.ts` each call `apiClient` with the same route prefix written out
fresh in every method instead of declared once as a `const BASE = "/..."` and interpolated. A rename of
the resource's route needs a find-and-replace across every method instead of one edit.

Found while fixing the same pattern in the admin console's `moderationApi.ts`/`adminApi.ts`
(`app/web/admin/src/features/{moderation,admins}/api/`) — see `app/shared/TECH_DEBT.md` for the sibling
entry covering `app/shared`'s own api modules with this shape.

**Resolves when:** each listed file gets a `const BASE = "/..."` at the top, with every method
interpolating it instead of restating the literal.
