import { Link } from "@tanstack/react-router";
import { Button } from "@concertable/web/shared/components/ui/button";
import { useOrganizationQuery } from "../hooks/useOrganizationQuery";

export function TaxDetailsBanner() {
  const { data: organization, isLoading } = useOrganizationQuery();

  // Presence is completeness — nag only when the tenant has no tax data yet.
  if (isLoading || !organization || organization.taxCompliance) return null;

  return (
    <div className="border-border bg-card flex items-center justify-between gap-4 rounded-xl border p-4">
      <div className="space-y-0.5">
        <p className="font-medium">Complete your tax details to get paid</p>
        <p className="text-muted-foreground text-sm">
          We're required to hold your tax details before we can pay out your
          earnings. Add them now so your payouts aren't held up.
        </p>
      </div>
      <Button size="sm" asChild>
        <Link to="/settings/organization">Complete tax details</Link>
      </Button>
    </div>
  );
}
