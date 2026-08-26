import { z } from "zod";
import type { InviteAdminRequest } from "../types";

export const inviteAdminRequestSchema = z.object({
  email: z.string().trim().toLowerCase().email("Enter a valid email address"),
}) satisfies z.ZodType<InviteAdminRequest>;
