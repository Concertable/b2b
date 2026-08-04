import { Link } from "@tanstack/react-router";
import { Button } from "@/components/ui/button";
import { useSelfBillingAgreementQuery } from "../hooks/useSelfBillingAgreementQuery";

export function SelfBillingAgreementBanner() {
  const { data: agreement, isLoading } = useSelfBillingAgreementQuery();

  if (isLoading || !agreement) return null;

  // Nag only when action is due: never signed, lapsed, or a renew affordance surfaced (within the window).
  const needsAction = agreement.status !== "Active" || agreement.actions.renew != null;
  if (!needsAction) return null;

  const isRenewal = agreement.status !== "None";

  return (
    <div className="border-border bg-card flex items-center justify-between gap-4 rounded-xl border p-4">
      <div className="space-y-0.5">
        <p className="font-medium">
          {isRenewal
            ? "Renew your self-billing agreement"
            : "Set up self-billing so we can invoice you"}
        </p>
        <p className="text-muted-foreground text-sm">
          {isRenewal
            ? "Yours is expiring — renew it so your settlements keep issuing invoices and paying out."
            : "We raise VAT invoices on your behalf under a self-billing agreement. Sign it so your settlements can be invoiced and paid out."}
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
