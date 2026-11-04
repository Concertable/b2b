import { Checkbox } from "@concertable/web/components/ui/checkbox";
import { Label } from "@concertable/web/components/ui/label";
import type { Role } from "../types";

interface Props {
  roles: ReadonlyArray<Role>;
  selected: ReadonlyArray<string>;
  onChange: (roleIds: string[]) => void;
  disabled?: boolean;
  idPrefix: string;
}

export function RoleSelector({ roles, selected, onChange, disabled, idPrefix }: Readonly<Props>) {
  return (
    <div className="space-y-2">
      {roles.map((role) => {
        const id = idPrefix + "-" + role.id;
        return (
          <div key={role.id} className="flex items-center gap-2">
            <Checkbox
              id={id}
              checked={selected.includes(role.id)}
              disabled={disabled}
              onCheckedChange={(checked) =>
                onChange(checked === true ? [...selected, role.id] : selected.filter((value) => value !== role.id))
              }
            />
            <Label htmlFor={id}>{role.name}</Label>
          </div>
        );
      })}
    </div>
  );
}
