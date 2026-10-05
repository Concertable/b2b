import { z } from "zod";

export const memberRolesRequestSchema = z.object({
  roleIds: z.array(z.string().uuid()).min(1, "Select at least one role"),
});
