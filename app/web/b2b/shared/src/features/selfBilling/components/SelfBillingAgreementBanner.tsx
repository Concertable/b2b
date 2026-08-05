import { Link } from "@tanstack/react-router";
import { Button } from "@concertable/web/shared/components/ui/button";
import { useSelfBillingAgreementQuery } from "../hooks/useSelfBillingAgreementQuery";

export function SelfBillingAgreementBanner() {
  const { data: agreement, isLoading } = useSelfBillingAgreementQuery();

  if (isLoading || !agreement) return null;

  // Nag only when action is due: never signed, lapsed, or a renew affordance surfaced (within the window).
  const needsAction = agreement.status !== "Active" || agreement.actions.renew != null;
  if (!needsAction) return null;

  const isRenewal = agreement.status !== "None";
  const isInForce = agreement.status === "Active";

  return (
    <div className="border-border bg-card flex items-center justify-between gap-4 rounded-xl border p-4">
      <div className="space-y-0.5">
        <p className="font-medium">
          {isRenewal
            ? "Renew your self-billing agreement"
            : "Set up self-billing so we can invoice you"}
        </p>
        <p className="text-muted-foreground text-sm">
          {isInForce
            ? "Yours is expiring soon — renew it now so your completed gigs keep being invoiced and paid out."
            : isRenewal
              ? "Yours has expired, so your completed gigs can't be invoiced or paid out until you renew it."
              : "We raise VAT invoices on your behalf under a self-billing agreement. Until you sign it, your completed gigs can't be invoiced or paid out."}
        </p>
      </div>
      <Button size="sm" asChild>
        <Link to="/settings/self-billing-agreement">
          {isRenewal ? "Renew agreement" : "Set up self-billing"}
        </Link>
      </Button>
    </div>
  );
}
