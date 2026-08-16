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

export interface OpportunityApplicationMetrics {
  opportunity: OpportunitySummary;
  applicationCount: number;
  daysUntilDeadline: number;
}

export interface OpportunityMatch {
  id: number;
  venueId: number;
  venueName: string;
  county: string;
  town: string;
  startDate: string;
  endDate: string;
  genres: Genre[];
  deal: Deal;
  fitScore: number;
  href: string;
}
