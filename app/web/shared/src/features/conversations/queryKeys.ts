import { privateQueryKey } from "@concertable/b2b/features/tenant";
import type { TenantSession } from "@concertable/b2b/features/tenant/types";

const root = (session: TenantSession | undefined) =>
  session === undefined
    ? (["tenant", "unselected", "unselected", 0, "conversations"] as const)
    : privateQueryKey(session, "conversations");

export const conversationKeys = {
  all: root,
  unreadCount: (session: TenantSession | undefined) =>
    [...root(session), "unread-count"] as const,
  previews: (session: TenantSession | undefined) =>
    [...root(session), "previews"] as const,
  previewPage: (session: TenantSession | undefined, pageNumber: number) =>
    [...root(session), "previews", pageNumber] as const,
  messages: (session: TenantSession | undefined, conversationId: number) =>
    [...root(session), "messages", conversationId] as const,
};
