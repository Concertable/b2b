import { useQuery } from "@tanstack/react-query";
import verificationApi from "../api/verificationApi";
import { verificationKeys } from "./verificationKeys";

export function useVerificationQuery() {
  return useQuery({
    queryKey: verificationKeys.status,
    queryFn: verificationApi.get,
  });
}
