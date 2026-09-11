import type { Genre } from "@concertable/shared/types/common";
import type { Deal } from "@b2b/features/deals";

export interface OpportunitySummary {
  id: number;
  venueId: number;
  venueName: string;
  startDate: string;
  endDate: string;
  genres: Genre[];
  deal: Deal;
}
