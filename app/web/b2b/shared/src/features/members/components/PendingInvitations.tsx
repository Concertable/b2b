import { Button } from "@concertable/web/components/ui/button";
import { usePendingInvitations } from "../hooks/usePendingInvitations";
import { Spinner } from "./Spinner";

function expiresInLabel(expiresAt: string): string {
  const days = Math.ceil((new Date(expiresAt).getTime() - Date.now()) / 86_400_000);
  if (days <= 0) return "expired";
  if (days === 1) return "expires in 1 day";
  return `expires in ${days} days`;
}

export function PendingInvitations() {
  const { invitations, isLoading, revoke } = usePendingInvitations();

  if (isLoading) return <Spinner />;

  return (
    <div className="space-y-4">
      <h3 className="font-medium">Pending invitations</h3>
      {!invitations || invitations.length === 0 ? (
        <p className="text-muted-foreground text-sm">No pending invitations.</p>
      ) : (
        <ul className="divide-border divide-y" data-testid="pending-invitations">
          {invitations.map((inv) => (
            <li
              key={inv.id}
              className="flex items-center justify-between gap-4 py-2"
              data-testid={`invitation-row-${inv.id}`}
            >
              <div className="space-y-0.5">
                <p className="text-sm font-medium">{inv.email}</p>
                <p className="text-muted-foreground text-xs">
                  {inv.role} · {expiresInLabel(inv.expiresAt)}
                </p>
              </div>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => revoke(inv.id)}
                data-testid={`revoke-invitation-${inv.id}`}
              >
                Revoke
              </Button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
