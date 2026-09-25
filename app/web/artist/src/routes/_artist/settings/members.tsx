import { createFileRoute } from "@tanstack/react-router";
import { MembersPage } from "@concertable/web-b2b/features/members";

export const Route = createFileRoute("/_artist/settings/members")({
  component: () => (
    <MembersPage
      businessActivity="artist"
      title="Members"
      description="People who can access this organization, and pending invitations."
    />
  ),
});
