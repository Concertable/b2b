import { Button } from "@/components/ui/button";
import { Plus } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import { defaultContract } from "@b2b/features/contracts";
import { useOpportunities } from "../../hooks/useOpportunities";
import { OpportunityEditCard } from "./OpportunityCard";
import dayjs from "dayjs";
import type { OpportunityDraft } from "@/features/concerts/types";

interface Props {
  venueId: number;
}

function buildDraft(): OpportunityDraft {
  const start = dayjs().add(7, "day").hour(19).minute(0).second(0).millisecond(0);
  const end = start.add(4, "hour");
  return {
    startDate: start.toISOString(),
    endDate: end.toISOString(),
    genres: [],
    contract: defaultContract("flatFee"),
  };
}

export function OpportunityList({ venueId }: Readonly<Props>) {
  const { opportunities, drafts, isLoading, addDraft, opportunityActions, draftActions } =
    useOpportunities(venueId);

  if (isLoading) return <OpportunityListSkeleton />;

  return (
    <div className="space-y-3">
      {opportunities.map((opportunity, i) => (
        <OpportunityEditCard
          key={`existing-${opportunity.id}`}
          opportunity={opportunity}
          onRemove={() => opportunityActions.remove(i)}
          onSetDates={(s, e) => opportunityActions.setDates(i, s, e)}
          onSetContractType={(t) => opportunityActions.setContractType(i, t)}
          onSetContract={(c) => opportunityActions.setContract(i, c)}
          onSetPaymentMethod={(m) => opportunityActions.setPaymentMethod(i, m)}
          onToggleGenre={(g) => opportunityActions.toggleGenre(i, g)}
        />
      ))}

      {drafts.map((draft, i) => (
        <OpportunityEditCard
          key={`draft-${i}`}
          opportunity={draft}
          onRemove={() => draftActions.remove(i)}
          onSetDates={(s, e) => draftActions.setDates(i, s, e)}
          onSetContractType={(t) => draftActions.setContractType(i, t)}
          onSetContract={(c) => draftActions.setContract(i, c)}
          onSetPaymentMethod={(m) => draftActions.setPaymentMethod(i, m)}
          onToggleGenre={(g) => draftActions.toggleGenre(i, g)}
        />
      ))}

      <Button
        variant="outline"
        onClick={() => addDraft(buildDraft())}
        data-testid="opportunity-add"
      >
        <Plus className="mr-1.5 size-4" />
        Add opportunity
      </Button>
    </div>
  );
}

function OpportunityListSkeleton() {
  return (
    <div className="space-y-3">
      {Array.from({ length: 3 }).map((_, i) => (
        <Skeleton key={i} className="h-24 w-full rounded-xl" />
      ))}
    </div>
  );
}
