import { createFileRoute } from "@tanstack/react-router";
import { AcceptInvitationPage } from "@concertable/web-b2b/features/members";
import { requireLocalB2bAuth } from "@concertable/web-b2b/features/tenant";

export const Route = createFileRoute("/settings/members/accept/$invitationId")({
  beforeLoad: requireLocalB2bAuth,
  component: RouteComponent,
});

function RouteComponent() {
  const { invitationId } = Route.useParams();
  return <AcceptInvitationPage invitationId={invitationId} tenantType="Artist" />;
}
