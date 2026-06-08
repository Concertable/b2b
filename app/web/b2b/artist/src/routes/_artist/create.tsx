import { createFileRoute } from "@tanstack/react-router";
import { CreateArtistPage } from "../../features/artist";

export const Route = createFileRoute("/_artist/create")({
  component: CreateArtistPage,
});
