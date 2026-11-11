import { queryOptions } from "@tanstack/react-query";
import { tenantSession } from "@concertable/b2b/features/tenant";
import type { TenantSession } from "@concertable/b2b/features/tenant/types";
import conversationApi from "./api/conversationApi";
import { conversationKeys } from "./queryKeys";

export function requireCurrentSession(session: TenantSession | undefined): void {
  if (session === undefined || !tenantSession.isCurrent(session))
    throw new Error("The organization changed. Reopen the inbox.");
}

export const conversationQueries = {
  previews: (session: TenantSession | undefined, pageNumber = 1) =>
    queryOptions({
      queryKey: conversationKeys.previewPage(session, pageNumber),
      queryFn: ({ signal }) => {
        requireCurrentSession(session);
        return conversationApi.getPreviews(pageNumber, signal);
      },
      enabled: session !== undefined,
      meta: { silenceErrors: true },
    }),

  messages: (session: TenantSession | undefined, conversationId: number) =>
    queryOptions({
      queryKey: conversationKeys.messages(session, conversationId),
      queryFn: ({ signal }) => {
        requireCurrentSession(session);
        return conversationApi.getMessages(conversationId, signal);
      },
      enabled: session !== undefined,
      meta: { silenceErrors: true },
    }),

  unreadCount: (session: TenantSession | undefined) =>
    queryOptions({
      queryKey: conversationKeys.unreadCount(session),
      queryFn: ({ signal }) => {
        requireCurrentSession(session);
        return conversationApi.getUnreadCount(signal);
      },
      enabled: session !== undefined,
      refetchInterval: 30_000,
      meta: { silenceErrors: true },
    }),
};
