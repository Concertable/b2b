import { createFileRoute } from "@tanstack/react-router";
import { VenueDashboardPage } from "../../features/dashboard";

export const Route = createFileRoute("/_venue/")({
  component: VenueDashboardPage,
});
