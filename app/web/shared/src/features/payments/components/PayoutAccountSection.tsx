import { ExternalLink, CheckCircle, XCircle, Clock } from "lucide-react";
import { Button } from "@concertable/web/shared/components/ui/button";
import { Separator } from "@concertable/web/shared/components/ui/separator";
import { usePayoutAccount } from "../hooks/usePayoutAccount";

export function PayoutAccountSection() {
  const { accountStatus, isLoading, isLinkLoading, openOnboarding } =
    usePayoutAccount();
  const isBusy = isLinkLoading || isLoading;

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
          {isLoading ? (
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
          <Button onClick={() => void openOnboarding()} disabled={isBusy}>
            <ExternalLink className="size-4" />
            {isBusy
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
