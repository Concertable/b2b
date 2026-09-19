import { Button } from "@concertable/web/components/ui/button";
import { useAcceptInvitation } from "../hooks/useAcceptInvitation";
import type { TenantBusinessActivity } from "@b2b/features/tenant";

const Spinner = () => (
  <div className="text-muted-foreground size-6 animate-spin rounded-full border-2 border-current border-t-transparent" />
);

export function AcceptInvitationPage({
  invitationId,
  businessActivity,
}: Readonly<{ invitationId: string; businessActivity?: TenantBusinessActivity }>) {
  const { isError } = useAcceptInvitation(invitationId, businessActivity);

  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4 p-6 text-center">
      {isError ? (
        <div className="max-w-sm space-y-4" data-testid="accept-error">
          <h1 className="text-lg font-semibold">Couldn't accept the invitation</h1>
          <Button asChild variant="outline">
            <a href="/">Go to dashboard</a>
          </Button>
        </div>
      ) : (
        <div className="flex flex-col items-center gap-3" data-testid="accept-pending">
          <Spinner />
          <p className="text-muted-foreground text-sm">Accepting your invitation…</p>
        </div>
      )}
    </div>
  );
}
