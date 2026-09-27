import { createFileRoute } from "@tanstack/react-router";
import { AcceptInvitationPage } from "@concertable/web-b2b/features/members";
import { requireLocalB2bAuth } from "@concertable/web-b2b/features/tenant";

function AcceptInvitationRoute() {
  const { invitationId } = Route.useParams();
  return <AcceptInvitationPage invitationId={invitationId} />;
}

export const Route = createFileRoute("/settings/members/accept/$invitationId")({
  beforeLoad: requireLocalB2bAuth,
  component: AcceptInvitationRoute,
});
