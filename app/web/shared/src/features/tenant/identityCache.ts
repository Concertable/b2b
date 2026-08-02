import { meQueryKey } from "@/features/user/hooks/useSyncUser";
import { queryClient } from "@/lib/queryClient";
import type { B2bIdentity, Membership } from "./types";

const EMPTY_MEMBERSHIPS: ReadonlyArray<Membership> = [];

export function getCachedMemberships(): ReadonlyArray<Membership> {
  return (
    queryClient.getQueryData<B2bIdentity>(meQueryKey)?.memberships ??
    EMPTY_MEMBERSHIPS
  );
}
