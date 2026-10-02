import { useState } from "react";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { Button } from "@concertable/web/components/ui/button";
import { Input } from "@concertable/web/components/ui/input";
import { useOrganizationQuery } from "@concertable/web-b2b/features/organizations";

function BusinessHomePage() {
  const navigate = useNavigate();
  const organization = useOrganizationQuery();
  const [concertId, setConcertId] = useState("");
  return (
    <main className="mx-auto max-w-4xl space-y-8 p-8">
      <div>
        <h1 className="text-3xl font-semibold">
          {organization.data?.legalName ?? "Operations"}
        </h1>
        <p className="mt-2 text-muted-foreground">
          Open concert summaries shared with this organization.
        </p>
      </div>
      <form
        className="flex max-w-md gap-3"
        onSubmit={(event) => {
          event.preventDefault();
          if (/^\d+$/.test(concertId))
            void navigate({
              to: "/concert/$id",
              params: { id: Number(concertId) },
            });
        }}
      >
        <Input
          aria-label="Concert ID"
          inputMode="numeric"
          onChange={(event) => setConcertId(event.target.value)}
          placeholder="Concert ID"
          value={concertId}
        />
        <Button type="submit" disabled={!/^\d+$/.test(concertId)}>
          Open summary
        </Button>
      </form>
    </main>
  );
}

export const Route = createFileRoute("/_business/app")({ component: BusinessHomePage });
