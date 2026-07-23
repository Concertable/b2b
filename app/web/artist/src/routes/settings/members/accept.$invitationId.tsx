import { createFileRoute } from "@tanstack/react-router";
import { requireAuth } from "@/features/auth";
import { AcceptInvitationPage } from "@b2b/features/members";

export const Route = createFileRoute("/settings/members/accept/$invitationId")({
  beforeLoad: ({ location }) => requireAuth({ location }),
  component: RouteComponent,
});

function RouteComponent() {
  const { invitationId } = Route.useParams();
  return <AcceptInvitationPage invitationId={invitationId} />;
}
