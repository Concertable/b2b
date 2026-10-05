import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button } from "@concertable/web/components/ui/button";
import { Input } from "@concertable/web/components/ui/input";
import { Label } from "@concertable/web/components/ui/label";
import { useInviteMember } from "../hooks/useInviteMember";
import { inviteMemberRequestSchema } from "../schemas/inviteMemberRequestSchema";
import type { InviteMemberRequest } from "../types";
import { RoleSelector } from "./RoleSelector";

export function InviteForm() {
  const { submit, isPending, roles, rolesLoading } = useInviteMember();
  const {
    control, register, handleSubmit, reset,
    formState: { errors, isValid },
  } = useForm<InviteMemberRequest>({
    resolver: zodResolver(inviteMemberRequestSchema),
    defaultValues: { email: "", roleIds: [] },
    mode: "onChange",
  });

  return (
    <form
      onSubmit={handleSubmit((request) => submit(request, () => reset()))}
      className="space-y-4"
      data-testid="invite-form"
    >
      <h3 className="font-medium">Invite a member</h3>
      <div className="space-y-1">
        <Label htmlFor="invite-email">Email</Label>
        <Input id="invite-email" type="email" aria-invalid={errors.email !== undefined} data-testid="invite-email" {...register("email")} />
        {errors.email && <p className="text-destructive text-xs" data-testid="invite-error">{errors.email.message}</p>}
      </div>
      <div className="space-y-1">
        <p className="text-sm font-medium">Roles</p>
        <Controller
          control={control}
          name="roleIds"
          render={({ field }) => (
            <RoleSelector
              roles={roles.filter((role) => role.isInvitationAssignable)}
              selected={field.value}
              onChange={field.onChange}
              disabled={rolesLoading || isPending}
              idPrefix="invite-role"
            />
          )}
        />
        {errors.roleIds && <p className="text-destructive text-xs">{errors.roleIds.message}</p>}
        {!rolesLoading && roles.length === 0 && <p className="text-muted-foreground text-xs">No roles are available to assign.</p>}
      </div>
      <Button type="submit" disabled={isPending || rolesLoading || !isValid} data-testid="invite-submit">
        {isPending ? "Sending..." : "Send invite"}
      </Button>
    </form>
  );
}
