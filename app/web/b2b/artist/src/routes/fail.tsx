import { createFileRoute } from "@tanstack/react-router";
import { FailPage } from "@/features/payments";

export const Route = createFileRoute("/fail")({
  component: FailPage,
});
