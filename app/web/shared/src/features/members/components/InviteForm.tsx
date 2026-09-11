import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { TENANT_ROLE_LABELS } from "@b2b/features/tenant";
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
import { useInviteMember } from "../hooks/useInviteMember";
import { inviteMemberRequestSchema } from "../schemas/inviteMemberRequestSchema";
import type { InviteMemberRequest } from "../types";

export function InviteForm() {
  const { submit, isPending, roleOptions } = useInviteMember();
  const {
    control,
    register,
    handleSubmit,
    reset,
    formState: { errors, isValid },
  } = useForm<InviteMemberRequest>({
    resolver: zodResolver(inviteMemberRequestSchema),
    defaultValues: { email: "", role: "manager" },
    mode: "onChange",
  });

  const onValid = (request: InviteMemberRequest) => {
    submit(request, () => reset());
  };

  return (
    <form
      onSubmit={handleSubmit(onValid)}
      className="space-y-4"
      data-testid="invite-form"
    >
      <h3 className="font-medium">Invite a member</h3>
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
        <div className="flex-1 space-y-1">
          <Label htmlFor="invite-email">Email</Label>
          <Input
            id="invite-email"
            type="email"
            aria-invalid={errors.email !== undefined}
            data-testid="invite-email"
            {...register("email")}
          />
        </div>
        <div className="space-y-1">
          <Label htmlFor="invite-role">Role</Label>
          <Controller
            control={control}
            name="role"
            render={({ field }) => (
              <Select value={field.value} onValueChange={field.onChange}>
                <SelectTrigger
                  id="invite-role"
                  className="w-36"
                  data-testid="invite-role"
                >
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {roleOptions.map((role) => (
                    <SelectItem key={role} value={role}>
                      {TENANT_ROLE_LABELS[role]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          />
        </div>
        <Button
          type="submit"
          disabled={isPending || !isValid}
          data-testid="invite-submit"
        >
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
