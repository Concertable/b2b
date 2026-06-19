import { createFileRoute } from "@tanstack/react-router";
import { OrganizationPage } from "@b2b/features/organizations";

export const Route = createFileRoute("/_venue/settings/organization")({
  component: () => (
    <OrganizationPage
      title="Organization"
      description="Your organization's legal and tax details, used for invoicing and payouts."
    />
  ),
});
