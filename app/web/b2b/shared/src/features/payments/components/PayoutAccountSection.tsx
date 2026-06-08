import { ExternalLink, CheckCircle, XCircle, Clock } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { useMountEffect } from "@concertable/shared/hooks/useMountEffect";
import {
  usePayoutAccountStatusQuery,
  useStripeOnboardingQuery,
} from "../hooks/usePayoutAccountQuery";

export function PayoutAccountSection() {
  const {
    data: accountStatus,
    refetch: refetchStatus,
    isLoading: isStatusLoading,
  } = usePayoutAccountStatusQuery(true);
  const { refetch: openOnboarding, isFetching } = useStripeOnboardingQuery();

  useMountEffect(() => {
    function handleMessage(event: MessageEvent) {
      if (event.origin !== window.location.origin) return;
      if (event.data?.type === "stripe_return")
        refetchStatus().then(({ data: status }) => {
          if (status === "Verified") toast.success("Payout account verified");
          else
            toast.info(
              "Setup incomplete — finish the remaining steps to get verified",
            );
        });
      else if (event.data?.type === "stripe_refresh")
        openOnboarding().then(({ data: link }) => {
          if (link) window.open(link, "_blank");
        });
    }

    window.addEventListener("message", handleMessage);
    return () => window.removeEventListener("message", handleMessage);
  });

  return (
    <>
      <Separator />

      <div className="space-y-4">
        <h3 className="font-medium">Payout Account</h3>
        <p className="text-muted-foreground text-sm">
          Connect your Stripe account to receive payments for concerts and
          bookings.
        </p>
        <div className="flex items-center gap-3 pt-2">
          {isStatusLoading ? (
            <div className="text-muted-foreground size-5 animate-spin rounded-full border-2 border-current border-t-transparent" />
          ) : accountStatus === "Verified" ? (
            <span className="flex items-center gap-1 text-sm text-green-600">
              <CheckCircle className="size-4" /> Verified
            </span>
          ) : accountStatus === "Pending" ? (
            <span className="flex items-center gap-1 text-sm text-amber-500">
              <Clock className="size-4" /> Pending verification
            </span>
          ) : accountStatus === "NotVerified" ? (
            <span className="text-destructive flex items-center gap-1 text-sm">
              <XCircle className="size-4" /> Not verified
            </span>
          ) : null}
          <Button
            onClick={() =>
              openOnboarding().then(({ data: link }) => {
                if (link) window.open(link, "_blank");
              })
            }
            disabled={isFetching || isStatusLoading}
          >
            <ExternalLink className="size-4" />
            {isFetching || isStatusLoading
              ? "Loading..."
              : accountStatus === "Verified"
                ? "Manage Payout Account"
                : "Set up Payout Account"}
          </Button>
        </div>
      </div>
    </>
  );
}
