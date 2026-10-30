import { createFileRoute } from "@tanstack/react-router";
import { ConcertOperationsPage } from "@concertable/web-b2b/features/concerts";

export const Route = createFileRoute("/_artist/my/concerts/concert/$id")({
  params: {
    parse: (params) => ({ id: Number(params.id) }),
    stringify: (params) => ({ id: String(params.id) }),
  },
  component: () => {
    const { id } = Route.useParams();
    return <ConcertOperationsPage id={id} />;
  },
});
