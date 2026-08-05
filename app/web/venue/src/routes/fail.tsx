import { createFileRoute } from "@tanstack/react-router";
import { FailPage } from "@concertable/web/shared/features/payments";

export const Route = createFileRoute("/fail")({
  component: FailPage,
});
