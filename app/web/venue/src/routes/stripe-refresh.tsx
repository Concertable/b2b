import { createFileRoute } from "@tanstack/react-router";
import { StripeRefreshPage } from "@concertable/web/shared/features/payments";

export const Route = createFileRoute("/stripe-refresh")({
  component: StripeRefreshPage,
});
