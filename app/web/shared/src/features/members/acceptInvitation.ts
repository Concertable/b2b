import type { Membership } from "@b2b/features/tenant";

interface InvitationAcceptanceDependencies {
  readonly accept: (invitationId: string) => Promise<Membership>;
  readonly selectTenant: (tenantId: string) => Promise<void>;
  readonly navigate: (path: string) => void;
}

export async function acceptInvitation(
  invitationId: string,
  { accept, selectTenant, navigate }: InvitationAcceptanceDependencies,
) {
  const membership = await accept(invitationId);
  await selectTenant(membership.tenantId);
  navigate("/settings/members");
  return membership;
}
