import type { TenantType } from "@concertable/b2b/features/tenant/types";
import { Button } from "@concertable/web/components/ui/button";
import { useTenant } from "../hooks/useTenant";

export function TenantChooser({ tenantType }: Readonly<{ tenantType: TenantType }>) {
  const { memberships, isSelectionPending, selectTenant } =
    useTenant(tenantType);

  return (
    <div className="flex min-h-screen flex-col items-center justify-center p-6">
      <div className="w-full max-w-sm space-y-6" data-testid="tenant-chooser">
        <div className="space-y-1 text-center">
          <h1 className="text-lg font-semibold">Choose your organization</h1>
          <p className="text-muted-foreground text-sm">
            You manage more than one organization. Pick which one to work in —
            you can switch at any time.
          </p>
        </div>
        <div className="flex flex-col gap-2">
          {memberships.map((m) => (
            <Button
              key={m.tenantId}
              variant="outline"
              disabled={isSelectionPending}
              className="justify-start"
              onClick={() => selectTenant(m.tenantId)}
              data-testid={`tenant-chooser-option-${m.tenantId}`}
            >
              {m.legalName}
            </Button>
          ))}
        </div>
      </div>
    </div>
  );
}
