import {
  redirectToBusiness,
  requireAuth,
  requireBusinessAuth,
} from "@/features/auth";
import identityApi from "./api/identityApi";
import { tenantSession } from "./tenantSession";
import type { TenantType } from "./types";

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
  await requireB2bAuth();
  const resolution = tenantSession.resolve(tenantType);
  if (resolution.memberships.length === 0) return redirectToBusiness();
  return { selectionRequired: resolution.selectionRequired };
}
