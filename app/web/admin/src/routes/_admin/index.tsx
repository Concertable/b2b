import { createFileRoute } from "@tanstack/react-router";
import { AdminPage } from "../../features/admins";

export const Route = createFileRoute("/_admin/")({
  component: AdminPage,
});
