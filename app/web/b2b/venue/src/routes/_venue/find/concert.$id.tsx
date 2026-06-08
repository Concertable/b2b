import { createFileRoute } from "@tanstack/react-router";
import { ConcertDetailsPage } from "@/features/concerts";

export const Route = createFileRoute("/_venue/find/concert/$id")({
  params: {
    parse: (params) => ({ id: Number(params.id) }),
    stringify: (params) => ({ id: String(params.id) }),
  },
  component: function () {
    const { id } = Route.useParams();
    return <ConcertDetailsPage id={id} />;
  },
});
