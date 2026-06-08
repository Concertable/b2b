import { createFileRoute } from "@tanstack/react-router";
import { MyConcertPage } from "@b2b/features/concerts";

export const Route = createFileRoute("/_artist/my/concerts/concert/$id")({
  params: {
    parse: (params) => ({ id: Number(params.id) }),
    stringify: (params) => ({ id: String(params.id) }),
  },
  component: () => {
    const { id } = Route.useParams();
    return <MyConcertPage id={id} />;
  },
});
