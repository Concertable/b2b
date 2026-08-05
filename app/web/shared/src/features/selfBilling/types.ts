import type { ActionLink } from "@concertable/shared/types/common";

export type SelfBillingAgreementStatus = "None" | "Active" | "Expired";

export interface SelfBillingAgreementActions {
  grant?: ActionLink | null;
  renew?: ActionLink | null;
  pdf?: ActionLink | null;
}

export interface SelfBillingAgreement {
  status: SelfBillingAgreementStatus;
  supplierLegalName: string | null;
  acceptedAtUtc: string | null;
  expiresAtUtc: string | null;
  actions: SelfBillingAgreementActions;
}
