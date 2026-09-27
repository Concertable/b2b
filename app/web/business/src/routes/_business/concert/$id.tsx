import { createFileRoute } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { currentPrivateQueryKey } from "@concertable/b2b/features/tenant";
import { apiClient } from "@concertable/shared/lib/apiClient";

interface ConcertSummary {
  readonly id: number;
  readonly name: string;
  readonly startDate: string;
  readonly endDate: string;
  readonly venueName: string;
  readonly artistName: string;
  readonly state: string;
}

function ConcertSummaryPage() {
  const { id } = Route.useParams();
  const summary = useQuery({
    queryKey: currentPrivateQueryKey("concert", id, "summary"),
    queryFn: async () =>
      (await apiClient.get<ConcertSummary>(`/concert/${id}/summary`)).data,
  });
  if (summary.isLoading) return <div className="p-8">Loading summary…</div>;
  if (!summary.data)
    return <div className="p-8 text-destructive">Concert summary unavailable.</div>;
  return (
    <main className="mx-auto max-w-3xl space-y-4 p-8">
      <h1 className="text-3xl font-semibold">{summary.data.name}</h1>
      <div className="rounded-lg border border-border p-5">
        <p>{summary.data.artistName}</p>
        <p>{summary.data.venueName}</p>
        <p className="mt-2 text-muted-foreground">{summary.data.state}</p>
      </div>
    </main>
  );
}

export const Route = createFileRoute("/_business/concert/$id")({
  params: {
    parse: (params) => ({ id: Number(params.id) }),
    stringify: (params) => ({ id: String(params.id) }),
  },
  component: ConcertSummaryPage,
});
