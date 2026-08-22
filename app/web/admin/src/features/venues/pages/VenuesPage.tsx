import { PendingVenuesList } from "../components/PendingVenuesList";

export function VenuesPage() {
  return (
    <div className="max-w-4xl space-y-8">
      <div>
        <h2 className="text-lg font-semibold">Venue approval</h2>
        <p className="text-muted-foreground text-sm">
          Review and approve venues awaiting platform approval.
        </p>
      </div>

      <PendingVenuesList />
    </div>
  );
}
