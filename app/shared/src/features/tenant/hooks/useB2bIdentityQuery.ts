import { useQuery } from "@tanstack/react-query";
import identityApi from "../api/identityApi";

export const b2bIdentityKeys = {
  all: () => ["auth", "me"] as const,
};

export function useB2bIdentityQuery() {
  return useQuery({
    queryKey: b2bIdentityKeys.all(),
    queryFn: identityApi.getMe,
  });
}
