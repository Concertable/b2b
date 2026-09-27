export interface PendingVerification {
  readonly tenantId: string;
  readonly legalName: string;
  readonly contactEmail: string;
  readonly submittedAt: string;
}
