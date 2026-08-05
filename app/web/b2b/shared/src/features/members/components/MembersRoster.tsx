import { TENANT_ROLES, type TenantRole } from "@b2b/features/tenant";
import { Button } from "@concertable/web/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@concertable/web/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@concertable/web/components/ui/table";
import { useMembersRoster } from "../hooks/useMembersRoster";
import { Spinner } from "./Spinner";

interface Props {
  canManageRoles: boolean;
  canRemove: boolean;
}

export function MembersRoster({ canManageRoles, canRemove }: Readonly<Props>) {
  const { members, isLoading, changeRole, removeMember } = useMembersRoster();

  if (isLoading) return <Spinner />;
  if (!members || members.length === 0)
    return <p className="text-muted-foreground text-sm">No members yet.</p>;

  return (
    <div className="space-y-4">
      <h3 className="font-medium">Members</h3>
      <Table data-testid="members-roster">
        <TableHeader>
          <TableRow>
            <TableHead>Email</TableHead>
            <TableHead>Role</TableHead>
            {canRemove && <TableHead className="text-right">Actions</TableHead>}
          </TableRow>
        </TableHeader>
        <TableBody>
          {members.map((m) => (
            <TableRow key={m.userId} data-testid={`member-row-${m.userId}`}>
              <TableCell>{m.email}</TableCell>
              <TableCell>
                {canManageRoles ? (
                  <Select
                    value={m.role}
                    onValueChange={(role) =>
                      changeRole(m.userId, role as TenantRole)
                    }
                  >
                    <SelectTrigger
                      size="sm"
                      className="w-36"
                      data-testid={`member-role-${m.userId}`}
                    >
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {TENANT_ROLES.map((role) => (
                        <SelectItem key={role} value={role}>
                          {role}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                ) : (
                  m.role
                )}
              </TableCell>
              {canRemove && (
                <TableCell className="text-right">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => removeMember(m.userId)}
                    data-testid={`remove-member-${m.userId}`}
                  >
                    Remove
                  </Button>
                </TableCell>
              )}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
