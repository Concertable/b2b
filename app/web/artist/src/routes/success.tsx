import { createFileRoute } from "@tanstack/react-router";
import { SuccessPage } from "@/features/payments";

export const Route = createFileRoute("/success")({
  component: SuccessPage,
});
