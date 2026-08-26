import { redirect } from "@tanstack/react-router";
import { requireAuth } from "@concertable/web/features/auth";
import { meQueryKey } from "@concertable/web/features/user";
import { queryClient } from "@concertable/web/lib/queryClient";
import identityApi from "./api/identityApi";
import type { Identity } from "./types";

export async function requireAdmin({
  location,
}: {
  location: { pathname: string };
}) {
  await requireAuth({ location, getMe: identityApi.getMe });
  const identity = queryClient.getQueryData<Identity>(meQueryKey);
  if (!identity?.isAdmin) throw redirect({ to: "/forbidden" });
}
