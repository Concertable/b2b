import { createFileRoute } from "@tanstack/react-router";
import { ArtistDetailsPage } from "@/features/artists";

export const Route = createFileRoute("/_venue/find/artist/$id")({
  params: {
    parse: (params) => ({ id: Number(params.id) }),
    stringify: (params) => ({ id: String(params.id) }),
  },
  component: function () {
    const { id } = Route.useParams();
    return <ArtistDetailsPage id={id} />;
  },
});
