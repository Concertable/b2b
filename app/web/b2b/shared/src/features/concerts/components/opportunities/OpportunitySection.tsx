import type { ReactNode } from "react";
import { Editable } from "@/components/editable/Editable";
import { Skeleton } from "@/components/ui/skeleton";
import { useAllOpportunitiesQuery } from "../../hooks/useOpportunitiesQuery";
import { OpportunityCard } from "./OpportunityCard";
import { OpportunityList } from "./OpportunityList";
import type { Opportunity } from "@/features/concerts/types";

interface Props {
  venueId: number;
  renderActions?: (opportunity: Opportunity) => ReactNode;
}

export function OpportunitySection({ venueId, renderActions }: Readonly<Props>) {
  const { data, isLoading, isError } = useAllOpportunitiesQuery(venueId);

  if (isLoading) return <OpportunitySectionSkeleton />;
  if (isError)
    return <p className="text-destructive">Failed to load opportunities.</p>;

  return (
    <Editable
      view={
        data && data.length > 0 ? (
          <div className="space-y-3">
            {data.map((opportunity) => (
              <OpportunityCard
                key={opportunity.id}
                opportunity={opportunity}
                actions={renderActions?.(opportunity)}
              />
            ))}
          </div>
        ) : (
          <p className="text-muted-foreground">No opportunities yet.</p>
        )
      }
      edit={<OpportunityList venueId={venueId} />}
    />
  );
}

function OpportunitySectionSkeleton() {
  return (
    <div className="space-y-3">
      {Array.from({ length: 3 }).map((_, i) => (
        <div
          key={i}
          className="border-border bg-card space-y-3 rounded-xl border p-4"
        >
          <div className="flex items-start justify-between gap-4">
            <div className="space-y-1.5">
              <Skeleton className="h-4 w-48" />
              <Skeleton className="h-4 w-32" />
            </div>
            <div className="flex gap-2">
              <Skeleton className="h-8 w-28" />
              <Skeleton className="h-8 w-24" />
            </div>
          </div>
          <div className="flex gap-1.5">
            <Skeleton className="h-5 w-16 rounded-full" />
            <Skeleton className="h-5 w-20 rounded-full" />
          </div>
        </div>
      ))}
    </div>
  );
}
