import { useQuery } from "@tanstack/react-query";
import selfBillingAgreementApi from "../api/selfBillingAgreementApi";

export function useSelfBillingAgreementQuery() {
  return useQuery({
    queryKey: ["self-billing-agreement"],
    queryFn: selfBillingAgreementApi.get,
  });
}
