import { createFileRoute } from "@tanstack/react-router";
import { MyApplicationsPage } from "../../../features/concerts";

export const Route = createFileRoute("/_artist/my/applications")({
  component: MyApplicationsPage,
});
