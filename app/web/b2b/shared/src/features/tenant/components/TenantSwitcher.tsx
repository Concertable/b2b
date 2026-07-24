import type { TenantType } from "../types";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  useActiveMembership,
  useSamePersonaMemberships,
  useSelectTenant,
} from "../model";

export function TenantSwitcher({ persona }: Readonly<{ persona: TenantType }>) {
  const memberships = useSamePersonaMemberships(persona);
  const active = useActiveMembership(persona);
  const selectTenant = useSelectTenant();

  if (memberships.length <= 1) return null;

  return (
    <Select value={active?.tenantId ?? ""} onValueChange={selectTenant}>
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
