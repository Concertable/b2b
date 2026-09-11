import { createFileRoute } from "@tanstack/react-router";
import { FailPage } from "@concertable/web/features/payments";

export const Route = createFileRoute("/fail")({
  component: FailPage,
});
