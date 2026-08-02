import {
  redirectToBusiness,
  requireAuth,
  requireBusinessAuth,
} from "@/features/auth";
import identityApi from "./api/identityApi";
import { getCachedMemberships } from "./identityCache";
import { filterMembershipsByPersona } from "./memberships";
import type { TenantType } from "./types";

export function requireB2bAuth(): Promise<void> {
  return requireBusinessAuth(identityApi.getMe);
}

export function requireLocalB2bAuth({
  location,
}: {
  location: { pathname: string };
}) {
  return requireAuth({ location, getMe: identityApi.getMe });
}

export async function requireBusinessPersona(
  persona: TenantType,
): Promise<void> {
  await requireB2bAuth();
  if (
    filterMembershipsByPersona(getCachedMemberships(), persona).length === 0
  )
    return redirectToBusiness();
}
