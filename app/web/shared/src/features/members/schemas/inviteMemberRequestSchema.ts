import { z } from "zod";
import { memberRolesRequestSchema } from "./memberRolesRequestSchema";

export const inviteMemberRequestSchema = memberRolesRequestSchema.extend({
  email: z.string().trim().toLowerCase().email("Enter a valid email address"),
});
