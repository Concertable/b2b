import { useSyncUser } from "@/features/user";
import identityApi from "./api/identityApi";

export function useSyncB2bIdentity(): void {
  useSyncUser(identityApi.getMe);
}
