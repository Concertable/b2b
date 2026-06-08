import { createFileRoute } from "@tanstack/react-router";
import { StripeReturnPage } from "@/features/payments";

export const Route = createFileRoute("/stripe-return")({
  component: StripeReturnPage,
});
