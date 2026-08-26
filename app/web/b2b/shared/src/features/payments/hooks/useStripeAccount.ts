import {
  usePayoutAccountStatusQuery,
  useStripeOnboardingQuery,
} from "./usePayoutAccountQuery";

export function useStripeAccount() {
  const { data: accountStatus, isLoading } = usePayoutAccountStatusQuery(true);
  const { refetch, isFetching: isLoadingLink } = useStripeOnboardingQuery();

  return {
    isVerified: accountStatus === "verified",
    isLoading,
    isLoadingLink,
    beginOnboarding: refetch,
  };
}
