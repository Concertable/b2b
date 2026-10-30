import { useQuery } from "@tanstack/react-query";
import type { TenantSession } from "@concertable/b2b/features/tenant/types";
import { conversationQueries } from "../queryOptions";

export function useConversationMessagesQuery(
  session: TenantSession,
  conversationId: number,
) {
  return useQuery(conversationQueries.messages(session, conversationId));
}
