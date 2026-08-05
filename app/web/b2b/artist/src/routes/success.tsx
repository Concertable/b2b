import { createFileRoute } from "@tanstack/react-router";
import { SuccessPage } from "@concertable/web/shared/features/payments";

export const Route = createFileRoute("/success")({
  component: SuccessPage,
});
