import { createFileRoute } from "@tanstack/react-router";
import { MembersPage } from "@concertable/web-b2b/features/members";

export const Route = createFileRoute("/_business/settings/members")({
  component: () => (
    <MembersPage
      title="Members"
      description="People who can access this organization, and pending invitations."
    />
  ),
});
