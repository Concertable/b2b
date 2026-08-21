import { ReportsQueue } from "../components/ReportsQueue";

export function ModerationPage() {
  return (
    <div className="max-w-4xl space-y-8">
      <div>
        <h2 className="text-lg font-semibold">Moderation</h2>
        <p className="text-muted-foreground text-sm">
          Review reported messages and take action.
        </p>
      </div>

      <ReportsQueue />
    </div>
  );
}
