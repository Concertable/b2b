import { toast } from "sonner";
import { useMountEffect } from "@concertable/shared/hooks/useMountEffect";
import {
  usePayoutAccountStatusQuery,
  useStripeOnboardingQuery,
} from "./usePayoutAccountQuery";

export function usePayoutAccount() {
  const {
    data: accountStatus,
    refetch: refetchStatus,
    isLoading,
  } = usePayoutAccountStatusQuery(true);
  const { refetch: fetchOnboardingLink, isFetching: isLinkLoading } =
    useStripeOnboardingQuery();

  const openOnboarding = async () => {
    const { data: link } = await fetchOnboardingLink();
    if (link) window.open(link, "_blank");
  };

  useMountEffect(() => {
    function handleMessage(event: MessageEvent) {
      if (event.origin !== window.location.origin) return;
      if (event.data?.type === "stripe_return")
        refetchStatus().then(({ data: status }) => {
          if (status === "verified") toast.success("Payout account verified");
          else
            toast.info(
              "Setup incomplete — finish the remaining steps to get verified",
            );
        });
      else if (event.data?.type === "stripe_refresh") void openOnboarding();
    }

    window.addEventListener("message", handleMessage);
    return () => window.removeEventListener("message", handleMessage);
  });

  return { accountStatus, isLoading, isLinkLoading, openOnboarding };
}
