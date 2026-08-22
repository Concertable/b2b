import { z } from "zod";
import {
  INVITE_MEMBER_ROLES,
  type InviteMemberRequest,
} from "../types";

export const inviteMemberRequestSchema = z.object({
  email: z.string().trim().toLowerCase().email("Enter a valid email address"),
  role: z.enum(INVITE_MEMBER_ROLES),
}) satisfies z.ZodType<InviteMemberRequest>;
