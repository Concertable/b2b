import type { TenantType } from "../types";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useTenant } from "../hooks/useTenant";

export function TenantSwitcher({ persona }: Readonly<{ persona: TenantType }>) {
  const { memberships, activeMembership, selectTenant } = useTenant(persona);

  if (memberships.length <= 1) return null;

  return (
    <Select
      value={activeMembership?.tenantId ?? ""}
      onValueChange={selectTenant}
    >
      <SelectTrigger
        size="sm"
        data-testid="tenant-switcher"
        className="border-primary-foreground/20 bg-primary-foreground/10 text-primary-foreground max-w-48"
      >
        <SelectValue placeholder="Select organization" />
      </SelectTrigger>
      <SelectContent>
        {memberships.map((m) => (
          <SelectItem
            key={m.tenantId}
            value={m.tenantId}
            data-testid={`tenant-option-${m.tenantId}`}
          >
            {m.legalName}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
