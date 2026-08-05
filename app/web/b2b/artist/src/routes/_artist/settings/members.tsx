import { createFileRoute } from "@tanstack/react-router";
import { MembersPage } from "@concertable/b2b/web/shared/features/members";

export const Route = createFileRoute("/_artist/settings/members")({
  component: () => (
    <MembersPage
      tenantType="Artist"
      title="Members"
      description="People who can access this organization, and pending invitations."
    />
  ),
});
