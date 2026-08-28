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
