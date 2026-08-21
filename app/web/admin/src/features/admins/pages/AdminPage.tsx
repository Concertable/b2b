import { Separator } from "@concertable/web/components/ui/separator";
import { AdminsRoster } from "../components/AdminsRoster";
import { PendingInvitations } from "../components/PendingInvitations";
import { InviteForm } from "../components/InviteForm";

export function AdminPage() {
  return (
    <div className="max-w-2xl space-y-8">
      <div>
        <h2 className="text-lg font-semibold">Admins</h2>
        <p className="text-muted-foreground text-sm">
          Manage who has platform admin access.
        </p>
      </div>

      <Separator />

      <AdminsRoster />

      <Separator />

      <PendingInvitations />

      <Separator />

      <InviteForm />
    </div>
  );
}
