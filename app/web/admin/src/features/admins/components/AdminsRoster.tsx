import { Button } from "@concertable/web/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@concertable/web/components/ui/table";
import { useAdminsRoster } from "../hooks/useAdminsRoster";
import { Spinner } from "./Spinner";

export function AdminsRoster() {
  const { admins, isLoading, canRevoke, revoke } = useAdminsRoster();

  if (isLoading) return <Spinner />;
  if (!admins || admins.length === 0)
    return <p className="text-muted-foreground text-sm">No admins yet.</p>;

  return (
    <div className="space-y-4">
      <h3 className="font-medium">Admins</h3>
      <Table data-testid="admins-roster">
        <TableHeader>
          <TableRow>
            <TableHead>Email</TableHead>
            <TableHead className="text-right">Actions</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {admins.map((admin) => (
            <TableRow key={admin.sub} data-testid={`admin-row-${admin.sub}`}>
              <TableCell>{admin.email}</TableCell>
              <TableCell className="text-right">
                <Button
                  variant="ghost"
                  size="sm"
                  disabled={!canRevoke}
                  onClick={() => revoke(admin.sub)}
                  data-testid={`revoke-admin-${admin.sub}`}
                >
                  Revoke
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
