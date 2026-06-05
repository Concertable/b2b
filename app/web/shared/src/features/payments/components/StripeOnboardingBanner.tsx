import { Button } from "@/components/ui/button";
import { useStripeAccount } from "../hooks/useStripeAccount";

export function StripeOnboardingBanner() {
  const { isVerified, isLoading, isLoadingLink, beginOnboarding } =
    useStripeAccount();

  if (isLoading || isVerified) return null;

  return (
    <div className="border-border bg-card flex items-center justify-between gap-4 rounded-xl border p-4">
      <div className="space-y-0.5">
        <p className="font-medium">Complete your payment setup</p>
        <p className="text-muted-foreground text-sm">
          You need to verify your Stripe account before you can send or receive
          payments.
        </p>
      </div>
      <Button size="sm" disabled={isLoadingLink} onClick={() => { void beginOnboarding(); }}>
        {isLoadingLink ? "Loading..." : "Set up payments"}
      </Button>
    </div>
  );
}
