export type VerificationTenantType = "venue" | "artist";

export const VERIFICATION_TENANT_TYPE_LABELS: Record<
  VerificationTenantType,
  string
> = {
  venue: "Venue",
  artist: "Artist",
};

export interface PendingVerification {
  tenantId: string;
  tenantType: VerificationTenantType;
  name?: string;
  email?: string;
  submittedAt: string;
}
