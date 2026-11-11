import { useQuery } from "@tanstack/react-query";
import type { TenantSession } from "@concertable/b2b/features/tenant/types";
import { conversationQueries } from "../queryOptions";

export function useConversationUnreadCountQuery(session: TenantSession) {
  return useQuery(conversationQueries.unreadCount(session));
}
