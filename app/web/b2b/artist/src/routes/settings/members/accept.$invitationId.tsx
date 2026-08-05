import { createFileRoute } from "@tanstack/react-router";
import { AcceptInvitationPage } from "@concertable/b2b/web/shared/features/members";
import { requireLocalB2bAuth } from "@concertable/b2b/web/shared/features/tenant";

export const Route = createFileRoute("/settings/members/accept/$invitationId")({
  beforeLoad: requireLocalB2bAuth,
  component: RouteComponent,
});

function RouteComponent() {
  const { invitationId } = Route.useParams();
  return <AcceptInvitationPage invitationId={invitationId} tenantType="Artist" />;
}
