import { createFileRoute } from "@tanstack/react-router";
import { VerificationPage } from "@concertable/web-b2b/features/verification";

export const Route = createFileRoute("/_artist/settings/verification")({
  component: VerificationPage,
});
