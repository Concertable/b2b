import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button } from "@concertable/web/components/ui/button";
import { Input } from "@concertable/web/components/ui/input";
import { Label } from "@concertable/web/components/ui/label";
import { useInviteAdmin } from "../hooks/useInviteAdmin";
import { inviteAdminRequestSchema } from "../schemas/inviteAdminRequestSchema";
import type { InviteAdminRequest } from "../types";

export function InviteForm() {
  const { submit, isPending } = useInviteAdmin();
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<InviteAdminRequest>({
    resolver: zodResolver(inviteAdminRequestSchema),
    defaultValues: { email: "" },
    mode: "onChange",
  });

  const onValid = (request: InviteAdminRequest) => {
    submit(request, () => reset());
  };

  return (
    <form
      onSubmit={handleSubmit(onValid)}
      className="space-y-4"
      data-testid="invite-form"
    >
      <h3 className="font-medium">Invite an admin</h3>
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
        <div className="flex-1 space-y-1">
          <Label htmlFor="invite-email">Email</Label>
          <Input
            id="invite-email"
            type="email"
            aria-invalid={errors.email != null}
            data-testid="invite-email"
            {...register("email")}
          />
        </div>
        <Button type="submit" disabled={isPending} data-testid="invite-submit">
          {isPending ? "Sending..." : "Send invite"}
        </Button>
      </div>
      {errors.email && (
        <p className="text-destructive text-xs" data-testid="invite-error">
          {errors.email.message}
        </p>
      )}
    </form>
  );
}
