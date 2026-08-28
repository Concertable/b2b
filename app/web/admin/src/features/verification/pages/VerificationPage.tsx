import { PendingVerificationsList } from "../components/PendingVerificationsList";

export function VerificationPage() {
  return (
    <div className="max-w-4xl space-y-8">
      <div>
        <h2 className="text-lg font-semibold">Organisation verification</h2>
        <p className="text-muted-foreground text-sm">
          Review evidence submitted by venues and artists, then approve or reject
          each organisation.
        </p>
      </div>

      <PendingVerificationsList />
    </div>
  );
}
