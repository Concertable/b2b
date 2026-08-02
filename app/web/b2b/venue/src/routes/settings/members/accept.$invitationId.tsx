import { createFileRoute } from "@tanstack/react-router";
import { AcceptInvitationPage } from "@b2b/features/members";
import { requireB2bAuth } from "@b2b/features/tenant";

export const Route = createFileRoute("/settings/members/accept/$invitationId")({
  beforeLoad: requireB2bAuth,
  component: RouteComponent,
});

function RouteComponent() {
  const { invitationId } = Route.useParams();
  return <AcceptInvitationPage invitationId={invitationId} />;
}
