import { useState } from "react";
import dayjs from "dayjs";
import { Button } from "@concertable/web/components/ui/button";
import { PaginationControls } from "@concertable/web/components/ui/PaginationControls";
import { Spinner } from "@concertable/web/components/ui/spinner";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@concertable/web/components/ui/table";
import { usePendingVerifications } from "../hooks/usePendingVerifications";
import { VERIFICATION_TENANT_TYPE_LABELS } from "../types";
import { RejectVerificationDialog } from "./RejectVerificationDialog";

export function PendingVerificationsList() {
  const {
    verifications,
    pageNumber,
    totalPages,
    isLoading,
    isError,
    nextPage,
    prevPage,
    approve,
  } = usePendingVerifications();
  const [rejectingTenantId, setRejectingTenantId] = useState<string>();

  if (isLoading) return <Spinner />;
  if (isError)
    return (
      <p className="text-destructive text-sm">
        Failed to load pending verifications.
      </p>
    );
  if (!verifications || verifications.length === 0)
    return (
      <p className="text-muted-foreground text-sm">
        No organisations awaiting verification.
      </p>
    );

  return (
    <div className="space-y-4">
      <Table data-testid="pending-verifications-list">
        <TableHeader>
          <TableRow>
            <TableHead>Name</TableHead>
            <TableHead>Type</TableHead>
            <TableHead>Email</TableHead>
            <TableHead>Submitted</TableHead>
            <TableHead className="text-right">Actions</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {verifications.map((verification) => (
            <TableRow
              key={verification.tenantId}
              data-testid={`pending-verification-row-${verification.tenantId}`}
            >
              <TableCell>{verification.name ?? "—"}</TableCell>
              <TableCell>
                {VERIFICATION_TENANT_TYPE_LABELS[verification.tenantType]}
              </TableCell>
              <TableCell>{verification.email ?? "—"}</TableCell>
              <TableCell>
                {dayjs(verification.submittedAt).format("D MMM YYYY")}
              </TableCell>
              <TableCell className="space-x-2 text-right">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setRejectingTenantId(verification.tenantId)}
                  data-testid={`reject-verification-${verification.tenantId}`}
                >
                  Reject
                </Button>
                <Button
                  size="sm"
                  onClick={() => approve(verification.tenantId)}
                  data-testid={`approve-verification-${verification.tenantId}`}
                >
                  Approve
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

      <PaginationControls
        pageNumber={pageNumber}
        totalPages={totalPages}
        onPrev={prevPage}
        onNext={nextPage}
      />

      {rejectingTenantId !== undefined && (
        <RejectVerificationDialog
          tenantId={rejectingTenantId}
          open
          onOpenChange={(next) => !next && setRejectingTenantId(undefined)}
        />
      )}
    </div>
  );
}
