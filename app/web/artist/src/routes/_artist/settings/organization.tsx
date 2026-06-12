import { createFileRoute } from "@tanstack/react-router";
import { OrganizationPage } from "@b2b/features/organizations";

export const Route = createFileRoute("/_artist/settings/organization")({
  component: () => (
    <OrganizationPage
      title="Business & tax details"
      description="The legal identity behind your bookings, used for invoicing and payouts."
    />
  ),
});
