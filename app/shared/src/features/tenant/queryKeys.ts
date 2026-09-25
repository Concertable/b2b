import type { Query, QueryClient } from "@tanstack/react-query";
import { tenantSession } from "./tenantSession";
import type { TenantSession } from "./types";

export const privateQueryKey = (
  session: TenantSession,
  ...parts: ReadonlyArray<unknown>
) =>
  [
    "tenant",
    session.tenantId,
    session.membershipId,
    session.permissionVersion,
    ...parts,
  ] as const;

export const currentPrivateQueryKey = (...parts: ReadonlyArray<unknown>) => {
  const session = tenantSession.current();
  return session === undefined
    ? (["tenant", "unselected", "unselected", 0, ...parts] as const)
    : privateQueryKey(session, ...parts);
};

export const isPrivateQuery = (query: Query) => query.queryKey[0] === "tenant";

export async function settlePendingMutations(queryClient: QueryClient) {
  if (queryClient.isMutating() === 0) return;
  await new Promise<void>((resolve) => {
    let unsubscribe: () => void = () => undefined;
    const settle = () => {
      if (queryClient.isMutating() !== 0) return;
      unsubscribe();
      resolve();
    };
    unsubscribe = queryClient.getMutationCache().subscribe(settle);
    settle();
  });
}
