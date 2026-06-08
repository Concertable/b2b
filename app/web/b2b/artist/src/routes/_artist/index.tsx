import { createFileRoute } from "@tanstack/react-router";
import { ArtistDashboardPage } from "../../features/dashboard";

export const Route = createFileRoute("/_artist/")({
  component: ArtistDashboardPage,
});
