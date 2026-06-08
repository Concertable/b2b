import { createFileRoute } from "@tanstack/react-router";
import { MyArtistPage } from "../../../features/artist";

export const Route = createFileRoute("/_artist/my/")({
  component: MyArtistPage,
});
