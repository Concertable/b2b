import { useSyncUser } from "@/features/user";
import { apiClient } from "@concertable/shared/lib/apiClient";
import type { B2bIdentity } from "./types";

export async function getB2bIdentity(): Promise<B2bIdentity> {
  const { data } = await apiClient.get<B2bIdentity>("/auth/me");
  return data;
}

export function useSyncB2bIdentity(): void {
  useSyncUser(getB2bIdentity);
}
