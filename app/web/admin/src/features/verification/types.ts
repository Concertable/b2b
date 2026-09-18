export type VerificationTenantBusinessProfile = "venue" | "artist";

export const VERIFICATION_TENANT_TYPE_LABELS: Record<
  VerificationTenantBusinessProfile,
  string
> = {
  venue: "Venue",
  artist: "Artist",
};

export interface PendingVerification {
  tenantId: string;
  businessProfile: VerificationTenantBusinessProfile;
  name?: string;
  email?: string;
  submittedAt: string;
}
