import { useQuery } from "@tanstack/react-query";
import { currentPrivateQueryKey } from "@concertable/b2b/features/tenant";
import selfBillingAgreementApi from "../api/selfBillingAgreementApi";

export function useSelfBillingAgreementQuery() {
  return useQuery({
    queryKey: currentPrivateQueryKey("self-billing-agreement"),
    queryFn: selfBillingAgreementApi.get,
  });
}
