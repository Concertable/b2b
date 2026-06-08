import { createFileRoute } from "@tanstack/react-router";
import { VenueDetailsPage } from "../../../features/venues";

export const Route = createFileRoute("/_artist/find/venue/$id")({
  params: {
    parse: (params) => ({ id: Number(params.id) }),
    stringify: (params) => ({ id: String(params.id) }),
  },
  component: function () {
    const { id } = Route.useParams();
    return <VenueDetailsPage id={id} />;
  },
});
