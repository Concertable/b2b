import { useTenant, type TenantBusinessActivity } from "@b2b/features/tenant";
import { Separator } from "@concertable/web/components/ui/separator";
import { MembersRoster } from "../components/MembersRoster";
import { PendingInvitations } from "../components/PendingInvitations";
import { InviteForm } from "../components/InviteForm";

interface MembersPageProps {
  businessActivity?: TenantBusinessActivity;
  title: string;
  description: string;
}

export function MembersPage({ businessActivity, title, description }: MembersPageProps) {
  const { permissions } = useTenant(businessActivity);
  const canInvite = permissions.has("members.invite");
  const canManageRoles = permissions.has("members.manage_roles");
  const canRemove = permissions.has("members.remove");

  return (
    <div className="max-w-2xl space-y-8">
      <div>
        <h2 className="text-lg font-semibold">{title}</h2>
        <p className="text-muted-foreground text-sm">{description}</p>
      </div>

      <Separator />

      <MembersRoster canManageRoles={canManageRoles} canRemove={canRemove} />

      {canInvite && (
        <>
          <Separator />
          <PendingInvitations />
          <Separator />
          <InviteForm />
        </>
      )}
    </div>
  );
}
