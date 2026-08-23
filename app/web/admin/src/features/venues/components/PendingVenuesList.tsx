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
import { usePendingVenues } from "../hooks/usePendingVenues";

export function PendingVenuesList() {
  const {
    venues,
    pageNumber,
    totalPages,
    isLoading,
    isError,
    nextPage,
    prevPage,
    approve,
  } = usePendingVenues();

  if (isLoading) return <Spinner />;
  if (isError)
    return (
      <p className="text-destructive text-sm">Failed to load pending venues.</p>
    );
  if (!venues || venues.length === 0)
    return <p className="text-muted-foreground text-sm">No venues awaiting approval.</p>;

  return (
    <div className="space-y-4">
      <Table data-testid="pending-venues-list">
        <TableHeader>
          <TableRow>
            <TableHead>Name</TableHead>
            <TableHead>Location</TableHead>
            <TableHead>Email</TableHead>
            <TableHead className="text-right">Actions</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {venues.map((venue) => (
            <TableRow key={venue.id} data-testid={`pending-venue-row-${venue.id}`}>
              <TableCell>{venue.name}</TableCell>
              <TableCell>{venue.town}, {venue.county}</TableCell>
              <TableCell>{venue.email}</TableCell>
              <TableCell className="text-right">
                <Button
                  size="sm"
                  onClick={() => approve(venue.id)}
                  data-testid={`approve-venue-${venue.id}`}
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
    </div>
  );
}
