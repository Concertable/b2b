import { createFileRoute } from "@tanstack/react-router";
import { StripeReturnPage } from "@concertable/web/shared/features/payments";

export const Route = createFileRoute("/stripe-return")({
  component: StripeReturnPage,
});
