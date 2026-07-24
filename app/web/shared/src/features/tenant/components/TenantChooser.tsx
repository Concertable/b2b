import type { TenantType } from "../types";
import { Button } from "@/components/ui/button";
import {
  useSamePersonaMemberships,
  useSelectTenant,
} from "../model";

export function TenantChooser({ persona }: Readonly<{ persona: TenantType }>) {
  const memberships = useSamePersonaMemberships(persona);
  const selectTenant = useSelectTenant();

  return (
    <div className="flex min-h-screen flex-col items-center justify-center p-6">
      <div className="w-full max-w-sm space-y-6" data-testid="tenant-chooser">
        <div className="space-y-1 text-center">
          <h1 className="text-lg font-semibold">Choose your organization</h1>
          <p className="text-muted-foreground text-sm">
            You manage more than one organization. Pick which one to work in — you
            can switch at any time.
          </p>
        </div>
        <div className="flex flex-col gap-2">
          {memberships.map((m) => (
            <Button
              key={m.tenantId}
              variant="outline"
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
