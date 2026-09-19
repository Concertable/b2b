import { createFileRoute } from "@tanstack/react-router";
import { OrganizationPage } from "@concertable/web-b2b/features/organizations";

export const Route = createFileRoute("/_business/settings/organization")({
  component: () => (
    <OrganizationPage
      title="Organization"
      description="Your organization's contact, legal and tax details."
    />
  ),
});
