import {
  redirectToBusiness,
  requireAuth,
  requireBusinessAuth,
} from "@concertable/web/features/auth";
import {
  identityApi,
  tenantSession,
} from "@concertable/b2b/features/tenant";
import type { TenantType } from "@concertable/b2b/features/tenant/types";
import { tenantSessionReady } from "./webTenantSession";

function requireB2bAuth(): Promise<void> {
  return requireBusinessAuth(identityApi.getMe);
}

export function requireLocalB2bAuth({
  location,
}: {
  location: { pathname: string };
}) {
  return requireAuth({ location, getMe: identityApi.getMe });
}

export async function resolveTenantRoute(
  tenantType: TenantType,
): Promise<{ selectionRequired: boolean }> {
  await Promise.all([requireB2bAuth(), tenantSessionReady]);
  const resolution = await tenantSession.resolve(tenantType);
  if (resolution.memberships.length === 0) return redirectToBusiness();
  return { selectionRequired: resolution.selectionRequired };
}
