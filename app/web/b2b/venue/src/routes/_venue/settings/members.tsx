import { createFileRoute } from "@tanstack/react-router";
import { MembersPage } from "@b2b/features/members";

export const Route = createFileRoute("/_venue/settings/members")({
  component: () => (
    <MembersPage
      persona="Venue"
      title="Members"
      description="People who can access this organization, and pending invitations."
    />
  ),
});
