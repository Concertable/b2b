import { createFileRoute } from "@tanstack/react-router";
import { StripeRefreshPage } from "@/features/payments";

export const Route = createFileRoute("/stripe-refresh")({
  component: StripeRefreshPage,
});
