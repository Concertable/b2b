import type { ActionLink } from "@concertable/shared/types/common";

export type SelfBillingAgreementStatus = "none" | "active" | "expired";

export interface SelfBillingAgreementActions {
  grant?: ActionLink;
  renew?: ActionLink;
  pdf?: ActionLink;
}

export interface SelfBillingAgreement {
  status: SelfBillingAgreementStatus;
  supplierLegalName?: string;
  acceptedAtUtc?: string;
  expiresAtUtc?: string;
  actions: SelfBillingAgreementActions;
}
