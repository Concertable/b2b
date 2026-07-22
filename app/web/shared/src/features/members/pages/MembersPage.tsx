import { useHasPermission, type TenantType } from "@b2b/features/tenant";
import { Separator } from "@/components/ui/separator";
import { MembersRoster } from "../components/MembersRoster";
import { PendingInvitations } from "../components/PendingInvitations";
import { InviteForm } from "../components/InviteForm";

interface MembersPageProps {
  persona: TenantType;
  title: string;
  description: string;
}

export function MembersPage({ persona, title, description }: MembersPageProps) {
  const canInvite = useHasPermission(persona, "MembersInvite");
  const canManageRoles = useHasPermission(persona, "MembersManageRoles");
  const canRemove = useHasPermission(persona, "MembersRemove");

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
