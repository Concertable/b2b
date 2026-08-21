import { useState, type FormEvent } from "react";
import { Button } from "@concertable/web/components/ui/button";
import { Input } from "@concertable/web/components/ui/input";
import { Label } from "@concertable/web/components/ui/label";
import { useInviteAdmin } from "../hooks/useInviteAdmin";

export function InviteForm() {
  const { submit, validate, isPending } = useInviteAdmin();
  const [email, setEmail] = useState("");
  const [touched, setTouched] = useState(false);

  const parsed = validate({ email });
  const error = touched && !parsed.success ? parsed.error.issues[0].message : null;

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    const result = submit({ email }, () => {
      setEmail("");
      setTouched(false);
    });
    if (!result.success) setTouched(true);
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4" data-testid="invite-form">
      <h3 className="font-medium">Invite an admin</h3>
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
        <div className="flex-1 space-y-1">
          <Label htmlFor="invite-email">Email</Label>
          <Input
            id="invite-email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            onBlur={() => setTouched(true)}
            aria-invalid={error != null}
            data-testid="invite-email"
          />
        </div>
        <Button type="submit" disabled={isPending} data-testid="invite-submit">
          {isPending ? "Sending..." : "Send invite"}
        </Button>
      </div>
      {error && (
        <p className="text-destructive text-xs" data-testid="invite-error">
          {error}
        </p>
      )}
    </form>
  );
}
