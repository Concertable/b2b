import { createFileRoute } from "@tanstack/react-router";
import { FindArtistPage } from "../../../features/search";
import { SearchSchema } from "@concertable/web/shared/features/search";

export const Route = createFileRoute("/_venue/find/")({
  component: FindArtistPage,
  validateSearch: SearchSchema(),
});
