import { useQuery } from "@tanstack/react-query";
import stripeAccountApi from "@/features/payments/api/stripeAccountApi";

export function usePayoutAccountStatusQuery(enabled: boolean) {
  return useQuery({
    queryKey: ["stripe", "account-status"],
    queryFn: stripeAccountApi.getAccountStatus,
    enabled,
    staleTime: 0,
    gcTime: 0,
  });
}

export function useStripeOnboardingQuery() {
  return useQuery({
    queryKey: ["stripe", "onboarding-link"],
    queryFn: stripeAccountApi.getOnboardingLink,
    enabled: false,
    throwOnError: false,
  });
}
