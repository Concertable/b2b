import { useState } from "react";
import { Button } from "@concertable/web/components/ui/button";
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@concertable/web/components/ui/table";
import { useMembersRoster } from "../hooks/useMembersRoster";
import { memberRolesRequestSchema } from "../schemas/memberRolesRequestSchema";
import type { Member, Role } from "../types";
import { RoleSelector } from "./RoleSelector";
import { Spinner } from "./Spinner";

interface Props {
  canManageRoles: boolean;
  canRemove: boolean;
}

function MemberRow({ member, roles, canManageRoles, canRemove, isUpdating, changeRoles, removeMember }: {
  member: Member;
  roles: ReadonlyArray<Role>;
  canManageRoles: boolean;
  canRemove: boolean;
  isUpdating: boolean;
  changeRoles: (userId: string, request: { roleIds: string[] }, onDone: () => void) => void;
  removeMember: (userId: string) => void;
}) {
  const [selection, setSelection] = useState<string[] | undefined>();
  const selected = selection ?? member.roles.map((role) => role.id);
  const parsed = memberRolesRequestSchema.safeParse({ roleIds: selected });

  return (
    <TableRow data-testid={"member-row-" + member.userId}>
      <TableCell>{member.email}</TableCell>
      <TableCell>
        {canManageRoles ? (
          <div className="space-y-2">
            <RoleSelector roles={roles} selected={selected} onChange={setSelection} disabled={isUpdating} idPrefix={"member-" + member.userId} />
            {!parsed.success && <p className="text-destructive text-xs">{parsed.error.issues[0]?.message}</p>}
            <Button
              size="sm" variant="outline"
              disabled={selection === undefined || !parsed.success || isUpdating}
              onClick={() => {
                if (parsed.success) {
                  changeRoles(member.userId, parsed.data, () => setSelection(undefined));
                }
              }}
              data-testid={"member-roles-" + member.userId}
            >Save roles</Button>
          </div>
        ) : (
          member.roles.map((role) => role.name).join(", ")
        )}
      </TableCell>
      {canRemove && <TableCell className="text-right">
        <Button variant="ghost" size="sm" onClick={() => removeMember(member.userId)} data-testid={"remove-member-" + member.userId}>Remove</Button>
      </TableCell>}
    </TableRow>
  );
}

export function MembersRoster({ canManageRoles, canRemove }: Readonly<Props>) {
  const { members, roles, isLoading, isUpdating, changeRoles, removeMember } = useMembersRoster(canManageRoles);
  if (isLoading) return <Spinner />;
  if (!members || members.length === 0) return <p className="text-muted-foreground text-sm">No members yet.</p>;
  return (
    <div className="space-y-4">
      <h3 className="font-medium">Members</h3>
      <Table data-testid="members-roster">
        <TableHeader><TableRow><TableHead>Email</TableHead><TableHead>Roles</TableHead>{canRemove && <TableHead className="text-right">Actions</TableHead>}</TableRow></TableHeader>
        <TableBody>
          {members.map((member) => <MemberRow
            key={member.userId} member={member} roles={roles}
            canManageRoles={canManageRoles} canRemove={canRemove} isUpdating={isUpdating}
            changeRoles={changeRoles} removeMember={removeMember}
          />)}
        </TableBody>
      </Table>
    </div>
  );
}
