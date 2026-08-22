import { useState } from "react";
import { Badge } from "@concertable/web/components/ui/badge";
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
import { REPORT_CATEGORY_LABELS } from "@concertable/shared/features/messaging";
import { useReportsQueue } from "../hooks/useReportsQueue";
import { REPORT_OUTCOME_LABELS } from "../types";
import { ResolveReportDialog } from "./ResolveReportDialog";

export function ReportsQueue() {
  const {
    reports,
    pageNumber,
    totalPages,
    isLoading,
    isError,
    nextPage,
    prevPage,
    hideMessage,
    restoreMessage,
  } = useReportsQueue();
  const [resolvingReportId, setResolvingReportId] = useState<number>();

  if (isLoading) return <Spinner />;
  if (isError)
    return (
      <p className="text-destructive text-sm">Failed to load reports.</p>
    );
  if (!reports || reports.length === 0)
    return <p className="text-muted-foreground text-sm">No reports.</p>;

  return (
    <div className="space-y-4">
      <Table data-testid="reports-queue">
        <TableHeader>
          <TableRow>
            <TableHead>Reference</TableHead>
            <TableHead>Category</TableHead>
            <TableHead>Excerpt</TableHead>
            <TableHead>Status</TableHead>
            <TableHead className="text-right">Actions</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {reports.map((report) => (
            <TableRow key={report.id} data-testid={`report-row-${report.id}`}>
              <TableCell className="font-mono text-xs">{report.reference}</TableCell>
              <TableCell>{REPORT_CATEGORY_LABELS[report.category]}</TableCell>
              <TableCell className="max-w-xs truncate" title={report.messageExcerpt}>
                {report.messageExcerpt}
              </TableCell>
              <TableCell>
                {report.outcome ? (
                  <Badge variant="secondary">
                    {REPORT_OUTCOME_LABELS[report.outcome]}
                  </Badge>
                ) : (
                  <Badge>Pending</Badge>
                )}
              </TableCell>
              <TableCell className="text-right space-x-2">
                <Button
                  variant="destructive"
                  size="sm"
                  onClick={() => hideMessage(report.messageId)}
                  data-testid={`hide-message-${report.id}`}
                >
                  Hide
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => restoreMessage(report.messageId)}
                  data-testid={`restore-message-${report.id}`}
                >
                  Restore
                </Button>
                <Button
                  size="sm"
                  disabled={report.outcome != null}
                  onClick={() => setResolvingReportId(report.id)}
                  data-testid={`resolve-report-${report.id}`}
                >
                  Resolve
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

      {resolvingReportId !== undefined && (
        <ResolveReportDialog
          reportId={resolvingReportId}
          open
          onOpenChange={(next) => !next && setResolvingReportId(undefined)}
        />
      )}
    </div>
  );
}
