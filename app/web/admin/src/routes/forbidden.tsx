import { createFileRoute } from "@tanstack/react-router";

function Forbidden() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-2 p-8 text-center">
      <h1 className="text-lg font-semibold">Access denied</h1>
      <p className="text-muted-foreground text-sm">
        This account does not have admin access to Concertable.
      </p>
    </div>
  );
}

export const Route = createFileRoute("/forbidden")({
  component: Forbidden,
});
