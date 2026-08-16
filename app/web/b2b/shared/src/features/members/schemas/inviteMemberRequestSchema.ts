import { z } from "zod";

export const inviteMemberRequestSchema = z.object({
  email: z.string().trim().toLowerCase().email("Enter a valid email address"),
  role: z.enum(["manager", "finance", "staff", "door", "sound"]),
});

export type InviteMemberRequest = z.infer<typeof inviteMemberRequestSchema>;
