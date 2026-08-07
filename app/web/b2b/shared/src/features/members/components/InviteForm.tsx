import { useState, type FormEvent } from "react";
import { Button } from "@concertable/web/components/ui/button";
import { Input } from "@concertable/web/components/ui/input";
import { Label } from "@concertable/web/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@concertable/web/components/ui/select";
import {
  useInviteMember,
  type InviteBuffer,
} from "../hooks/useInviteMember";

export function InviteForm() {
  const { submit, validate, isPending, roleOptions } = useInviteMember();
  const [email, setEmail] = useState("");
  const [role, setRole] = useState<InviteBuffer["role"]>("Manager");
  const [touched, setTouched] = useState(false);

  const parsed = validate({ email, role });
  const error = touched && !parsed.success ? parsed.error.issues[0].message : null;

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    const result = submit({ email, role }, () => {
      setEmail("");
      setRole("Manager");
      setTouched(false);
    });
    if (!result.success) setTouched(true);
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4" data-testid="invite-form">
      <h3 className="font-medium">Invite a member</h3>
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
        <div className="space-y-1">
          <Label htmlFor="invite-role">Role</Label>
          <Select
            value={role}
            onValueChange={(r) => setRole(r as InviteBuffer["role"])}
          >
            <SelectTrigger
              id="invite-role"
              className="w-36"
              data-testid="invite-role"
            >
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {roleOptions.map((r) => (
                <SelectItem key={r} value={r}>
                  {r}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
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
