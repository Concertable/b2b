import type { ReportCategory } from "@concertable/shared/features/messaging";

export type { ReportCategory };

export type ReportOutcome = "noActionTaken" | "contentRemoved" | "referredToLegal";

export const REPORT_OUTCOME_LABELS: Record<ReportOutcome, string> = {
  noActionTaken: "No action taken",
  contentRemoved: "Content removed",
  referredToLegal: "Referred to legal",
};

export interface ContentReport {
  id: number;
  reference: string;
  messageId: number;
  reporterTenantId: string;
  reportedTenantId: string;
  category: ReportCategory;
  details?: string;
  messageExcerpt: string;
  submittedAt: string;
  outcome?: ReportOutcome;
  resolvedAt?: string;
  resolutionNotes?: string;
}
