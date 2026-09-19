import { useQuery } from "@tanstack/react-query";
import { currentPrivateQueryKey } from "@concertable/b2b/features/tenant";
import stripeAccountApi from "@concertable/web/features/payments/api/stripeAccountApi";

export function usePayoutAccountStatusQuery(enabled: boolean) {
  return useQuery({
    queryKey: currentPrivateQueryKey("stripe", "account-status"),
    queryFn: stripeAccountApi.getAccountStatus,
    enabled,
    staleTime: 0,
    gcTime: 0,
  });
}

export function useStripeOnboardingQuery() {
  return useQuery({
    queryKey: currentPrivateQueryKey("stripe", "onboarding-link"),
    queryFn: stripeAccountApi.getOnboardingLink,
    enabled: false,
    throwOnError: false,
  });
}
